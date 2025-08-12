using MachineSimulator.Machine;
using UnityEngine;

namespace MachineSimulator.MachineModel
{
    public sealed class LLMachineStateProvider : MonoBehaviour
    {
        public LLMachineState CurrentLowLevelMachineState => new LLMachineState(_m1Rot, _m2Rot, _m3Rot, _m4Rot, _m5Rot, _m6Rot);

        private float _m1Rot;
        private float _m2Rot;
        private float _m3Rot;
        private float _m4Rot;
        private float _m5Rot;
        private float _m6Rot;

        public void SetRotationStateForArmWithIndex(int index, float rot)
        {
            switch (index)
            {
                case 1:
                    SetM1State(rot);
                    break;
                case 2:
                    SetM2State(rot);
                    break;
                case 3:
                    SetM3State(rot);
                    break;
                case 4:
                    SetM4State(rot);
                    break;
                case 5:
                    SetM5State(rot);
                    break;
                case 6:
                    SetM6State(rot);
                    break;
            }
        }
        
        public void SetM1State(float rot) => _m1Rot = rot;
        public void SetM2State(float rot) => _m2Rot = rot;
        public void SetM3State(float rot) => _m3Rot = rot;
        public void SetM4State(float rot) => _m4Rot = rot;
        public void SetM5State(float rot) => _m5Rot = rot;
        public void SetM6State(float rot) => _m6Rot = rot;
    }
}