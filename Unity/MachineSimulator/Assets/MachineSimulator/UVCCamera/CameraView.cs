using UnityEngine;

namespace MachineSimulator.UVCCamera
{
    public class CameraView : MonoBehaviour
    {
        private UVCCameraPlugin _uvcPlugin;
        private Renderer _renderer;

        void Start()
        {
            _uvcPlugin = GetComponentInParent<UVCCameraPlugin>();
            _renderer = GetComponent<Renderer>();
        }

        private void Update()
        {
            if (!_uvcPlugin.CameraIsInitialized) return;

            var tex = _uvcPlugin.Texture;
            _renderer.material.mainTexture = tex;
        }
    }
}