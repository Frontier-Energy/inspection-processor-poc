using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;

namespace InspectionProcessor.Services;

public interface IInspectionEmailSender
{
    Task SendAsync(InspectionEmailMessage message, CancellationToken cancellationToken);
}

public sealed class InspectionEmailSender : IInspectionEmailSender
{
    private readonly InspectionEmailOptions _options;

    public InspectionEmailSender(IOptions<InspectionEmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(InspectionEmailMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString) || string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            throw new InvalidOperationException(
                "Email settings are missing. Set InspectionEmail:ConnectionString and InspectionEmail:FromAddress.");
        }

        var emailClient = new EmailClient(_options.ConnectionString);
        var content = new EmailContent(message.Subject)
        {
            Html = message.HtmlBody
        };

        var recipients = new EmailRecipients([new EmailAddress(message.ToAddress)]);
        var emailMessage = new EmailMessage(_options.FromAddress, recipients, content);

        if (emailMessage.Attachments is not null)
        {
            foreach (var attachment in message.Attachments)
            {
                emailMessage.Attachments.Add(new EmailAttachment(
                    attachment.FileName,
                    attachment.ContentType,
                    BinaryData.FromBytes(attachment.Content)));
            }
        }

        await emailClient.SendAsync(WaitUntil.Completed, emailMessage, cancellationToken);
    }
}
