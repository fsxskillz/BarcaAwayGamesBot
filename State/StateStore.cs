using System.Text.Json;
using BarcaAwayTickets.Models;

namespace BarcaAwayTickets.State;

public sealed class StateStore(string path) : IStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
    public async Task<IReadOnlyList<MatchInfo>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path)) { Console.WriteLine("state.json does not exist yet; starting with an empty state."); return []; }
        await using var stream = File.OpenRead(path);
        return (await JsonSerializer.DeserializeAsync<StateDocument>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("state.json is empty or invalid.")).Matches;
    }
    public IReadOnlyList<MatchInfo> GetNewMatches(IEnumerable<MatchInfo> currentMatches, IEnumerable<MatchInfo> knownMatches)
    {
        var knownIds = knownMatches.Select(match => match.Id).ToHashSet(StringComparer.Ordinal);
        return currentMatches.Where(match => knownIds.Add(match.Id)).ToList();
    }
    public IReadOnlyList<MatchInfo> MergeMatches(IEnumerable<MatchInfo> currentMatches, IEnumerable<MatchInfo> knownMatches)
    {
        var known = knownMatches.ToList();
        var currentById = currentMatches
            .GroupBy(match => match.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var merged = known.Select(knownMatch => currentById.TryGetValue(knownMatch.Id, out var currentMatch)
            ? currentMatch with { FirstSeenAt = knownMatch.FirstSeenAt }
            : knownMatch).ToList();
        var knownIds = known.Select(match => match.Id).ToHashSet(StringComparer.Ordinal);
        merged.AddRange(currentById.Values.Where(match => knownIds.Add(match.Id)));
        return merged;
    }
    public async Task SaveAsync(IEnumerable<MatchInfo> matches, CancellationToken cancellationToken = default)
    {
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, new StateDocument { Matches = matches.ToList() }, JsonOptions, cancellationToken);
        await stream.WriteAsync("\n"u8.ToArray(), cancellationToken);
    }
    private sealed class StateDocument { public List<MatchInfo> Matches { get; init; } = []; }
}
