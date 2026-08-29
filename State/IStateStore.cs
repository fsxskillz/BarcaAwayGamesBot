using BarcaAwayTickets.Models;

namespace BarcaAwayTickets.State;

public interface IStateStore
{
    Task<IReadOnlyList<MatchInfo>> LoadAsync(CancellationToken cancellationToken = default);
    IReadOnlyList<MatchInfo> GetNewMatches(IEnumerable<MatchInfo> currentMatches, IEnumerable<MatchInfo> knownMatches);
    IReadOnlyList<MatchInfo> MergeMatches(IEnumerable<MatchInfo> currentMatches, IEnumerable<MatchInfo> knownMatches);
    Task SaveAsync(IEnumerable<MatchInfo> matches, CancellationToken cancellationToken = default);
}
