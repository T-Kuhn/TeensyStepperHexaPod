using System;
using System.Runtime.InteropServices;
using System.Threading;
using UniRx;
using vcp = MachineSimulator.UVCCamera.OpenCVConstants.VideoCaptureProperties;
using UnityEngine;

namespace MachineSimulator.UVCCamera
{
    public class UVCCameraPlugin : MonoBehaviour
    {
        [DllImport("UVCCameraPlugin")]
        private static extern IntPtr getCamera(int id);

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

        [SerializeField] private int _id;

        private IntPtr _camera;
        public Texture2D Texture;

        private byte[] _pixelsFront;
        private GCHandle _pixelsFrontHandle;
        private IntPtr _pixelsFrontPtr;

        private byte[] _pixelsBack;
        private GCHandle _pixelsBackHandle;
        private IntPtr _pixelsBackPtr;

        private Thread _cameraThread;
        private volatile bool _isRunning;
        private readonly object _lock = new object();
        private bool _hasNewFrame;

        private readonly CameraProperties _defaultCameraProperties = new CameraProperties()
        {
            // NOTE: Our camera's lowest supported resolution is 1280x720
            Width = 1280,
            Height = 720,
            Exposure = -7,
            Gain = 2,
            Saturation = 55,
            Contrast = 15,
            FPS = 120,
            AutoWhiteBalance = 0 // OFF
        };

        [SerializeField] private CameraProperties _cameraProperties;

        public int ImageRetrievalTookTooLongCount { get; private set; }
        public long LastImageRetrievalTime { get; private set; }

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
            _cameraProperties.AutoWhiteBalance = GetCameraProperty(vcp.CAP_PROP_AUTO_WB) == 1.0 ? 1 : 0;
        }

        public void SetCameraProperties()
        {
            SetCameraProperty(vcp.CAP_PROP_EXPOSURE, _cameraProperties.Exposure);
            SetCameraProperty(vcp.CAP_PROP_GAIN, _cameraProperties.Gain);
            SetCameraProperty(vcp.CAP_PROP_CONTRAST, _cameraProperties.Contrast);
            SetCameraProperty(vcp.CAP_PROP_ISO_SPEED, _cameraProperties.ISO);
            SetCameraProperty(vcp.CAP_PROP_SATURATION, _cameraProperties.Saturation);
            SetCameraProperty(vcp.CAP_PROP_AUTO_WB, _cameraProperties.AutoWhiteBalance);
        }

        private void InitializeCamera()
        {
            _camera = getCamera(_id);

            setCameraProperty(_camera, (int)vcp.CAP_PROP_FRAME_WIDTH, _defaultCameraProperties.Width);
            setCameraProperty(_camera, (int)vcp.CAP_PROP_FRAME_HEIGHT, _defaultCameraProperties.Height);
            setCameraProperty(_camera, (int)vcp.CAP_PROP_EXPOSURE, _defaultCameraProperties.Exposure);
            setCameraProperty(_camera, (int)vcp.CAP_PROP_GAIN, _defaultCameraProperties.Gain);
            setCameraProperty(_camera, (int)vcp.CAP_PROP_SATURATION, _defaultCameraProperties.Saturation);
            setCameraProperty(_camera, (int)vcp.CAP_PROP_CONTRAST, _defaultCameraProperties.Contrast);
            setCameraProperty(_camera, (int)vcp.CAP_PROP_FPS, _defaultCameraProperties.FPS);
            setCameraProperty(_camera, (int)vcp.CAP_PROP_AUTO_WB, 0);

            Texture = new Texture2D((int)_defaultCameraProperties.Width, (int)_defaultCameraProperties.Height,
                TextureFormat.RGB24, false);

            // BGR
            var byteCount = (int)_defaultCameraProperties.Width * (int)_defaultCameraProperties.Height * 3;
            _pixelsFront = new byte[byteCount];
            _pixelsFrontHandle = GCHandle.Alloc(_pixelsFront, GCHandleType.Pinned);
            _pixelsFrontPtr = _pixelsFrontHandle.AddrOfPinnedObject();

            _pixelsBack = new byte[byteCount];
            _pixelsBackHandle = GCHandle.Alloc(_pixelsBack, GCHandleType.Pinned);
            _pixelsBackPtr = _pixelsBackHandle.AddrOfPinnedObject();

            GetCameraProperties();

            _isRunning = true;
            _cameraThread = new Thread(CameraLoop);
            _cameraThread.IsBackground = true;
            _cameraThread.Start();

            CameraIsInitialized = true;
        }

        private void CameraLoop()
        {
            var sw = new System.Diagnostics.Stopwatch();
            while (_isRunning)
            {
                sw.Restart();
                var result = getCameraTexture(
                    _camera,
                    _pixelsBackPtr,
                    (int)_defaultCameraProperties.Width,
                    (int)_defaultCameraProperties.Height
                );
                sw.Stop();

                if (sw.ElapsedMilliseconds > 20)
                {
                    Debug.Log($"Image retrieval took {sw.ElapsedMilliseconds}ms on camera with ID: " + _id);
                    ImageRetrievalTookTooLongCount++;
                    LastImageRetrievalTime = sw.ElapsedMilliseconds;
                }

                if (result < 0)
                {
                    Debug.LogError($"getCameraTexture returned error code: {result}");
                    Thread.Sleep(10); // Sleep a bit on error
                    continue;
                }

                lock (_lock)
                {
                    // Swap pointers/buffers
                    (_pixelsBackPtr, _pixelsFrontPtr) = (_pixelsFrontPtr, _pixelsBackPtr);
                    (_pixelsBack, _pixelsFront) = (_pixelsFront, _pixelsBack);

                    _hasNewFrame = true;
                }
            }
        }

        private void Update()
        {
            if (!CameraIsInitialized)
            {
                InitializeCamera();
            }

            if (_hasNewFrame)
            {
                lock (_lock)
                {
                    Texture.SetPixelData(_pixelsFront, 0);
                    _hasNewFrame = false;
                }

                Texture.Apply();
            }
        }

        private void OnDestroy()
        {
            _isRunning = false;

            if (_cameraThread != null && _cameraThread.IsAlive)
            {
                // Wait for the thread to finish. 
                // We give it some time, but not forever to avoid freezing Unity.
                if (!_cameraThread.Join(1000))
                {
                    Debug.LogWarning("Camera thread did not terminate in time. Memory might leak or crash on exit.");
                }
            }

            if (_pixelsFrontHandle.IsAllocated) _pixelsFrontHandle.Free();
            if (_pixelsBackHandle.IsAllocated) _pixelsBackHandle.Free();

            if (_camera != IntPtr.Zero)
            {
                releaseCamera(_camera);
                _camera = IntPtr.Zero;
            }
        }

        private void OnApplicationQuit()
        {
            OnDestroy();
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
        public int AutoWhiteBalance;
    }
}