namespace Routing.Application.Planning.Planner
{
    //konstanty pro planner, nemmene ale udavajici smer planeru jako takoveho
    public sealed class PlannerSettings
    {
        public int MaxIterations => 100;
        public int DefaultStepDistanceMeters => 100;
    }
}
