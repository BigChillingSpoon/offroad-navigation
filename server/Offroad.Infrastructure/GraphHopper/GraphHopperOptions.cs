using System.ComponentModel.DataAnnotations;

namespace Routing.Infrastructure.GraphHopper
{
    public sealed class GraphHopperOptions
    {
        public string SectionName => "GraphHopper";

        [Required]
        [Url]
        public string BaseUrl => "http://localhost:8989";
        public string? ApiKey { get; set; }
        public bool Instructions => false;
        public bool CalcPoints => true;
        public bool PointsEncoded => true;
        public  bool Elevation => true;
        public string[] RequestedDetails  => new[]{ "road_class", "surface" };
    }
}

