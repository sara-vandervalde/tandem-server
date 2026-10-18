using Microsoft.Extensions.Logging;
using Tandem.Core.Interfaces;

namespace Tandem.Data.Notifications;

// Development channel: nothing is sent, the notification is only written to the log.
// The body is not logged - it may contain personal details
public class LogNotificationSender : INotificationSender
{
    private readonly ILogger<LogNotificationSender> _logger;

    public LogNotificationSender(ILogger<LogNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Notification to {Recipient}: {Subject}", recipient, subject);
        return Task.CompletedTask;
    }
}
