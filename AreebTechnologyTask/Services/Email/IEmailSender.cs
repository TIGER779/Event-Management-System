using MimeKit;

namespace AreebTechnologyTask.Services.Email
{
    /// <summary>Sends an email message (implemented with MailKit).</summary>
    public interface IEmailSender
    {
        Task SendAsync(MimeMessage message, CancellationToken cancellationToken = default);
    }
}
