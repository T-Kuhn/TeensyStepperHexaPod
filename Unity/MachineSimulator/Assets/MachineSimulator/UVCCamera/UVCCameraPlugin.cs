using System;
using System.Runtime.InteropServices;
using vcp = MachineSimulator.UVCCamera.OpenCVConstants.VideoCaptureProperties;
using UnityEngine;

namespace MachineSimulator.UVCCamera
{
    public class UVCCameraPlugin : MonoBehaviour
    {
        [DllImport("UVCCameraPlugin")]
        private static extern IntPtr getCamera();

        [DllImport("UVCCameraPlugin")]
        private static extern double getCameraProperty(IntPtr camera, int propertyId);

        [DllImport("UVCCameraPlugin")]
        private static extern int setCameraProperty(IntPtr camera, int propertyId, double value);

        [DllImport("UVCCameraPlugin")]
        private static extern void releaseCamera(IntPtr camera);

        [DllImport("UVCCameraPlugin")]
        private static extern int getCameraTexture(IntPtr camera, IntPtr data, int width, int height);

        [DllImport("UVCCameraPlugin")]
        private static extern int getCameraDimensions(IntPtr camera, out int width, out int height);

        private IntPtr _camera;
        public Texture2D Texture;
        private Color32[] _pixels;
        private GCHandle _pixelsHandle;
        private IntPtr _pixelsPtr;

        private readonly CameraProperties _defaultCameraProperties = new CameraProperties()
        {
            // NOTE: Our camera's lowest supported resolution is 1280x720
            Width = 1280,
            Height = 720,
            Exposure = -7,
            Gain = 2,
            Saturation = 55,
            Contrast = 15,
            FPS = 120
        };

        [SerializeField] private CameraProperties _cameraProperties;

        public bool CameraIsInitialized { get; private set; }

        private double GetCameraProperty(vcp property)
        {
            return getCameraProperty(_camera, (int)property);
        }

        private void SetCameraProperty(vcp property, double value)
        {
            setCameraProperty(_camera, (int)property, value);
        }

        public void GetCameraProperties()
        {
            _cameraProperties.Width = GetCameraProperty(vcp.CAP_PROP_FRAME_WIDTH);
            _cameraProperties.Height = GetCameraProperty(vcp.CAP_PROP_FRAME_HEIGHT);
            _cameraProperties.FPS = GetCameraProperty(vcp.CAP_PROP_FPS);
            _cameraProperties.Exposure = GetCameraProperty(vcp.CAP_PROP_EXPOSURE);
            _cameraProperties.Gain = GetCameraProperty(vcp.CAP_PROP_GAIN);
            _cameraProperties.Contrast = GetCameraProperty(vcp.CAP_PROP_CONTRAST);
            _cameraProperties.ISO = GetCameraProperty(vcp.CAP_PROP_ISO_SPEED);
            _cameraProperties.Saturation = GetCameraProperty(vcp.CAP_PROP_SATURATION);
        }

        public void SetCameraProperties()
        {
            SetCameraProperty(vcp.CAP_PROP_EXPOSURE, _cameraProperties.Exposure);
            SetCameraProperty(vcp.CAP_PROP_GAIN, _cameraProperties.Gain);
            SetCameraProperty(vcp.CAP_PROP_CONTRAST, _cameraProperties.Contrast);
            SetCameraProperty(vcp.CAP_PROP_ISO_SPEED, _cameraProperties.ISO);
            SetCameraProperty(vcp.CAP_PROP_SATURATION, _cameraProperties.Saturation);
        }

        private void InitializeCamera()
        {
            _camera = getCamera();

            setCameraProperty(_camera, (int)vcp.CAP_PROP_FRAME_WIDTH, _defaultCameraProperties.Width);
            setCameraProperty(_camera, (int)vcp.CAP_PROP_FRAME_HEIGHT, _defaultCameraProperties.Height);
            setCameraProperty(_camera, (int)vcp.CAP_PROP_EXPOSURE, _defaultCameraProperties.Exposure);
            setCameraProperty(_camera, (int)vcp.CAP_PROP_GAIN, _defaultCameraProperties.Gain);
            setCameraProperty(_camera, (int)vcp.CAP_PROP_SATURATION, _defaultCameraProperties.Saturation);
            setCameraProperty(_camera, (int)vcp.CAP_PROP_CONTRAST, _defaultCameraProperties.Contrast);
            setCameraProperty(_camera, (int)vcp.CAP_PROP_FPS, _defaultCameraProperties.FPS);

            Texture = new Texture2D((int)_defaultCameraProperties.Width, (int)_defaultCameraProperties.Height,
                TextureFormat.ARGB32, false);
            _pixels = Texture.GetPixels32();

            _pixelsHandle = GCHandle.Alloc(_pixels, GCHandleType.Pinned);
            _pixelsPtr = _pixelsHandle.AddrOfPinnedObject();

            GetCameraProperties();

            CameraIsInitialized = true;
        }

        private void Update()
        {
            if (!CameraIsInitialized)
            {
                InitializeCamera();
            }

            getCameraTexture(
                _camera,
                _pixelsPtr,
                (int)_defaultCameraProperties.Width,
                (int)_defaultCameraProperties.Height
            );

            Texture.SetPixels32(_pixels);
            Texture.Apply();
        }

        private void OnApplicationQuit()
        {
            _pixelsHandle.Free();
            releaseCamera(_camera);
        }
    }

    [Serializable]
    struct CameraProperties
    {
        public double Width;
        public double Height;
        public double FPS;
        public double Exposure;
        public double Gain;
        public double Contrast;
        public double ISO;
        public double Saturation;
    }
}