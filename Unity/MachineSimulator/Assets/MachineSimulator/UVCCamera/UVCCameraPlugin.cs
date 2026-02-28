using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.MachineSimulator.ImageProcessing;
using UnityEngine;

namespace MachineSimulator.UVCCamera
{
    public class UVCCameraPlugin : MonoBehaviour
    {
        // Open camera by device index, configure resolution/FPS, and disable all
        // automatic camera controls internally. Returns a handle or IntPtr.Zero on failure.
        [DllImport("UVCCameraPlugin")]
        private static extern IntPtr openCamera(int deviceIndex, int width, int height, int fps);

        [DllImport("UVCCameraPlugin")]
        private static extern void releaseCamera(IntPtr camera);

        [DllImport("UVCCameraPlugin")]
        private static extern int getCameraTexture(IntPtr camera, IntPtr data, int width, int height);

        [DllImport("UVCCameraPlugin")]
        private static extern int getCameraDimensions(IntPtr camera, out int width, out int height);

        [DllImport("UVCCameraPlugin")]
        private static extern int setCameraExposure(IntPtr camera, int value);

        [DllImport("UVCCameraPlugin")]
        private static extern int setCameraGain(IntPtr camera, int value);

        [DllImport("UVCCameraPlugin")]
        private static extern int setCameraContrast(IntPtr camera, int value);

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

        public void ResetCameraProperties()
        {
            _cameraProperties = new CameraProperties();
        }

        public int ImageRetrievalTookTooLongCount { get; private set; }
        public long LastImageRetrievalTime { get; private set; }

        public bool CameraIsInitialized { get; private set; }

        // Apply exposure, gain, and contrast to the running camera.
        public void SetCameraProperties()
        {
            if (_camera == IntPtr.Zero) return;

            int result;
            result = setCameraExposure(_camera, (int)_cameraProperties.Exposure);
            if (result == 0) Debug.LogWarning($"Failed to set camera exposure to {_cameraProperties.Exposure}");

            result = setCameraGain(_camera, (int)_cameraProperties.Gain);
            if (result == 0) Debug.LogWarning($"Failed to set camera gain to {_cameraProperties.Gain}");

            result = setCameraContrast(_camera, (int)_cameraProperties.Contrast);
            if (result == 0) Debug.LogWarning($"Failed to set camera contrast to {_cameraProperties.Contrast}");
        }

        private void InitializeCamera()
        {
            ResetCameraProperties();

            // Open camera and configure resolution/FPS in one call.
            // All auto-controls are disabled internally by the plugin.
            _camera = openCamera(
                _id,
                (int)_cameraProperties.Width,
                (int)_cameraProperties.Height,
                (int)_cameraProperties.FPS
            );

            if (_camera == IntPtr.Zero)
            {
                Debug.LogError($"Failed to open camera with device index {_id}");
                return;
            }

            // Apply manual exposure / gain / contrast.
            SetCameraProperties();

            // Read back actual negotiated dimensions (may differ from requested).
            int actualWidth  = (int)_cameraProperties.Width;
            int actualHeight = (int)_cameraProperties.Height;
            getCameraDimensions(_camera, out actualWidth, out actualHeight);

            Texture = new Texture2D(actualWidth, actualHeight, TextureFormat.RGB24, false);

            // BGR -- 3 bytes per pixel
            var byteCount = actualWidth * actualHeight * 3;
            _pixelsFront = new byte[byteCount];
            _pixelsFrontHandle = GCHandle.Alloc(_pixelsFront, GCHandleType.Pinned);
            _pixelsFrontPtr = _pixelsFrontHandle.AddrOfPinnedObject();

            _pixelsBack = new byte[byteCount];
            _pixelsBackHandle = GCHandle.Alloc(_pixelsBack, GCHandleType.Pinned);
            _pixelsBackPtr = _pixelsBackHandle.AddrOfPinnedObject();

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

                if (sw.ElapsedMilliseconds > 50)
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

        private readonly BallDetection _ballDetection = new BallDetection();

        private bool _isLogging;
        private readonly List<string> _ballPositionLogs = new List<string>();

        private void Update()
        {
            if (!CameraIsInitialized)
            {
                InitializeCamera();
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                Debug.Log("Start");
                _isLogging = true;
                _ballPositionLogs.Clear();
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("End");
                _isLogging = false;
                File.WriteAllLines("ballpositionlogs.txt", _ballPositionLogs);
                _ballPositionLogs.Clear();
            }

            if (_hasNewFrame)
            {
                lock (_lock)
                {
                    var res = _ballDetection.BallDataFromPixelBoarders(_pixelsFront, 500);

                    if (_isLogging && res.Count > 0)
                    {
                        var time = (long)(Time.realtimeSinceStartup * 1000);
                        var ball = res[0];
                        _ballPositionLogs.Add($"{time};{ball.PositionX};{ball.PositionY}");
                    }

                    Texture.SetPixelData(_pixelsFront, 0);
                    Texture.Apply();

                    _hasNewFrame = false;
                }
            }
        }

        private void OnDestroy()
        {
            _isRunning = false;

            if (_cameraThread != null && _cameraThread.IsAlive)
            {
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
        public double Width = Constants.CameraResolutionWidth;
        public double Height = Constants.CameraResolutionHeight;
        public double FPS = 120;
        public double Exposure = -7;
        public double Gain = 2;
        public double Contrast = 15;
    }
}
