using System.Text;
using UnityEngine;

namespace MachineSimulator.Logging
{
    public sealed class Logger : MonoBehaviour
    {
        private float _elapsedLogTime = 0f;
        private readonly StringBuilder _logBuilder = new StringBuilder();
        private bool _isLogging;

        public void StartLogging()
        {
            _logBuilder.Clear();
            _elapsedLogTime = 0f;
            _isLogging = true;
        }

        public void StopLogging()
        {
            _isLogging = false;
            Debug.Log(_logBuilder.ToString());
        }

        // NOTE: We are assuming that the logger gets a new value every frame;
        //       that's why it is okay to manage elapsed time here.
        public void UpdateLogging(float value)
        {
            if (!_isLogging) return;

            _elapsedLogTime += Time.deltaTime;

            _logBuilder.AppendLine(_elapsedLogTime.ToString("0.0000") + ", " + value.ToString("0.0000"));
        }
    }
}