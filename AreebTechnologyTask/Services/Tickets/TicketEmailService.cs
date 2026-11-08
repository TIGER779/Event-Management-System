using System.Globalization;
using AreebTechnologyTask.Data;
using AreebTechnologyTask.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;
using static System.Net.WebUtility;

namespace AreebTechnologyTask.Services.Tickets
{
    /// <summary>Builds and sends the booking confirmation email with the ticket attached.</summary>
    public class TicketEmailService
    {
        private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

        private readonly AppDbContext _db;
        private readonly TicketCodeService _codes;
        private readonly TicketDocumentService _documents;
        private readonly IEmailSender _sender;
        private readonly EmailOptions _options;
        private readonly ILogger<TicketEmailService> _logger;

        public TicketEmailService(AppDbContext db, TicketCodeService codes, TicketDocumentService documents,
            IEmailSender sender, IOptions<EmailOptions> options, ILogger<TicketEmailService> logger)
        {
            _db = db;
            _codes = codes;
            _documents = documents;
            _sender = sender;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>Sends the ticket email for a booking. Returns false if the booking no longer exists.</summary>
        public async Task<bool> SendTicketAsync(int bookingId, CancellationToken cancellationToken = default)
        {
            var booking = await _db.Bookings
                .Include(b => b.User)
                .Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
            if (booking == null)
            {
                _logger.LogWarning("Booking {BookingId} not found (cancelled?); no ticket email sent.", bookingId);
                return false;
            }

            var ticket = TicketInfo.From(booking, _codes, _options.PublicBaseUrl);
            await _sender.SendAsync(BuildMessage(ticket), cancellationToken);
            return true;
        }

        private MimeMessage BuildMessage(TicketInfo ticket)
        {
            var message = new MimeMessage();
            message.To.Add(new MailboxAddress(ticket.HolderName, ticket.HolderEmail));
            message.Subject = $"Your ticket for {ticket.EventName}";

            var body = new BodyBuilder();

            // Pictures shown inside the email are embedded ("cid:" links), so they display without loading
            // anything from the internet.
            var qr = body.LinkedResources.Add("ticket-qr.png", _documents.CreateQrPng(ticket, 8), new ContentType("image", "png"));
            qr.ContentId = MimeUtils.GenerateMessageId();

            string? imageSrc = null;
            var image = _documents.LoadEventImage(ticket);
            if (image is { } local)
            {
                var picture = body.LinkedResources.Add(local.FileName, local.Bytes, ContentType.Parse(MimeTypes.GetMimeType(local.FileName)));
                picture.ContentId = MimeUtils.GenerateMessageId();
                imageSrc = "cid:" + picture.ContentId;
            }
            else if (ticket.ImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                imageSrc = ticket.ImageUrl;
            }

            body.Attachments.Add($"ticket-{ticket.TicketCode}.pdf", _documents.CreatePdf(ticket), new ContentType("application", "pdf"));
            body.Attachments.Add("add-to-calendar.ics", _documents.CreateCalendarFile(ticket),
                ContentType.Parse("text/calendar; charset=utf-8; method=PUBLISH"));

            body.HtmlBody = BuildHtml(ticket, "cid:" + qr.ContentId, imageSrc);
            body.TextBody = BuildText(ticket);
            message.Body = body.ToMessageBody();
            return message;
        }

        private string MyBookingsUrl => $"{_options.PublicBaseUrl.TrimEnd('/')}/Booking/MyBookings";

        private string BuildText(TicketInfo t) => $"""
            Hi {t.HolderName},

            Your booking is confirmed. Here is your ticket for {t.EventName}.

            Date:      {t.EventDate.ToString("dddd, MMMM d, yyyy", English)}
            Time:      {t.EventDate.ToString("h:mm tt", English)}
            Venue:     {t.Venue}
            Price:     {t.Price.ToString("C0", English)}
            Ticket ID: {t.TicketCode}

            Your ticket is attached as a PDF. Show its QR code at the entrance.
            Open the attached .ics file to add the event to your calendar.

            Check your ticket: {t.VerifyUrl}
            Your bookings: {MyBookingsUrl}

            Event Booking
            """;

        // Emails use tables and inline styles, because many email apps (Gmail, Outlook) ignore <style> blocks.
        private string BuildHtml(TicketInfo t, string qrSrc, string? imageSrc)
        {
            string Row(string label, string value) => $"""
                <tr>
                  <td style="padding:6px 0;color:#6C757D;font-size:13px;width:110px;">{HtmlEncode(label)}</td>
                  <td style="padding:6px 0;color:#2C3E50;font-size:15px;font-weight:bold;">{HtmlEncode(value)}</td>
                </tr>
                """;

            var picture = imageSrc == null ? "" : $"""
                <tr><td><img src="{HtmlEncode(imageSrc)}" width="600" alt="{HtmlEncode(t.EventName)}" style="display:block;width:100%;max-width:600px;height:auto;border:0;"></td></tr>
                """;

            return $"""
                <!DOCTYPE html>
                <html lang="en">
                <head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Your ticket</title></head>
                <body style="margin:0;padding:0;background:#F3F5F7;font-family:Segoe UI,Arial,Helvetica,sans-serif;">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#F3F5F7;padding:24px 12px;">
                    <tr><td align="center">
                      <table role="presentation" width="600" cellpadding="0" cellspacing="0" style="width:100%;max-width:600px;background:#FFFFFF;border-radius:8px;overflow:hidden;border:1px solid #E1E6EB;">
                        <tr><td style="background:#2C3E50;padding:22px 28px;">
                          <div style="color:#AFC4D6;font-size:12px;letter-spacing:2px;">BOOKING CONFIRMED</div>
                          <div style="color:#FFFFFF;font-size:24px;font-weight:bold;margin-top:4px;">{HtmlEncode(t.EventName)}</div>
                        </td></tr>
                        {picture}
                        <tr><td style="padding:24px 28px 8px 28px;color:#2C3E50;font-size:15px;line-height:1.5;">
                          <p style="margin:0 0 12px 0;">Hi {HtmlEncode(t.HolderName)},</p>
                          <p style="margin:0;">You're going to <strong>{HtmlEncode(t.EventName)}</strong>! Your ticket is attached as a PDF,
                             and you can also show the QR code below at the entrance.</p>
                        </td></tr>
                        <tr><td style="padding:8px 28px;">
                          <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                            {Row("Date", t.EventDate.ToString("dddd, MMMM d, yyyy", English))}
                            {Row("Time", t.EventDate.ToString("h:mm tt", English))}
                            {Row("Venue", t.Venue)}
                            {Row("Price", t.Price.ToString("C0", English))}
                            {Row("Ticket ID", t.TicketCode)}
                          </table>
                        </td></tr>
                        <tr><td align="center" style="padding:16px 28px;">
                          <img src="{qrSrc}" width="200" height="200" alt="Ticket QR code {HtmlEncode(t.TicketCode)}" style="display:block;border:1px solid #E1E6EB;border-radius:6px;">
                          <div style="color:#6C757D;font-size:12px;margin-top:8px;">Scan at the entrance &middot; valid for one person</div>
                        </td></tr>
                        <tr><td align="center" style="padding:8px 28px 24px 28px;">
                          <a href="{HtmlEncode(MyBookingsUrl)}" style="display:inline-block;background:#1F6FA8;color:#FFFFFF;text-decoration:none;font-weight:bold;padding:12px 24px;border-radius:6px;">View my bookings</a>
                          <div style="color:#6C757D;font-size:12px;margin-top:12px;">Open the attached <strong>add-to-calendar.ics</strong> file to add this event to your calendar.</div>
                        </td></tr>
                        <tr><td style="background:#F8F9FA;padding:14px 28px;color:#6C757D;font-size:12px;border-top:1px solid #E1E6EB;">
                          You received this email because you booked a ticket on Event Booking.
                          <a href="{HtmlEncode(t.VerifyUrl)}" style="color:#1F6FA8;">Check this ticket</a>
                        </td></tr>
                      </table>
                    </td></tr>
                  </table>
                </body>
                </html>
                """;
        }
    }
}
