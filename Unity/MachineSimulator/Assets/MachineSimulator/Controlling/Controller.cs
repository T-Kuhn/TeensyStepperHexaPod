using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using MachineSimulator.ImageProcessing;
using MachineSimulator.Machine;
using MachineSimulator.Sequencing;
using UnityEngine;

namespace MachineSimulator.Controlling
{
    public sealed class Controller : MonoBehaviour
    {
        [SerializeField] private SequenceCreator _sequenceCreator;
        [SerializeField] private MachineModel.MachineModel _machineModel;
        [SerializeField] private RealMachine _realMachine;

        [SerializeField] private MonoBehaviour _cameOne;
        private IBallPositionProvider BallPositionProviderOne => _cameOne as IBallPositionProvider;

        [SerializeField] private MonoBehaviour _camTwo;
        private IBallPositionProvider BallPositionProviderTwo => _camTwo as IBallPositionProvider;

        private Transform _cameraOneTransform;
        private Transform _cameraTwoTransform;

        private Vector3 _camOneDetectedBallDir;
        private Vector3 _camTwoDetectedBallDir;

        [SerializeField] private Transform _planeOneOrigin;
        [SerializeField] private Transform _planeTwoOrigin;

        [SerializeField] private Transform _ballVisualization;

        private Vector3? _ballPosition;

        private void Start()
        {
            RunMachineLoopAsync().Forget();
        }

        private async UniTask RunMachineLoopAsync()
        {
            while (true)
            {
                if (_ballPosition.HasValue
                    && (BallPositionProviderOne is { IsBallDetected: true } || BallPositionProviderTwo is { IsBallDetected: true })
                    && _realMachine.IsReady
                    && _ballPosition.Value.y < 0.3f)
                {
                    // NOTE: defaultTime (3) / 4 = 0.75 (same as "Speed x4" setting)
                    var commandTime = 0.75f;
                    await SequenceFromCode.GoUpAndDownAsync(_machineModel, _sequenceCreator, commandTime, CancellationToken.None, true);
                }

                // NOTE: Needs to run after LateUpdate to ensure that we get newest ball position data.
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, CancellationToken.None);
            }
        }

        private void OnValidate()
        {
            if (_cameOne != null && !(_cameOne is IBallPositionProvider))
            {
                Debug.LogError($"{_cameOne.name} does not implement IBallPositionProvider!");
                _cameOne = null;
            }

            if (_camTwo != null && !(_camTwo is IBallPositionProvider))
            {
                Debug.LogError($"{_camTwo.name} does not implement IBallPositionProvider!");
                _camTwo = null;
            }
        }

        public void InjectRefs(Transform cameraOneTransform, Transform cameraTwoTransform)
        {
            _cameraOneTransform = cameraOneTransform;
            _cameraTwoTransform = cameraTwoTransform;
        }

        private bool _isLogging;
        private readonly List<string> _ballPositionLogs = new List<string>();

        // NOTE: LateUpdate because we get newest ball position in Update.
        //       Using LateUpdate to make sure we always get the newest position data.
        private void LateUpdate()
        {
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
                File.WriteAllLines($"ballpositionlogs.txt", _ballPositionLogs);
                _ballPositionLogs.Clear();
            }

            if (BallPositionProviderOne != null && _cameraOneTransform != null)
            {
                _camOneDetectedBallDir = CalculateDetectedBallDirection(_cameraOneTransform, BallPositionProviderOne.NewestBallPosition);
                AlignPlane(_planeOneOrigin, _cameraOneTransform, _camOneDetectedBallDir);
            }

            if (BallPositionProviderTwo != null && _cameraTwoTransform != null)
            {
                _camTwoDetectedBallDir = CalculateDetectedBallDirection(_cameraTwoTransform, BallPositionProviderTwo.NewestBallPosition);
                AlignPlane(_planeTwoOrigin, _cameraTwoTransform, _camTwoDetectedBallDir);
            }

            _ballPosition = CalculateIntersectionPoint();
            if (_ballPosition.HasValue && (BallPositionProviderOne is { IsBallDetected: true } || BallPositionProviderTwo is { IsBallDetected: true }))
            {
                _ballVisualization.position = _ballPosition.Value;

                if (_isLogging)
                {
                    var time = (long)(Time.realtimeSinceStartup * 1000);
                    _ballPositionLogs.Add($"{time};{_ballPosition.Value.x};{_ballPosition.Value.y};{_ballPosition.Value.z}");
                }
            }
        }

        private Vector3 CalculateDetectedBallDirection(Transform cameraTransform, Vector2 ballPosition)
        {
            var (horizontal, vertical) = Converter.ConvertToAngle(ballPosition);
            var rotation = cameraTransform.rotation * Quaternion.Euler(vertical, horizontal, 0f);
            return rotation * Vector3.forward;
        }

        private Vector3? CalculateIntersectionPoint()
        {
            if (BallPositionProviderOne == null || BallPositionProviderTwo == null ||
                _cameraOneTransform == null || _cameraTwoTransform == null ||
                _planeOneOrigin == null || _planeTwoOrigin == null)
            {
                return null;
            }

            // Step1: Figure out which ballposition is the oldest
            var camOneIsOldest = BallPositionProviderOne.TimeStamp <= BallPositionProviderTwo.TimeStamp;

            // Step2: Use AlignedPlane corresponding to oldest ballposition data as target plane
            // Also determine the correct layer to raycast against
            var targetLayerMask = camOneIsOldest
                ? LayerMask.GetMask("PlaneOne")
                : LayerMask.GetMask("PlaneTwo");

            // Step3: Shoot ray in direction of other ballposition data's detected ball direction (origin is corresponding _cameraTransform)
            var rayOrigin = camOneIsOldest ? _cameraTwoTransform : _cameraOneTransform;
            var rayDirection = camOneIsOldest ? _camTwoDetectedBallDir : _camOneDetectedBallDir;

            var ray = new Ray(rayOrigin.position, rayDirection);

            // Step4: Return the intersection point using Physics.Raycast with the correct layer
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity, targetLayerMask))
            {
                return hit.point;
            }

            return null;
        }

        private void AlignPlane(Transform planeOrigin, Transform cameraTransform, Vector3 detectedBallDir)
        {
            // Step1: Place plane origin at camera origin
            planeOrigin.position = cameraTransform.position;

            // Step2: Rotate plane so that plane.forward is aligned with detectedBallDir
            var rot = Quaternion.LookRotation(detectedBallDir);

            // NOTE: We only really care about the Y-axis rotation.
            var restrictedRot = Quaternion.Euler(0f, rot.eulerAngles.y, 0f);
            planeOrigin.rotation = restrictedRot;
        }

        private void OnDrawGizmos()
        {
            if (_cameraOneTransform != null)
            {
                DrawGizmoLineFor(_cameraOneTransform, Color.green, _cameraOneTransform.forward, 0.1f);
                DrawGizmoLineFor(_cameraOneTransform, Color.yellow, _camOneDetectedBallDir, 0.1f);
                DrawGizmoLineFor(_cameraOneTransform, Color.blue, _cameraOneTransform.up, 0.05f);
            }

            if (_cameraTwoTransform != null)
            {
                DrawGizmoLineFor(_cameraTwoTransform, Color.green, _cameraTwoTransform.forward, 0.1f);
                DrawGizmoLineFor(_cameraTwoTransform, Color.yellow, _camTwoDetectedBallDir, 0.1f);
                DrawGizmoLineFor(_cameraTwoTransform, Color.blue, _cameraTwoTransform.up, 0.05f);
            }
        }

        private void DrawGizmoLineFor(Transform camTransform, Color color, Vector3 direction, float length)
        {
            if (camTransform == null)
            {
                return;
            }

            Gizmos.color = color;
            var position = camTransform.position;
            Gizmos.DrawLine(position, position + direction * length);
        }
    }
}