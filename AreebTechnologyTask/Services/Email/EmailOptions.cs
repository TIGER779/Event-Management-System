namespace AreebTechnologyTask.Services.Email
{
    /// <summary>
    /// Settings from the "Email" section of appsettings.json (or Email__... environment variables).
    /// </summary>
    public class EmailOptions
    {
        /// <summary>
        /// "Smtp" (send through an SMTP server such as Mailpit or Gmail),
        /// "PickupDirectory" (save each email as an .eml file instead of sending it), or "Disabled".
        /// </summary>
        public string Mode { get; set; } = "Smtp";

        public string FromAddress { get; set; } = "tickets@eventbooking.local";
        public string FromName { get; set; } = "Event Booking";

        /// <summary>The site's public address, used for links and QR codes in emails (no trailing slash).</summary>
        public string PublicBaseUrl { get; set; } = "https://localhost:5025";

        /// <summary>Folder for .eml files when Mode is "PickupDirectory" (relative to the app folder).</summary>
        public string PickupDirectory { get; set; } = "App_Data/emails";

        public SmtpOptions Smtp { get; set; } = new();
    }

    public class SmtpOptions
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 1025;

        /// <summary>"None" (Mailpit), "StartTls" (Gmail on port 587), "SslOnConnect" (port 465) or "Auto".</summary>
        public string Security { get; set; } = "None";

        /// <summary>Leave empty when the server needs no login (Mailpit). Keep real credentials in user-secrets.</summary>
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
