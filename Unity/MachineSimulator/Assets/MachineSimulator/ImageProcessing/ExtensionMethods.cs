using MachineSimulator;
using UnityEngine;

public static class ExtensionMethods
{
    private static byte[] _blackBGR = new byte[3];
    public static int GetBGRIndex(this Vector2Int position)
    {
        return (position.y * Constants.CameraResolutionWidth + position.x) * 3;
    }

    public static bool IsInBounds(this Vector2Int position)
    {
        return position.x < Constants.CameraResolutionWidth && position.x >= 0
                                                            && position.y < Constants.CameraResolutionHeight && position.y >= 0;
    }
}
