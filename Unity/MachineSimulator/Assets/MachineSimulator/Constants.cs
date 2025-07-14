using MachineSimulator.Machine;

namespace MachineSimulator
{
    public static class Constants
    {
        public const int BaudRate = 921600;

        public static readonly LLMachineState OriginMachineState = new LLMachineState();
        public static readonly LLMachineState OffsetFromTableState = new LLMachineState(-1.5f, -0.4f, -2.4f, 1.7f, -0.5f, 0.9f);
    }
}