using System;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.MachineSimulator.ImageProcessing;
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

        [SerializeField] private CameraProperties _cameraProperties = new CameraProperties();

        public void Reset()
        {
            _cameraProperties = new CameraProperties();
        }

        public int ImageRetrievalTookTooLongCount { get; private set; }
        public long LastImageRetrievalTime { get; private set; }

        public bool CameraIsInitialized { get; private set; }

        private double GetCameraProperty(vcp property)
        {
            return getCameraProperty(_camera, (int)property);
        }

        private void SetCameraProperty(vcp property, double value)
        {
            var result = setCameraProperty(_camera, (int)property, value);
            if (result == 0)
            {
                Debug.LogWarning($"Failed to set camera property {property} to {value}");
            }
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
            SetCameraProperty(vcp.CAP_PROP_AUTO_EXPOSURE, 0.0);
            SetCameraProperty(vcp.CAP_PROP_EXPOSURE, _cameraProperties.Exposure);
            SetCameraProperty(vcp.CAP_PROP_GAIN, _cameraProperties.Gain);
            SetCameraProperty(vcp.CAP_PROP_CONTRAST, _cameraProperties.Contrast);
            SetCameraProperty(vcp.CAP_PROP_ISO_SPEED, _cameraProperties.ISO);
            SetCameraProperty(vcp.CAP_PROP_SATURATION, _cameraProperties.Saturation);
            SetCameraProperty(vcp.CAP_PROP_AUTO_WB, _cameraProperties.AutoWhiteBalance);
        }

        private void InitializeCamera()
        {
            if (_cameraProperties.Width == 0) _cameraProperties.Width = 1280;
            if (_cameraProperties.Height == 0) _cameraProperties.Height = 720;
            if (_cameraProperties.FPS == 0) _cameraProperties.FPS = 120;
            if (_cameraProperties.Exposure == 0) _cameraProperties.Exposure = -7;
            if (_cameraProperties.Gain == 0) _cameraProperties.Gain = 2;
            if (_cameraProperties.Saturation == 0) _cameraProperties.Saturation = 55;
            if (_cameraProperties.Contrast == 0) _cameraProperties.Contrast = 15;

            _camera = getCamera(_id);

            setCameraProperty(_camera, (int)vcp.CAP_PROP_FRAME_WIDTH, _cameraProperties.Width);
            setCameraProperty(_camera, (int)vcp.CAP_PROP_FRAME_HEIGHT, _cameraProperties.Height);
            setCameraProperty(_camera, (int)vcp.CAP_PROP_EXPOSURE, _cameraProperties.Exposure);
            setCameraProperty(_camera, (int)vcp.CAP_PROP_GAIN, _cameraProperties.Gain);
            setCameraProperty(_camera, (int)vcp.CAP_PROP_SATURATION, _cameraProperties.Saturation);
            setCameraProperty(_camera, (int)vcp.CAP_PROP_CONTRAST, _cameraProperties.Contrast);
            setCameraProperty(_camera, (int)vcp.CAP_PROP_FPS, _cameraProperties.FPS);
            setCameraProperty(_camera, (int)vcp.CAP_PROP_AUTO_WB, 0);

            Texture = new Texture2D((int)_cameraProperties.Width, (int)_cameraProperties.Height,
                TextureFormat.RGB24, false);

            // BGR
            var byteCount = (int)_cameraProperties.Width * (int)_cameraProperties.Height * 3;
            _pixelsFront = new byte[byteCount];
            _pixelsFrontHandle = GCHandle.Alloc(_pixelsFront, GCHandleType.Pinned);
            _pixelsFrontPtr = _pixelsFrontHandle.AddrOfPinnedObject();

            _pixelsBack = new byte[byteCount];
            _pixelsBackHandle = GCHandle.Alloc(_pixelsBack, GCHandleType.Pinned);
            _pixelsBackPtr = _pixelsBackHandle.AddrOfPinnedObject();

            GetCameraProperties();

            _isRunning = true;
            _cameraThread = new Thread(CameraLoop);
            _cameraThread.Priority = System.Threading.ThreadPriority.Highest;
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
                    (int)_cameraProperties.Width,
                    (int)_cameraProperties.Height
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
                    Debug.Log($"getCameraTexture returned error code: {result}");
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


        private BallDetection _ballDetection = new BallDetection();

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
                    _ballDetection.BallDataFromPixelBoarders(_pixelsFront, 500);

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
    public class CameraProperties
    {
        public double Width = 1280;
        public double Height = 720;
        public double FPS = 120;
        public double Exposure = -7;
        public double Gain = 2;
        public double Contrast = 15;
        public double ISO;
        public double Saturation = 55;
        public int AutoWhiteBalance = 0;
    }
}