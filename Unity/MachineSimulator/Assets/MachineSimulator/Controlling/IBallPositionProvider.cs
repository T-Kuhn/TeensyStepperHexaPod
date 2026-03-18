using UnityEngine;

namespace MachineSimulator.Controlling
{
    public interface IBallPositionProvider
    {
        Vector2 NewestBallPosition { get; }
        float TimeStamp { get; }
        bool IsBallDetected { get; }
    }
}