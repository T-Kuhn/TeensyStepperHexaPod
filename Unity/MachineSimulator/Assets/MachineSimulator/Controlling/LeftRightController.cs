using UnityEngine;

namespace MachineSimulator.Controlling
{
    public sealed class LeftRightController : MonoBehaviour
    {
        private const float Step = 0.005f;
        private float _accumulatedIncrease;

        public float AccumulatedIncrease => _accumulatedIncrease;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.D))
            {
                _accumulatedIncrease += Step;
                Debug.Log($"Left/Right increase: {_accumulatedIncrease}");
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                _accumulatedIncrease -= Step;
                Debug.Log($"Left/Right increase: {_accumulatedIncrease}");
            }
        }
    }
}