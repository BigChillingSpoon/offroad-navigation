using FluentAssertions;
using Routing.Application.Contracts.Responses;
using Routing.Application.Mappings;
using Routing.Application.Planning.Intents;
using Routing.Domain.Enums;
using Routing.Domain.ValueObjects;

namespace Offroad.Tests.Routing.Application.Mappings;

/// <summary>
/// Unit tests for PolicyViolationDetector's last-mile tolerance logic.
///
/// Coordinate geometry reference (at latitude ~50°):
///   0.001° latitude  ≈ 111 m
///   0.0003° latitude ≈  33 m
///
/// The detector's tolerance threshold is 50 m.
/// </summary>
public sealed class PolicyViolationDetectorTests
{
    // ---------------------------------------------------------------
    // TEST 1 — PointEvent barrier MORE than 50m from destination → violation
    //
    //   P0 ──111m── P1 ──111m── P2 ──111m── P3
    //                ▲ barrier (PointIndex=1)
    //                distance to end: P1→P2→P3 ≈ 222m >> 50m
    // ---------------------------------------------------------------
    [Fact]
    public void Detect_PointBarrierBeyond50m_ReturnsGatesViolation()
    {
        // Arrange — 4 points, ~111m apart each
        var polyline = new List<Coordinate>
        {
            new(50.0800, 14.42),
            new(50.0810, 14.42),  // barrier here — 222m from end
            new(50.0820, 14.42),
            new(50.0830, 14.42)
        };

        var events = new List<TripEvent>
        {
            new PointEvent
            {
                Type = TripEventType.Barrier,
                SubType = nameof(BarrierType.LiftGate),
                PointIndex = 1,
                Coordinate = polyline[1]
            }
        };

        var intent = MakeIntent(allowGates: false, allowPrivateRoads: true);

        // Act
        var violations = PolicyViolationDetector.Detect(events, intent, polyline);

        // Assert
        violations.Should().ContainSingle()
            .Which.Should().Be(PolicyViolationType.Gates);
    }

    // ---------------------------------------------------------------
    // TEST 2 — PointEvent barrier LESS than 50m from destination → no violation
    //
    //   P0 ──111m── P1 ──111m── P2 ──33m── P3
    //                             ▲ barrier (PointIndex=2)
    //                             distance to end: P2→P3 ≈ 33m < 50m
    // ---------------------------------------------------------------
    [Fact]
    public void Detect_PointBarrierWithin50m_ReturnsNoViolation()
    {
        // Arrange — last segment is only ~33m (0.0003° lat)
        var polyline = new List<Coordinate>
        {
            new(50.0800, 14.42),
            new(50.0810, 14.42),
            new(50.0820, 14.42),  // barrier here — only 33m from end
            new(50.0823, 14.42)   // destination (+0.0003° ≈ 33m)
        };

        var events = new List<TripEvent>
        {
            new PointEvent
            {
                Type = TripEventType.Barrier,
                SubType = nameof(BarrierType.Gate),
                PointIndex = 2,
                Coordinate = polyline[2]
            }
        };

        var intent = MakeIntent(allowGates: false, allowPrivateRoads: true);

        // Act
        var violations = PolicyViolationDetector.Detect(events, intent, polyline);

        // Assert — barrier is within last-mile tolerance, forgiven
        violations.Should().BeEmpty();
    }

    // ---------------------------------------------------------------
    // TEST 3 — IntervalEvent restriction ends MORE than 50m from destination → violation
    //
    //   P0 ──111m── P1 ──111m── P2 ──111m── P3
    //   |←── private ──→|
    //   IntervalEvent ToIndex=1, distance to end: P1→P2→P3 ≈ 222m >> 50m
    // ---------------------------------------------------------------
    [Fact]
    public void Detect_IntervalRestrictionEndsBeyond50m_ReturnsRestrictedAreaViolation()
    {
        // Arrange — 4 points, ~111m apart
        var polyline = new List<Coordinate>
        {
            new(50.0800, 14.42),
            new(50.0810, 14.42),  // restriction ends here — 222m from end
            new(50.0820, 14.42),
            new(50.0830, 14.42)
        };

        var events = new List<TripEvent>
        {
            new IntervalEvent
            {
                Type = TripEventType.Restriction,
                SubType = nameof(RestrictionType.Private),
                FromIndex = 0,
                ToIndex = 1
            }
        };

        var intent = MakeIntent(allowGates: true, allowPrivateRoads: false);

        // Act
        var violations = PolicyViolationDetector.Detect(events, intent, polyline);

        // Assert
        violations.Should().ContainSingle()
            .Which.Should().Be(PolicyViolationType.RestrictedArea);
    }

    // ---------------------------------------------------------------
    // TEST 4 — IntervalEvent restriction ends LESS than 50m from destination → no violation
    //
    //   P0 ──111m── P1 ──111m── P2 ──33m── P3
    //                    |←── private ──→|
    //   IntervalEvent ToIndex=2, distance to end: P2→P3 ≈ 33m < 50m
    // ---------------------------------------------------------------
    [Fact]
    public void Detect_IntervalRestrictionEndsWithin50m_ReturnsNoViolation()
    {
        // Arrange — last segment only ~33m
        var polyline = new List<Coordinate>
        {
            new(50.0800, 14.42),
            new(50.0810, 14.42),
            new(50.0820, 14.42),  // restriction ends here — 33m from end
            new(50.0823, 14.42)   // destination (+0.0003° ≈ 33m)
        };

        var events = new List<TripEvent>
        {
            new IntervalEvent
            {
                Type = TripEventType.Restriction,
                SubType = nameof(RestrictionType.Private),
                FromIndex = 1,
                ToIndex = 2
            }
        };

        var intent = MakeIntent(allowGates: true, allowPrivateRoads: false);

        // Act
        var violations = PolicyViolationDetector.Detect(events, intent, polyline);

        // Assert — restriction is within last-mile tolerance, forgiven
        violations.Should().BeEmpty();
    }

    // ---------------------------------------------------------------
    // Helper
    // ---------------------------------------------------------------
    private static RouteIntent MakeIntent(bool allowGates, bool allowPrivateRoads) => new()
    {
        Start = new Coordinate(50.08, 14.42),
        End = new Coordinate(50.09, 14.42),
        Balance = RouteBalance.Balanced,
        AllowGates = allowGates,
        AllowPrivateRoads = allowPrivateRoads
    };
}
