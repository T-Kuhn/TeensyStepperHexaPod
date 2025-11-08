using UnityEngine;

namespace MachineSimulator.UI
{
    public sealed class FpsCounter : MonoBehaviour
    {
        [SerializeField] private UiView _view;
        private readonly float _updateInterval = 0.1f;

        private float _accumulatedTime;
        private int _frameCount;
        private float _timeLeft;

        private void Start()
        {
            _timeLeft = _updateInterval;
        }

        private void Update()
        {
            _timeLeft -= Time.deltaTime;
            _accumulatedTime += Time.timeScale / Time.deltaTime;
            _frameCount++;

            if (_timeLeft <= 0f)
            {
                var fps = _accumulatedTime / _frameCount;
                _view.SetTextOnFpsCounterLabelTo($"FPS: {fps:F1}");

                _timeLeft = _updateInterval;
                _accumulatedTime = 0f;
                _frameCount = 0;
            }
        }
    }
}