using UnityEngine;

namespace MachineSimulator.Utils
{
    public class HeightFromGroundMeasure : MonoBehaviour
    {
        [SerializeField] private float _groundHeight = 0f;

        public float CurrentHeightAboveGround;

        private void OnDrawGizmosSelected()
        {
            // This method also runs in editor mode when the object is selected
            UpdateHeightMeasurement();

            // Draw a white small cube gizmo
            Gizmos.color = Color.white;
            Gizmos.DrawCube(transform.position, Vector3.one * 0.001f);
        }

        private void UpdateHeightMeasurement()
        {
            // Get the world Y position of this transform
            var worldHeight = transform.position.y;

            // Calculate height above ground
            CurrentHeightAboveGround = worldHeight - _groundHeight;
        }
    }
}