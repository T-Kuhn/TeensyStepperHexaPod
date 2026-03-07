using UnityEngine;

namespace MachineSimulator.Controlling
{
    public sealed class Controller : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _cameOne;
        private IBallPositionProvider BallPositionProviderOne => _cameOne as IBallPositionProvider;
        
        [SerializeField] private MonoBehaviour _camTwo;
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

        // NOTE: LateUpdate because we get newest ball position in Update.
        //       Using LateUpdate to make sure we always get the newest position data.
        void LateUpdate()
        {
            Debug.Log("BallPosition of one: " + BallPositionProviderOne.NewestBallPosition);
        }
    }
}