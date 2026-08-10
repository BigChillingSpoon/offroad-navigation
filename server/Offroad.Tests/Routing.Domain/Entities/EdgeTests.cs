using Routing.Domain.Entities;

namespace Offroad.Tests.Routing.Domain.Entities;

public class EdgeTests
{
    private static Edge EdgeWith(bool isOffroad = false, string? highway = null, double lengthMeters = 1000) =>
        new(1, 10, 20, lengthMeters, hasBarrier: false, hasNoEntry: false, isRestricted: false,
            elevationGainMeters: 0, isOffroad: isOffroad, geometry: null, grade: 0, highway: highway);

    [Theory]
    [InlineData("track", true)]
    [InlineData("TRACK", true)]   // OSM tags can vary in case; match must be case-insensitive.
    [InlineData("Track", true)]
    [InlineData("unclassified", false)]
    [InlineData("service", false)]
    [InlineData("", false)]        // unknown highway
    public void IsTrack_MatchesTrackRoadClassCaseInsensitively(string highway, bool expected)
    {
        Assert.Equal(expected, EdgeWith(highway: highway).IsTrack);
    }

    [Fact]
    public void IsTrack_IsFalse_WhenHighwayUnknown()
    {
        Assert.False(EdgeWith(highway: null).IsTrack);
    }

    // IsTrack and IsOffroad are deliberately independent: neither implies the other. These are the two
    // corner cases that would break if the code ever collapsed one into the other.
    [Fact]
    public void IsTrack_IsIndependentOfIsOffroad()
    {
        // A paved / grade1 track is a track but NOT offroad (see is_offroad rule in build_routing_graph.sh).
        var pavedTrack = EdgeWith(isOffroad: false, highway: "track");
        Assert.True(pavedTrack.IsTrack);
        Assert.False(pavedTrack.IsOffroad);

        // A gravel unclassified road is offroad but NOT a track.
        var gravelRoad = EdgeWith(isOffroad: true, highway: "unclassified");
        Assert.False(gravelRoad.IsTrack);
        Assert.True(gravelRoad.IsOffroad);
    }

    [Fact]
    public void OffroadLengthMeters_EqualsLength_WhenOffroad()
    {
        Assert.Equal(1000, EdgeWith(isOffroad: true, lengthMeters: 1000).OffroadLengthMeters);
    }

    [Fact]
    public void OffroadLengthMeters_IsZero_WhenNotOffroad()
    {
        Assert.Equal(0, EdgeWith(isOffroad: false, lengthMeters: 1000).OffroadLengthMeters);
    }
}
