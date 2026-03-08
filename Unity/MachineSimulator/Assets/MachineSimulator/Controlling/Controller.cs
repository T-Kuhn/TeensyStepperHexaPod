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
            Debug.Log("BallPosition of one: " + BallPositionProviderOne.NewestBallPosition);
        }

        private void OnDrawGizmos()
        {
            DrawGizmoLineFor(_cameraOneTransform);
            DrawGizmoLineFor(_cameraTwoTransform);
        }

        private void DrawGizmoLineFor(Transform camTransform)
        {
            if (camTransform == null)
            {
                return;
            }

            Gizmos.color = Color.green;
            var transform1 = camTransform.transform;
            var position = transform1.position;
            Gizmos.DrawLine(position, position + transform1.forward * 0.1f);
        }
    }
}