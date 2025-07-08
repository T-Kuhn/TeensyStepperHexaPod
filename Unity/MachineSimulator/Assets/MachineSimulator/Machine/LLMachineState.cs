namespace MachineSimulator.Machine
{
    // Low Level Machine State
    public readonly struct LLMachineState
    {
        public float Motor1Rotation { get; }
        public float Motor2Rotation { get; }
        public float Motor3Rotation { get; }
        public float Motor4Rotation { get; }
        public float Motor5Rotation { get; }
        public float Motor6Rotation { get; }

        public LLMachineState(float m1Rot, float m2Rot, float m3Rot, float m4Rot,
            float m5Rot, float m6Rot)
        {
            Motor1Rotation = m1Rot;
            Motor2Rotation = m2Rot;
            Motor3Rotation = m3Rot;
            Motor4Rotation = m4Rot;
            Motor5Rotation = m5Rot;
            Motor6Rotation = m6Rot;
        }

        public static LLMachineState operator +(LLMachineState a, LLMachineState b)
        {
            return new LLMachineState(
                a.Motor1Rotation + b.Motor1Rotation,
                a.Motor2Rotation + b.Motor2Rotation,
                a.Motor3Rotation + b.Motor3Rotation,
                a.Motor4Rotation + b.Motor4Rotation,
                a.Motor5Rotation + b.Motor5Rotation,
                a.Motor6Rotation + b.Motor6Rotation);
        }

        public static LLMachineState operator -(LLMachineState a, LLMachineState b)
        {
            return new LLMachineState(
                a.Motor1Rotation - b.Motor1Rotation,
                a.Motor2Rotation - b.Motor2Rotation,
                a.Motor3Rotation - b.Motor3Rotation,
                a.Motor4Rotation - b.Motor4Rotation,
                a.Motor5Rotation - b.Motor5Rotation,
                a.Motor6Rotation - b.Motor6Rotation);
        }
    }
}