using MachineSimulator;
using UnityEngine;

public static class ExtensionMethods
{
    private static Color32 _black = new Color32(0,0,0,1);
    public static ref Color32 AtPosition(this Color32[] pixels, Vector2Int position)
    {
        if (position.x < Constants.CameraResolutionWidth && position.x >= 0
                                                         && position.y < Constants.CameraResolutionHeight && position.y >=0)
        {
            return ref pixels[position.y * Constants.CameraResolutionWidth + position.x];
        }
        else
        {
            return ref _black;
        }
    }
}
