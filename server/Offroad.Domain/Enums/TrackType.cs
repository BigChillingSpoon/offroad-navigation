using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Domain.Enums
{
    /// <summary>
    /// Represents the OpenStreetMap 'tracktype' tag, which defines the firmness and usability of a track.
    /// Used heavily to determine if a road segment is a genuine offroad path.
    /// </summary>
    public enum TrackType
    {
        /// <summary>
        /// Missing or unknown tracktype data in OSM.
        /// </summary>
        UNKNOWN,

        /// <summary>
        /// Solid, paved, or heavily compacted surface (e.g., asphalt, concrete, solid paving). 
        /// Smooth driving, essentially a standard road. Not considered offroad.
        /// </summary>
        GRADE1,

        /// <summary>
        /// Mostly solid with some soft parts (e.g., gravel or macadam with a grassy center). 
        /// Easily passable by regular cars.
        /// </summary>
        GRADE2,

        /// <summary>
        /// Even mix of hard and soft materials (e.g., dirt with stones, shallow ruts). 
        /// A typical forest or agricultural road. Passable by SUVs or slowly by regular cars.
        /// </summary>
        GRADE3,

        /// <summary>
        /// Mostly soft material (e.g., deep soil, mud, tractor tracks, high grass). 
        /// Requires high ground clearance or 4x4. Typical offroad.
        /// </summary>
        GRADE4,

        /// <summary>
        /// Completely soft and unpaved (e.g., deep mud, loose sand, extreme ruts, swamps). 
        /// Hardcore offroad/enduro territory. Impassable for standard vehicles.
        /// </summary>
        GRADE5
    }
}
