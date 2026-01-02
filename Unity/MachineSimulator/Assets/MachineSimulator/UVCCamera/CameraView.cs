using UnityEngine;

namespace MachineSimulator.UVCCamera
{
    public class CameraView : MonoBehaviour
    {
        void Start()
        {
            var tex = GetComponentInParent<UVCCameraPlugin>().Texture;
            GetComponent<Renderer>().material.mainTexture = tex;
        }
    }
}