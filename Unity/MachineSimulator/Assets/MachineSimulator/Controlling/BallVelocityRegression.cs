using System.Collections.Generic;
using UnityEngine;

namespace MachineSimulator.Controlling
{
    public sealed class BallVelocityRegression
    {
        private readonly int _sampleCount;
        private readonly Queue<(float time, Vector3 position)> _samples = new Queue<(float, Vector3)>();

        public BallVelocityRegression(int sampleCount = 10)
        {
            _sampleCount = sampleCount;
        }

        public void AddSample(float time, Vector3 position)
        {
            _samples.Enqueue((time, position));
            if (_samples.Count > _sampleCount)
            {
                _samples.Dequeue();
            }
        }

        public Vector3 CalculateVelocity()
        {
            if (_samples.Count < 2)
            {
                return Vector3.zero;
            }

            // Simple linear regression for each axis (x, y, z)
            // v = (n*sum(t*p) - sum(t)*sum(p)) / (n*sum(t^2) - (sum(t))^2)

            var n = _samples.Count;
            var firstTime = _samples.Peek().time;
            var sumT = 0f;
            var sumT2 = 0f;
            var sumP = Vector3.zero;
            var sumTP = Vector3.zero;

            foreach (var sample in _samples)
            {
                var t = sample.time - firstTime;
                var p = sample.position;

                sumT += t;
                sumT2 += t * t;
                sumP += p;
                sumTP += t * p;
            }

            var denominator = n * sumT2 - sumT * sumT;
            if (Mathf.Abs(denominator) < 1e-6f)
            {
                return Vector3.zero;
            }

            var velocity = (n * sumTP - sumT * sumP) / denominator;
            return velocity;
        }
    }
}
