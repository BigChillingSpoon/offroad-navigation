namespace Routing.Application.Planning.Candidates.Scoring
{
    public sealed class ScoringProfiles
    {
        public const string SectionName = "Scoring";

        public PenaltyWeights Route { get; set; } = new();
        public PenaltyWeights Loop { get; set; } = new();
    }

    public sealed class PenaltyWeights
    {
        public DetourWeights Detour { get; set; } = new();
        public RestrictionWeights Restrictions { get; set; } = new();
        public BarrierWeights Barriers { get; set; } = new();
    }

    public sealed class DetourWeights
    {
        public double MaxRatio { get; set; } = 0.3;
        public double StandardPenaltyRate { get; set; } = 50.0;
        public double ExcessiveBasePenalty { get; set; } = 15.0;
        public double ExcessiveRate { get; set; } = 200.0;
    }

    public sealed class RestrictionWeights
    {
        public double NationalPark { get; set; } = 500.0;
        public double NatureReserve { get; set; } = 400.0;
        public double NoAccess { get; set; } = 300.0;
        public double Private { get; set; } = 200.0;
        public double Forestry { get; set; } = 50.0;
        public double Agricultural { get; set; } = 50.0;
        public double Unknown { get; set; } = 30.0;
        public double Delivery { get; set; } = 20.0;
        public double Customers { get; set; } = 20.0;
        public double Destination { get; set; } = 10.0;
    }

    public sealed class BarrierWeights
    {
        public double Gate { get; set; } = 150.0;
        public double LiftGate { get; set; } = 100.0;
        public double SwingGate { get; set; } = 100.0;
        public double Chain { get; set; } = 80.0;
        public double Block { get; set; } = 80.0;
        public double Bollard { get; set; } = 30.0;
    }
}
