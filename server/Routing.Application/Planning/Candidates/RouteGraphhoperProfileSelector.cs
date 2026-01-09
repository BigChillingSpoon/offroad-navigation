using Routing.Application.Planning.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Application.Planning.Candidates
{
    public static class RouteGraphhoperProfileSelector
    {
        //from profile to graphhoper profile string
        public static string ToGraphhoperProfile(this UserRoutingProfile profile)
        {
            //here add profile mapping logic, such as allow private roads, allow gates...
            return "car"; // todo create custom profiles
        }
    }
}
