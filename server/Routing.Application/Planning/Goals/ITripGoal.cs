using Routing.Application.Planning.Intents;
using Routing.Application.Planning.State;

namespace Routing.Application.Planning.Goals
{
    //ukazuje planneru kdy ma skoncit planovani
    //vyhodnocuje konec na zaklade plannerstate a rozhoduje zda je cil dosazen
    public interface ITripGoal<in TIntent>
    where TIntent : ITripIntent
    {
        /// <summary>
        /// Is goal satisfied? If yes returns true, otherwise false. 
        /// </summary>
        bool IsSatisfied(PlannerState state, TIntent intent);

        /// <summary>
        /// how close we are to our goal (heuristics).
        /// higher = better.
        /// </summary>
        double GetGoalScore(PlannerState state, TIntent intent);
    }
}


