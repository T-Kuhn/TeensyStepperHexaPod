using UnityEngine;

namespace MachineSimulator.Controlling
{
    public sealed class RestHeightController : MonoBehaviour
    {
        [SerializeField] private float _step = 0.005f;
        private float _accumulatedIncrease;

        public float AccumulatedIncrease => _accumulatedIncrease;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                _accumulatedIncrease += _step;
                Debug.Log($"RestHeight increase: {_accumulatedIncrease}");
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                _accumulatedIncrease = Mathf.Max(0f, _accumulatedIncrease - _step);
                Debug.Log($"RestHeight increase: {_accumulatedIncrease}");
            }
        }
    }
}
