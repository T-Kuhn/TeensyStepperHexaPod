using UnityEngine;

namespace MachineSimulator.Controlling
{
    public static class TimeUntilNextImpact
    {
        /// <summary>
        /// Calculates the time (in seconds) until a vertically moving object reaches a given height.
        /// Returns null if there is no future intersection.
        /// </summary>
        /// <param name="currentHeight">Current vertical position (y₀)</param>
        /// <param name="currentVelocity">Current vertical velocity (v₀), positive = upward</param>
        /// <param name="targetHeight">Target height (e.g. plate height)</param>
        /// <param name="gravity">Gravity magnitude (positive, e.g. 9.81)</param>
        public static float? Calculate(
            float currentHeight,
            float currentVelocity,
            float targetHeight,
            float gravity = 9.81f)
        {
            // We solve:
            // y(t) = y0 + v0 * t - 0.5 * g * t^2 = targetHeight

            var dy = currentHeight - targetHeight;

            // Quadratic: -0.5*g*t^2 + v0*t + dy = 0
            // Discriminant:
            var discriminant = currentVelocity * currentVelocity + 2f * gravity * dy;

            if (discriminant < 0f)
            {
                // No real solution → never reaches target height
                return null;
            }

            var sqrtD = Mathf.Sqrt(discriminant);

            // Two solutions
            var t1 = (currentVelocity + sqrtD) / gravity;
            var t2 = (currentVelocity - sqrtD) / gravity;

            // We want the smallest positive time
            var t = float.MaxValue;

            if (t1 > 0f) t = t1;
            if (t2 > 0f && t2 < t) t = t2;

            if (t == float.MaxValue)
            {
                // Both solutions are in the past
                return null;
            }

            return t;
        }
    }
}