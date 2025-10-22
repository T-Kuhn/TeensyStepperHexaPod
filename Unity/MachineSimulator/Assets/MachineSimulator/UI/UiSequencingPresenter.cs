using System.Threading;
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

        private CancellationTokenSource _cts = new();

        private void Awake()
        {
            _view.OnLoadSequenceFromCodeClicked.Subscribe(_ =>
                {
                    // Teleport to origin
                    _sequenceCreator.ClearAll();
                    _machineModel.HexaPlateMover.TeleportToDefaultHeight();

                    // Sequence
                    SequenceFromCode.UpIntoTiltCircleMovementSequence(_machineModel, _sequenceCreator, _currentCommandTime);

                    // Back to origin
                    _machineModel.HexaPlateMover.TeleportToDefaultHeight();
                    _sequenceCreator.Add(SequenceFromCode.HLInstructionFromCurrentMachineState(_machineModel, _currentCommandTime));
                }
            ).AddTo(this);

            _view.OnPlaybackAsyncClicked.Subscribe(_ => { SequenceFromCode.StartAsyncExecutionAsync(_machineModel, _sequenceCreator, _currentCommandTime, _cts.Token).Forget(); }).AddTo(this);

            _view.OnPlaybackAsyncOnMachineClicked.Subscribe(_ => { SequenceFromCode.StartAsyncExecutionAsync(_machineModel, _sequenceCreator, _currentCommandTime, _cts.Token, true).Forget(); }).AddTo(this);

            _view.OnStopAllAsyncCLicked.Subscribe(_ =>
            {
                _cts.Cancel();
                _cts = new CancellationTokenSource();
            }).AddTo(this);

            _view.OnAddInstructionClicked.Subscribe(_ => { _sequenceCreator.Add(SequenceFromCode.HLInstructionFromCurrentMachineState(_machineModel, _defaultCommandTime)); }).AddTo(this);

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

            _view.OnPlaybackStringedClicked.Subscribe(_ => _sequenceCreator.StartStringedPlayback()).AddTo(this);

            _view.OnSendStringedToMachineClicked.Subscribe(_ => _sequenceCreator.StartStringedPlayback(true)).AddTo(this);

            _view.OnTeleportToOriginClicked.Subscribe(_ => _machineModel.HexaPlateMover.TeleportToDefaultHeight()).AddTo(this);

            _view.OnClearAllClicked.Subscribe(_ => _sequenceCreator.ClearAll()).AddTo(this);
        }
    }
}