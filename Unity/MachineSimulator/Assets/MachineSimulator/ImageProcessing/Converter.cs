using UnityEngine;
using c = MachineSimulator.Constants;

namespace MachineSimulator.ImageProcessing
{
    public static class Converter
    {
        public static (float HorizontalAngle, float verticalAngle) ConvertToAngle(Vector2 positionInImage)
        {
            var halfWidth = c.CameraResolutionWidth / 2f;
            var halfHeight = c.CameraResolutionHeight / 2f;

            // NOTE: Camera is mounted 90deg offset, so the vertical axis is the wide one (that's why we use width)
            var horizontalAngle = (positionInImage.y / halfHeight) * (c.CameraHorizontalFov / 2f);
            var verticalAngle = (positionInImage.x / halfWidth) * (c.CameraVerticalFov / 2f);

            return (horizontalAngle, verticalAngle);
        }
    }
}