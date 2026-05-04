using UnityEngine;

namespace MachineSimulator.Controlling
{
    public sealed class RestHeightController : MonoBehaviour
    {
        private const float Step = 0.01f;
        private float _accumulatedIncrease;

        public float AccumulatedIncrease => _accumulatedIncrease;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                _accumulatedIncrease += Step;
                Debug.Log($"RestHeight increase: {_accumulatedIncrease}");
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                _accumulatedIncrease = Mathf.Max(0f, _accumulatedIncrease - Step);
                Debug.Log($"RestHeight increase: {_accumulatedIncrease}");
            }
        }
    }
}
