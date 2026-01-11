using System;
using MachineSimulator.Machine;

namespace MachineSimulator
{
    public static class Constants
    {
        public const int BaudRate = 921600;

        public static readonly LLMachineState OriginMachineState = new LLMachineState();
        public static readonly LLMachineState OffsetFromTableState = new LLMachineState(-0.05f, 0.05f, -0.05f, 0.05f, -0.05f, 0.05f);

        public static readonly int CameraResolutionWidth = 1280;
        public static readonly int CameraResolutionHeight = 720;
        
        public static Byte Threshold = 70;
        public static int PixelSpacing = 30;
    }
}