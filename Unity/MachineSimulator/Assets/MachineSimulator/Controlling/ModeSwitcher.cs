using UnityEngine;

namespace MachineSimulator.Controlling
{
    public sealed class ModeSwitcher : MonoBehaviour
    {
        private BallHandlingMode _currentMode;

        public BallHandlingMode CurrentMode => _currentMode;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                SwitchMode(1);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                SwitchMode(-1);
            }
        }

        private void SwitchMode(int direction)
        {
            var modes = (BallHandlingMode[])System.Enum.GetValues(typeof(BallHandlingMode));
            var currentIndex = System.Array.IndexOf(modes, _currentMode);
            var nextIndex = (currentIndex + direction + modes.Length) % modes.Length;

            _currentMode = modes[nextIndex];
            Debug.Log($"Switched to {_currentMode}");
        }
    }

    public enum BallHandlingMode
    {
        None,
        SlowBouncing,
        FastBouncing,
        Alternating,
    }
}