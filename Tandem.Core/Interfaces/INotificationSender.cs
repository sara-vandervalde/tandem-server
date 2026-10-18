namespace Tandem.Core.Interfaces;

// Sends a notification to a user. Services depend only on this interface,
// so the channel (log, email, SMS) can be replaced without changing any service
public interface INotificationSender
{
    Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken);
}
