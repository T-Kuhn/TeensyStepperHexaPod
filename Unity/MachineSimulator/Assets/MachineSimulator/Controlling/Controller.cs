using MachineSimulator.ImageProcessing;
using UnityEngine;

namespace MachineSimulator.Controlling
{
    public sealed class Controller : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _cameOne;
        private IBallPositionProvider BallPositionProviderOne => _cameOne as IBallPositionProvider;

        [SerializeField] private MonoBehaviour _camTwo;

        private Transform _cameraOneTransform;
        private Transform _cameraTwoTransform;

        private Vector3 _camOneDetectedBallDir;
        private Vector3 _camTwoDetectedBallDir;

        private IBallPositionProvider BallPositionProviderTwo => _camTwo as IBallPositionProvider;

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

        // NOTE: LateUpdate because we get newest ball position in Update.
        //       Using LateUpdate to make sure we always get the newest position data.
        private void LateUpdate()
        {
            if (BallPositionProviderOne != null && _cameraOneTransform != null)
            {
                var (horizontal, vertical) = Converter.ConvertToAngle(BallPositionProviderOne.NewestBallPosition);
                var rotation = Quaternion.Euler(vertical, horizontal, 0f);
                _camOneDetectedBallDir = rotation * _cameraOneTransform.forward;
            }

            if (BallPositionProviderTwo != null && _cameraTwoTransform != null)
            {
                var (horizontal, vertical) = Converter.ConvertToAngle(BallPositionProviderTwo.NewestBallPosition);
                var rotation = Quaternion.Euler(vertical, horizontal, 0f);
                _camTwoDetectedBallDir = rotation * _cameraTwoTransform.forward;
            }
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