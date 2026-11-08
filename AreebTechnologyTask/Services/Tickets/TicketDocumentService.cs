using System.Globalization;
using System.Text;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EventHub.Services.Tickets
{
    /// <summary>Creates the files that make up a ticket: QR code image, PDF ticket and calendar (.ics) file.</summary>
    public class TicketDocumentService
    {
        private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

        // Events have no end time in the database, so calendar entries assume this length.
        private static readonly TimeSpan AssumedEventLength = TimeSpan.FromHours(3);

        private readonly IWebHostEnvironment _environment;

        public TicketDocumentService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>QR code (PNG) that opens the ticket's verification page when scanned.</summary>
        public byte[] CreateQrPng(TicketInfo ticket, int pixelsPerModule = 10)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(ticket.VerifyUrl, QRCodeGenerator.ECCLevel.Q);
            return new PngByteQRCode(data).GetGraphic(pixelsPerModule);
        }

        /// <summary>The event's picture from wwwroot/Images, or null for external or missing images.</summary>
        public (byte[] Bytes, string FileName)? LoadEventImage(TicketInfo ticket)
        {
            var url = ticket.ImageUrl?.Trim() ?? string.Empty;
            if (url.Length == 0 || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                                || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var relative = url.TrimStart('~').TrimStart('/');
            if (!relative.Contains('/'))
            {
                relative = "Images/" + relative;
            }

            var file = _environment.WebRootFileProvider.GetFileInfo(relative);
            if (!file.Exists || file.PhysicalPath == null)
            {
                return null;
            }
            return (File.ReadAllBytes(file.PhysicalPath), Path.GetFileName(file.PhysicalPath));
        }

        /// <summary>A one-page PDF ticket (A5-landscape wide, as tall as its content) with the event details and the QR code.</summary>
        public byte[] CreatePdf(TicketInfo ticket)
        {
            var qr = CreateQrPng(ticket);
            var image = LoadEventImage(ticket)?.Bytes;

            const string dark = "#2C3E50", muted = "#6C757D", border = "#D0D7DE";

            return Document.Create(document => document.Page(page =>
            {
                // Fixed width, height that fits the ticket (no empty space under it).
                page.ContinuousSize(PageSizes.A5.Landscape().Width);
                page.Margin(24);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(text => text.FontSize(10).FontColor(dark));

                page.Content().Border(1).BorderColor(border).Column(column =>
                {
                    // Header band
                    column.Item().Background(dark).PaddingVertical(12).PaddingHorizontal(16).Row(row =>
                    {
                        row.RelativeItem().Column(title =>
                        {
                            title.Item().Text("EVENT TICKET").FontSize(8).FontColor("#AFC4D6").LetterSpacing(0.15f);
                            title.Item().Text(ticket.EventName).FontSize(18).Bold().FontColor(Colors.White);
                        });
                        row.AutoItem().AlignMiddle().Text(ticket.Category.ToUpperInvariant()).FontSize(9).FontColor(Colors.White);
                    });

                    // Picture, details and QR code
                    column.Item().Padding(16).Row(row =>
                    {
                        if (image != null)
                        {
                            row.ConstantItem(150).Image(image).FitArea();
                            row.ConstantItem(16);
                        }

                        row.RelativeItem().Column(details =>
                        {
                            details.Spacing(7);
                            Field(details, "Date", ticket.EventDate.ToString("dddd, MMMM d, yyyy", English));
                            Field(details, "Time", ticket.EventDate.ToString("h:mm tt", English));
                            Field(details, "Venue", ticket.Venue);
                            Field(details, "Ticket holder", ticket.HolderName);
                            Field(details, "Price", ticket.Price.ToString("C0", English));
                        });

                        row.ConstantItem(16);
                        row.ConstantItem(130).Column(code =>
                        {
                            code.Item().Image(qr).FitArea();
                            code.Item().PaddingTop(4).AlignCenter().Text(ticket.TicketCode).FontSize(7.5f).SemiBold();
                        });
                    });

                    // Footer
                    column.Item().BorderTop(1).BorderColor(border).PaddingVertical(8).PaddingHorizontal(16).Row(row =>
                    {
                        row.RelativeItem().Text("Show this QR code at the entrance. Valid for one person.").FontSize(8).FontColor(muted);
                        row.AutoItem().Text($"Booked {ticket.BookedOnUtc.ToString("MMM d, yyyy HH:mm", English)} UTC").FontSize(8).FontColor(muted);
                    });
                });
            })).GeneratePdf();

            static void Field(ColumnDescriptor column, string label, string value) => column.Item().Column(field =>
            {
                field.Item().Text(label.ToUpperInvariant()).FontSize(7).FontColor(muted).LetterSpacing(0.1f);
                field.Item().Text(value).FontSize(11).SemiBold();
            });
        }

        /// <summary>Calendar file (iCalendar, RFC 5545) that adds the event to Google, Outlook or Apple Calendar.</summary>
        public byte[] CreateCalendarFile(TicketInfo ticket)
        {
            var lines = new List<string>
            {
                "BEGIN:VCALENDAR",
                "VERSION:2.0",
                "PRODID:-//EventHub//Tickets//EN",
                "CALSCALE:GREGORIAN",
                "METHOD:PUBLISH",
                "BEGIN:VEVENT",
                $"UID:booking-{ticket.BookingId}@eventhub",
                $"DTSTAMP:{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}",
                // Event times are stored without a time zone, so they are written as local ("floating") times.
                $"DTSTART:{ticket.EventDate:yyyyMMdd'T'HHmmss}",
                $"DTEND:{ticket.EventDate.Add(AssumedEventLength):yyyyMMdd'T'HHmmss}",
                $"SUMMARY:{Escape(ticket.EventName)}",
                $"LOCATION:{Escape(ticket.Venue)}",
                $"DESCRIPTION:{Escape($"Ticket {ticket.TicketCode} for {ticket.HolderName}.\nShow the QR code from your ticket email at the entrance.\nCheck your ticket: {ticket.VerifyUrl}")}",
                $"URL:{ticket.VerifyUrl}",
                "STATUS:CONFIRMED",
                "BEGIN:VALARM",
                "ACTION:DISPLAY",
                "TRIGGER:-P1D",
                $"DESCRIPTION:{Escape($"Tomorrow: {ticket.EventName}")}",
                "END:VALARM",
                "END:VEVENT",
                "END:VCALENDAR"
            };

            var text = new StringBuilder();
            foreach (var line in lines)
            {
                text.Append(Fold(line)).Append("\r\n"); // iCalendar uses CRLF line endings
            }
            return Encoding.UTF8.GetBytes(text.ToString());

            static string Escape(string value) => value
                .Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,")
                .Replace("\r\n", "\\n").Replace("\n", "\\n");

            // Lines longer than 75 bytes are continued on the next line, which starts with a space.
            static string Fold(string line)
            {
                var result = new StringBuilder();
                var count = 0;
                foreach (var ch in line)
                {
                    var size = Encoding.UTF8.GetByteCount(ch.ToString());
                    if (count + size > 75)
                    {
                        result.Append("\r\n ");
                        count = 1;
                    }
                    result.Append(ch);
                    count += size;
                }
                return result.ToString();
            }
        }
    }
}
