using BarcaAwayTickets.Models;

namespace BarcaAwayTickets.Scraping;

public interface IBarcaScraper
{
    Task<IReadOnlyList<MatchInfo>> ScrapeAsync(CancellationToken cancellationToken = default);
}
