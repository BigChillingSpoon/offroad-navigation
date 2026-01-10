using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Infrastructure.GraphHopper
{
    public sealed class GraphHopperOptions
    {
        public const string SectionName = "GraphHopper";

        [Required]
        [Url]
        public string BaseUrl { get; set; } = "http://localhost:8989";
        public string? ApiKey { get; set; }
        public bool Instructions { get; set; } = false;
        public bool CalcPoints { get; set; } = true;
        public bool PointsEncoded { get; set; } = true;
    }
}
