namespace BlockmapFramework
{
    /// <summary>
    /// Extension class allowing executing logic on classes implementing IClimbable.
    /// </summary>
    public static class ClimbableExtensions
    {
        public static float GetClimbCostUp(this IClimbable climbable, Entity e)
        {
            float cost = climbable.ClimbCostUp;
            if (e != null) cost *= (1f / e.ClimbingAptitude);
            return cost;
        }

        public static float GetClimbCostDown(this IClimbable climbable, Entity e)
        {
            float cost = climbable.ClimbCostDown;
            if (e != null) cost *= (1f / e.ClimbingAptitude);
            return cost;
        }

        public static float GetClimbSpeedUp(this IClimbable climbable, Entity e)
            => 1f / climbable.GetClimbCostUp(e);

        public static float GetClimbSpeedDown(this IClimbable climbable, Entity e)
            => 1f / climbable.GetClimbCostDown(e);
    }
}