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
        private CameraProperties _defaultCameraProperties;

        [SerializeField] private CameraProperties _cameraProperties;

        private void Awake()
        {
            _defaultCameraProperties = new CameraProperties()
            {
                Width = 640,
                Height = 480,
                Exposure = -7,
                Gain = 2,
                Saturation = 55,
                Contrast = 15,
                FPS = 120
            };
        }

        void Start()
        {
            _camera = getCamera();
            Debug.Log("Camera: " + _camera);

            var result = setCameraProperty(_camera, (int)vcp.CAP_PROP_FRAME_WIDTH, _defaultCameraProperties.Width);
            Debug.Log("Set camera width: " + result);
            result = setCameraProperty(_camera, (int)vcp.CAP_PROP_FRAME_HEIGHT, _defaultCameraProperties.Height);
            Debug.Log("Set camera height: " + result);
            result = setCameraProperty(_camera, (int)vcp.CAP_PROP_EXPOSURE, _defaultCameraProperties.Exposure);
            Debug.Log("Set camera exposure: " + result);
            result = setCameraProperty(_camera, (int)vcp.CAP_PROP_GAIN, _defaultCameraProperties.Gain);
            Debug.Log("Set camera gain: " + result);
            result = setCameraProperty(_camera, (int)vcp.CAP_PROP_SATURATION, _defaultCameraProperties.Saturation);
            Debug.Log("Set camera saturation: " + result);
            result = setCameraProperty(_camera, (int)vcp.CAP_PROP_CONTRAST, _defaultCameraProperties.Contrast);
            Debug.Log("Set camera contrast: " + result);
            result = setCameraProperty(_camera, (int)vcp.CAP_PROP_FPS, _defaultCameraProperties.FPS);
            Debug.Log("Set camera FPS: " + result);

            GetCameraProperties();

            Texture = new Texture2D((int)_defaultCameraProperties.Width, (int)_defaultCameraProperties.Height,
                TextureFormat.ARGB32, false);
            _pixels = Texture.GetPixels32();

            _pixelsHandle = GCHandle.Alloc(_pixels, GCHandleType.Pinned);
            _pixelsPtr = _pixelsHandle.AddrOfPinnedObject();
        }

        private double GetCameraProperty(vcp property)
        {
            return getCameraProperty(_camera, (int)property);
        }

        private void SetCameraProperty(vcp property, double value)
        {
            var result = setCameraProperty(_camera, (int)property, value);
            Debug.Log("Set camera property XX:" + property + ": " + result);
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

        private void Update()
        {
            Debug.Log("_camera: " + _camera);
            Debug.Log("_pixelsPtr: " + _pixelsPtr);
            Debug.Log("_defaultCameraProperties.Width: " + _defaultCameraProperties.Width);
            Debug.Log("_defaultCameraProperties.Height: " + _defaultCameraProperties.Height);
            
            var result = getCameraTexture(
                _camera,
                _pixelsPtr,
                (int)_defaultCameraProperties.Width,
                (int)_defaultCameraProperties.Height
            );
            Debug.Log("Get camera texture: " + result);

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