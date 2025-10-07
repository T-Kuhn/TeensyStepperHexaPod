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

        private float _currentCommandTime = 3f;

        // NOTE: New motors can handle 0.35f,
        //       but I don't think we should go faster than that. (Observed skipping with 0.175 with no load attached)
        private readonly float _defaultCommandTime = 3f;

        private void Awake()
        {
            _view.OnLoadSequenceFromCodeClicked.Subscribe(_ =>
                {
                    _sequenceCreator.ClearAll();
                    _machineModel.HexaPlateMover.TeleportToDefaultHeight();

                    // FirstStep
                    var rotation = Quaternion.identity;
                    var position = Vector3.up * 0.3f;
                    _machineModel.HexaPlateMover.UpdatePositionAndRotationTo(position, rotation);
                    _sequenceCreator.Add(HLInstructionFromCurrentMachineState(_defaultCommandTime));

                    // LastStep
                    _machineModel.HexaPlateMover.TeleportToDefaultHeight();
                    _sequenceCreator.Add(HLInstructionFromCurrentMachineState(_defaultCommandTime));
                }
            ).AddTo(this);

            _view.OnAddInstructionClicked.Subscribe(_ => { _sequenceCreator.Add(HLInstructionFromCurrentMachineState(_defaultCommandTime)); }).AddTo(this);

            _view.OnSeqDoubleSpeedClicked
                .Subscribe(_ =>
                {
                    _currentCommandTime = _defaultCommandTime / 2f;
                    _sequenceCreator.UpdateAllMoveTimesInSequenceTo(_currentCommandTime);
                })
                .AddTo(this);

            _view.OnSeqQuatrupleSpeedClicked
                .Subscribe(_ =>
                {
                    _currentCommandTime = _defaultCommandTime / 4f;
                    _sequenceCreator.UpdateAllMoveTimesInSequenceTo(_currentCommandTime);
                })
                .AddTo(this);

            _view.OnSeqNormalSpeedClicked
                .Subscribe(_ =>
                {
                    _currentCommandTime = _defaultCommandTime;
                    _sequenceCreator.UpdateAllMoveTimesInSequenceTo(_currentCommandTime);
                })
                .AddTo(this);

            _view.OnPlaybackClicked.Subscribe(_ => _sequenceCreator.StartPlayback()).AddTo(this);

            _view.OnPlaybackStringedClicked.Subscribe(_ => _sequenceCreator.StartStringedPlayback()).AddTo(this);

            _view.OnSendStringedToMachineClicked.Subscribe(_ => _sequenceCreator.StartStringedPlayback(true)).AddTo(this);

            _view.OnTeleportToOriginClicked.Subscribe(_ => _machineModel.HexaPlateMover.TeleportToDefaultHeight()).AddTo(this);

            _view.OnClearAllClicked.Subscribe(_ => _sequenceCreator.ClearAll()).AddTo(this);
        }

        private HLInstruction HLInstructionFromCurrentMachineState(float commandTime)
        {
            var platePosition = _machineModel.HexaPlateTransform.position;
            var plateRotation = _machineModel.HexaPlateTransform.rotation;
            var hlMachineState = new HLMachineState(platePosition, plateRotation);
            var instruction = new HLInstruction(hlMachineState, commandTime);
            return instruction;
        }
    }
}