using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Offroad.IntegrationTests.Infrastructure;
using Routing.Application.Contracts.Models;
using Routing.Application.Planning.Encoding;
using Routing.Domain.Enums;
using Routing.Domain.ValueObjects;

namespace Offroad.IntegrationTests;

/// <summary>
/// Integration tests that exercise the full POST /api/routes/plan pipeline —
/// from Controller through services, Polly resilience, custom-model generation,
/// GH response mapping, scoring, and DTO projection — with only the external
/// GraphHopper HTTP call replaced by a mock handler.
/// </summary>
public sealed class RoutePlanningTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RoutePlanningTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.GraphHopperHandler.Reset();
    }

    // ---------------------------------------------------------------
    // TEST 1 — End-to-End Success
    //   allowGates=true  →  mock GH returns a path with a LiftGate barrier
    //   Assert: 200 OK + events contain Barrier / LiftGate
    // ---------------------------------------------------------------
    [Fact]
    public async Task PlanRoute_GatesAllowed_ReturnsLiftGateBarrierEvent()
    {
        // Arrange
        var (polyline, pointCount) = BuildTestPolyline();

        var ghJson = GraphHopperResponseBuilder.Success(
            encodedPolyline: polyline,
            pointCount: pointCount,
            distance: 450.0,
            customBarrierJson: """[[0, 2, "none"], [2, 3, "lift_gate"], [3, 4, "none"]]""");

        _factory.GraphHopperHandler.SetupJsonResponse(ghJson);

        var request = new PlanRouteRequest
        {
            StartLatitude = 50.08,
            StartLongitude = 14.42,
            EndLatitude = 50.084,
            EndLongitude = 14.424,
            AllowGates = true,
            AllowPrivateRoads = true,
            RouteBalance = RouteBalance.Balanced
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/routes/plan", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var events = root.GetProperty("events").EnumerateArray().ToList();
        events.Should().ContainSingle(
            e => e.GetProperty("type").GetString() == "Barrier"
              && e.GetProperty("subType").GetString() == "LiftGate",
            "the mock GH response contained a lift_gate barrier interval, " +
            "so the mapped events must include a Barrier/LiftGate entry");

        root.GetProperty("metrics").GetProperty("totalDistanceMeters").GetDouble()
            .Should().BeGreaterThan(0, "a successful plan must report distance");
    }

    // ---------------------------------------------------------------
    // TEST 2 — Payload Validation
    //   allowGates=false  →  assert the outgoing GH request body
    //   contains custom_model.priority with "custom_barrier > 1"
    //   and multiply_by = 0.000001
    // ---------------------------------------------------------------
    [Fact]
    public async Task PlanRoute_GatesDisallowed_SendsBarrierPenaltyToGraphHopper()
    {
        // Arrange
        var (polyline, pointCount) = BuildTestPolyline();

        var ghJson = GraphHopperResponseBuilder.Success(
            encodedPolyline: polyline, pointCount: pointCount);

        _factory.GraphHopperHandler.SetupJsonResponse(ghJson);

        var request = new PlanRouteRequest
        {
            StartLatitude = 50.08,
            StartLongitude = 14.42,
            EndLatitude = 50.084,
            EndLongitude = 14.424,
            AllowGates = false,
            AllowPrivateRoads = true,
            RouteBalance = RouteBalance.Balanced
        };

        // Act
        await _client.PostAsJsonAsync("/api/routes/plan", request);

        // Assert — inspect the captured request that our mock handler received
        _factory.GraphHopperHandler.CapturedRequests.Should().NotBeEmpty(
            "the planning pipeline must call GraphHopper");

        var capturedBody = _factory.GraphHopperHandler.CapturedRequests[0].Body;
        capturedBody.Should().NotBeNullOrEmpty();

        using var doc = JsonDocument.Parse(capturedBody!);
        var priorities = doc.RootElement
            .GetProperty("custom_model")
            .GetProperty("priority");

        var barrierStatement = priorities.EnumerateArray()
            .FirstOrDefault(p =>
                p.GetProperty("if").GetString()!.Contains("custom_barrier > 1"));

        barrierStatement.ValueKind.Should().NotBe(JsonValueKind.Undefined,
            "when gates are disallowed, the custom model must penalise custom_barrier > 1");

        barrierStatement.GetProperty("multiply_by").GetDouble()
            .Should().Be(0.000001,
                "barrier penalty uses 0.000001 to force detour while avoiding " +
                "PointNotFoundException on artificial barrier edges (#18)");
    }

    // ---------------------------------------------------------------
    // TEST 3 — Polly Timeout E2E
    //   Mock GH never responds → Polly per-attempt + total timeout fire
    //   Assert: non-2xx error status from the controller
    // ---------------------------------------------------------------
    [Fact]
    public async Task PlanRoute_GraphHopperTimesOut_ReturnsServerError()
    {
        // Arrange — delay far exceeds Polly's calculated total timeout (~10s for nearby points)
        _factory.GraphHopperHandler.SetupDelay(TimeSpan.FromMinutes(5));

        var request = new PlanRouteRequest
        {
            StartLatitude = 50.08,
            StartLongitude = 14.42,
            EndLatitude = 50.084,
            EndLongitude = 14.424,
            AllowGates = true,
            AllowPrivateRoads = true,
            RouteBalance = RouteBalance.Balanced
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/routes/plan", request);

        // Assert
        // The Polly timeout fires → TimeoutRejectedException propagates.
        // Depending on how ResilienceHandler wraps the exception:
        //   • HttpRequestException → caught → RoutingProviderException(Unavailable) → 502
        //   • Unhandled → GlobalExceptionHandler → 500
        response.IsSuccessStatusCode.Should().BeFalse(
            "the request must fail when GraphHopper does not respond within the timeout");

        var status = (int)response.StatusCode;
        status.Should().BeOneOf(new[] { 500, 502, 504 },
            "expected a server-side error from timeout handling");
    }

    // ---------------------------------------------------------------
    // TEST 4 — Honest Routing: Gates disallowed but cage forces gate crossing
    //   allowGates=false, mock GH still returns a lift_gate
    //   Assert: 200 OK + policyViolations contains enum "Gates"
    // ---------------------------------------------------------------
    [Fact]
    public async Task PlanRoute_GatesDisallowedButRouteContainsGate_AddsPolicyViolation()
    {
        // Arrange — GH returns a gate despite the penalty (cage scenario)
        var (polyline, pointCount) = BuildTestPolyline();

        var ghJson = GraphHopperResponseBuilder.Success(
            encodedPolyline: polyline,
            pointCount: pointCount,
            distance: 450.0,
            customBarrierJson: """[[0, 2, "none"], [2, 3, "lift_gate"], [3, 4, "none"]]""");

        _factory.GraphHopperHandler.SetupJsonResponse(ghJson);

        var request = new PlanRouteRequest
        {
            StartLatitude = 50.08,
            StartLongitude = 14.42,
            EndLatitude = 50.084,
            EndLongitude = 14.424,
            AllowGates = false,
            AllowPrivateRoads = true,
            RouteBalance = RouteBalance.Balanced
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/routes/plan", request);

        // Assert — 200 OK, NOT 400: the user needs the route to escape the cage
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var violations = root.GetProperty("policyViolations").EnumerateArray()
            .Select(v => v.GetString())
            .ToList();

        violations.Should().ContainSingle(v => v == nameof(PolicyViolationType.Gates),
            "the response must include a typed Gates violation so the frontend can localise the warning");
    }

    // ---------------------------------------------------------------
    // TEST 5 — Honest Routing: Private roads disallowed but cage forces private road
    //   allowPrivateRoads=false, mock GH returns road_access = "private"
    //   Assert: 200 OK + policyViolations contains enum "PrivateRoads"
    // ---------------------------------------------------------------
    [Fact]
    public async Task PlanRoute_PrivateRoadsDisallowedButRouteContainsPrivate_AddsPolicyViolation()
    {
        // Arrange — GH returns a private road despite the penalty
        var (polyline, pointCount) = BuildTestPolyline();

        var ghJson = GraphHopperResponseBuilder.Success(
            encodedPolyline: polyline,
            pointCount: pointCount,
            distance: 450.0,
            roadAccessJson: """[[0, 2, "yes"], [2, 4, "private"]]""");

        _factory.GraphHopperHandler.SetupJsonResponse(ghJson);

        var request = new PlanRouteRequest
        {
            StartLatitude = 50.08,
            StartLongitude = 14.42,
            EndLatitude = 50.084,
            EndLongitude = 14.424,
            AllowGates = true,
            AllowPrivateRoads = false,
            RouteBalance = RouteBalance.Balanced
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/routes/plan", request);

        // Assert — 200 OK with a policy violation, not a rejection
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var violations = root.GetProperty("policyViolations").EnumerateArray()
            .Select(v => v.GetString())
            .ToList();

        violations.Should().ContainSingle(v => v == nameof(PolicyViolationType.PrivateRoads),
            "the response must include a typed PrivateRoads violation so the frontend can localise the warning");
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    /// <summary>
    /// Generates a 5-point encoded polyline around central Prague
    /// (well outside any national park polygon).
    /// </summary>
    private static (string EncodedPoints, int PointCount) BuildTestPolyline()
    {
        var coords = new List<Coordinate>
        {
            new(50.080, 14.420, 300),
            new(50.081, 14.421, 305),
            new(50.082, 14.422, 310),
            new(50.083, 14.423, 315),
            new(50.084, 14.424, 320)
        };

        var polyline = PolylineEncoder.Encode(coords, 1e5, 100.0, hasElevation: true);
        return (polyline.Points, coords.Count);
    }
}
