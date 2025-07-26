using System.Collections.Generic;
using MachineSimulator.Machine;
using UnityEngine;

namespace MachineSimulator.Sequencing
{
    public sealed class SequenceCreator : MonoBehaviour
    {
        [SerializeField] private MachineModel.MachineModel _machineModel;
        
        [SerializeField] private readonly List<HLInstruction> _sequence = new List<HLInstruction>();
        
        public void Add(HLInstruction hlInstruction)
        {
            _sequence.Add(hlInstruction);
        }

        public void StartPlayback()
        {
            var hexaPlateMover = _machineModel.HexaPlateMover;
            
            hexaPlateMover.StartPlaybackMode(_sequence);
        }

        public void ClearAll()
        {
            _sequence.Clear();
        }
    }
}
