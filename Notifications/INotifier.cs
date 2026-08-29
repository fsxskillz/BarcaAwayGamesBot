using BarcaAwayTickets.Models;

namespace BarcaAwayTickets.Notifications;

public interface INotifier
{
    Task NotifyAsync(MatchInfo match, CancellationToken cancellationToken = default);
}
