using UnityEngine;

namespace MachineSimulator.Application
{
    public sealed class ApplicationSettings : MonoBehaviour
    {
        private void Start()
        {
            QualitySettings.vSyncCount = 0;

            UnityEngine.Application.targetFrameRate = 500;
        }
    }
}