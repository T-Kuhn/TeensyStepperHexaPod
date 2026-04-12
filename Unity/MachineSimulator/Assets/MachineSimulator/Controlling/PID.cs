using Unity.VisualScripting;
using UnityEngine;

namespace MachineSimulator.Controlling
{
    public class PID
    {
        private readonly float _kP;
        private readonly float _kI;
        private readonly float _kD;

        private float _lastPosInCm;

        // MEMO:
        //     - Front -> Z-
        //     - Back  -> Z+
        //     - Right -> X+
        //     - Left  -> X-
        // rotating around Z axis in minus direction will make ball go to the Right
        // rotatint around X axis in minus direction will make ball go to the Front
        public PID(float kP = 0.5f, float kI = 0f, float kD = 0.75f)
        {
            _kP = kP;
            _kI = kI;
            _kD = kD;
        }

        // input: Position as in "position along one of the axis"
        // output: correction tilt along that axis
        // target is 0 (center of paddle) for now
        public float Update(float position)
        {
            var posInCm = position * 100f;

            // Proportional term
            var p = _kP * posInCm;

            var v = posInCm - _lastPosInCm;
            // Derivative term
            var d = _kD * v;

            _lastPosInCm = posInCm;

            return Normalize(p + d);
        }

        // Max correction around axis will be +/- 2 degrees
        private float Normalize(float value)
        {
            return Mathf.Clamp(value, -2f, 2f);
        }
    }
}