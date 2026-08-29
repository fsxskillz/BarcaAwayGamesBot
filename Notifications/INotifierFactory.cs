using BarcaAwayTickets.Configuration;

namespace BarcaAwayTickets.Notifications;

public interface INotifierFactory
{
    INotifier Create(NotificationOptions options);
}
