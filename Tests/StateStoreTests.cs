using BarcaAwayTickets.Models;
using BarcaAwayTickets.State;
using Xunit;

namespace BarcaAwayTickets.Tests;

public sealed class StateStoreTests
{
    [Fact]
    public void GetNewMatches_UsesStableIdAndRemovesDuplicatesFromCurrentResult()
    {
        var store = new StateStore(Path.Combine(Path.GetTempPath(), $"barca-state-{Guid.NewGuid():N}.json"));
        var known = new[] { Match("https://tickets.example.test/213", "Old title") };
        var current = new[]
        {
            Match("https://tickets.example.test/213", "Renamed title"),
            Match("https://tickets.example.test/214", "New match"),
            Match("https://tickets.example.test/214", "Duplicate result")
        };

        var match = Assert.Single(store.GetNewMatches(current, known));
        Assert.Equal("https://tickets.example.test/214", match.Id);
        Assert.Equal("New match", match.Name);
    }

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsState()
    {
        var path = Path.Combine(Path.GetTempPath(), $"barca-state-{Guid.NewGuid():N}.json");
        var store = new StateStore(path);
        var expected = Match("https://tickets.example.test/213", "FC Barcelona - Valencia");
        try
        {
            await store.SaveAsync([expected]);
            Assert.Equal([expected], await store.LoadAsync());
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void MergeMatches_RefreshesClosingFormWithoutChangingFirstSeenAt()
    {
        var store = new StateStore(Path.Combine(Path.GetTempPath(), $"barca-state-{Guid.NewGuid():N}.json"));
        var firstSeenAt = DateTimeOffset.Parse("2026-08-29T00:00:00Z");
        var known = new MatchInfo("https://tickets.example.test/213", "Valencia - FC Barcelona", null, null, "https://tickets.example.test/213", firstSeenAt);
        var current = known with { ClosingForm = "25 August, 11.59 PM", FirstSeenAt = firstSeenAt.AddDays(1) };

        var merged = Assert.Single(store.MergeMatches([current], [known]));

        Assert.Equal("25 August, 11.59 PM", merged.ClosingForm);
        Assert.Equal(firstSeenAt, merged.FirstSeenAt);
    }

    private static MatchInfo Match(string id, string name) => new(id, name, null, null, id, DateTimeOffset.Parse("2026-08-29T00:00:00Z"));
}
