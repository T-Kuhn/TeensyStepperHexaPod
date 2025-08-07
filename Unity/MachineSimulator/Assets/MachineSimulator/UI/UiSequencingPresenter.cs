using MachineSimulator.Machine;
using MachineSimulator.Sequencing;
using UniRx;
using UnityEngine;

namespace MachineSimulator.UI
{
    public sealed class UiSequencingPresenter : MonoBehaviour
    {
        [SerializeField] private UiView _view;
        [SerializeField] private SequenceCreator _sequenceCreator;
        [SerializeField] private MachineModel.MachineModel _machineModel;

        private void Awake()
        {
            _view.OnAddInstructionClicked.Subscribe(_ =>
            {
                var platePosition = _machineModel.HexaPlateTransform.position;
                var plateRotation = _machineModel.HexaPlateTransform.rotation;
                var moveTime = 1f;
                var hlMachineState = new HLMachineState(platePosition, plateRotation);
                var instruction = new HLInstruction(hlMachineState, moveTime);

                _sequenceCreator.Add(instruction);
            }).AddTo(this);

            _view.OnPlaybackClicked.Subscribe(_ => _sequenceCreator.StartPlayback()).AddTo(this);
            
            _view.OnPlaybackStringedClicked.Subscribe(_ => _sequenceCreator.StartStringedPlayback()).AddTo(this);

            _view.OnTeleportToOriginClicked.Subscribe(_ => _machineModel.HexaPlateMover.TeleportToDefaultHeight()).AddTo(this);
            
            _view.OnClearAllClicked.Subscribe(_ => _sequenceCreator.ClearAll()).AddTo(this);
        }
    }
}