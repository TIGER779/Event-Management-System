using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AreebTechnologyTask.Services.Email
{
    /// <summary>
    /// Sends emails with MailKit, Microsoft's recommended library for SMTP in .NET.
    /// The "Email:Mode" setting picks where messages go (see <see cref="EmailOptions"/>).
    /// </summary>
    public class MailKitEmailSender : IEmailSender
    {
        private readonly EmailOptions _options;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<MailKitEmailSender> _logger;

        public MailKitEmailSender(IOptions<EmailOptions> options, IWebHostEnvironment environment, ILogger<MailKitEmailSender> logger)
        {
            _options = options.Value;
            _environment = environment;
            _logger = logger;
        }

        public async Task SendAsync(MimeMessage message, CancellationToken cancellationToken = default)
        {
            if (message.From.Count == 0)
            {
                message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
            }

            switch (_options.Mode.Trim().ToLowerInvariant())
            {
                case "disabled":
                    _logger.LogInformation("Email is disabled; not sending \"{Subject}\" to {To}.", message.Subject, message.To);
                    return;

                case "pickupdirectory":
                    await SaveToPickupDirectoryAsync(message, cancellationToken);
                    return;

                default:
                    await SendWithSmtpAsync(message, cancellationToken);
                    return;
            }
        }

        private async Task SendWithSmtpAsync(MimeMessage message, CancellationToken cancellationToken)
        {
            var smtp = _options.Smtp;
            if (!Enum.TryParse<SecureSocketOptions>(smtp.Security, ignoreCase: true, out var security))
            {
                security = SecureSocketOptions.Auto;
            }

            using var client = new SmtpClient();
            await client.ConnectAsync(smtp.Host, smtp.Port, security, cancellationToken);
            if (!string.IsNullOrEmpty(smtp.Username))
            {
                await client.AuthenticateAsync(smtp.Username, smtp.Password ?? string.Empty, cancellationToken);
            }
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);

            _logger.LogInformation("Sent \"{Subject}\" to {To} via {Host}:{Port}.", message.Subject, message.To, smtp.Host, smtp.Port);
        }

        private async Task SaveToPickupDirectoryAsync(MimeMessage message, CancellationToken cancellationToken)
        {
            var folder = Path.IsPathRooted(_options.PickupDirectory)
                ? _options.PickupDirectory
                : Path.Combine(_environment.ContentRootPath, _options.PickupDirectory);
            Directory.CreateDirectory(folder);

            var file = Path.Combine(folder, $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.eml");
            await message.WriteToAsync(file, cancellationToken);

            _logger.LogInformation("Saved \"{Subject}\" for {To} to {File}.", message.Subject, message.To, file);
        }
    }
}
