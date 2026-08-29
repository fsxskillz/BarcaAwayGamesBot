namespace BarcaAwayTickets.Models;

public sealed record MatchInfo(
    string Id,
    string Name,
    string? Date,
    string? ClosingForm,
    string TicketUrl,
    DateTimeOffset FirstSeenAt);
