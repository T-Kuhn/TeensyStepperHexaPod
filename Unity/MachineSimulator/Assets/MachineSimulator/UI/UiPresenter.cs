using System;
using System.Collections.Generic;
using MachineSimulator.Machine;
using UniRx;
using UnityEngine;

namespace MachineSimulator.UI
{
    public sealed class UiPresenter : MonoBehaviour
    {
        [SerializeField] private UiView _view;
        [SerializeField] private RealMachine _realMachine;

        private float _currentCommandTime = 1f;
        private readonly float _defaultCommandTime = 0.7f;

        private void Awake()
        {
            var upState = new LLMachineState(-30f, 30f, -30f, 30f, -30f, 30f);
            var originState = new LLMachineState(-10f, 10f, -10f, 10f, -10f, 10f);

            var upOriginManyTimes = new List<LLMachineState>()
            {
                upState, originState,
                upState, originState,
                upState, originState,
                upState, originState,
                upState, originState,
                upState, originState,
                upState, originState,
                upState, originState,
                upState, originState,
                upState, originState,
                upState, originState,
                upState, originState,
                upState, originState,
                upState, originState,
                upState, originState,
                upState, originState,
                upState, originState,
                upState, originState,
                upState, originState,
                upState, originState
            };

            Register(_view.OnUpOriginClicked, () => new List<LLMachineState>() { upState, originState }.ToList(_currentCommandTime));
            Register(_view.OnUpOriginManyTimesClicked, () => upOriginManyTimes.ToList(_currentCommandTime));
            Register(_view.OnUpClicked, () => upState.ToList(_currentCommandTime));
            Register(_view.OnOriginClicked, () => originState.ToList(_currentCommandTime));
            Register(_view.OnDownClicked, Constants.OriginMachineState.ToList(_currentCommandTime));

            _view.OnDoubleSpeedClicked
                .Subscribe(_ => { _currentCommandTime = _defaultCommandTime / 2f; })
                .AddTo(this);

            _view.OnQuatrupleSpeedClicked
                .Subscribe(_ => { _currentCommandTime = _defaultCommandTime / 4f; })
                .AddTo(this);

            _view.OnNormalSpeedClicked
                .Subscribe(_ => { _currentCommandTime = _defaultCommandTime; })
                .AddTo(this);


            var amount = 0.1f;
            Register(_view.OnM1PlusClicked, new LLMachineState(-amount, 0f, 0f, 0f, 0f, 0f).ToList(0.1f, true));
            Register(_view.OnM1MinusClicked, new LLMachineState(amount, 0f, 0f, 0f, 0f, 0f).ToList(0.1f, true));
            Register(_view.OnM2PlusClicked, new LLMachineState(0f, amount, 0f, 0f, 0f, 0f).ToList(0.1f, true));
            Register(_view.OnM2MinusClicked, new LLMachineState(0f, -amount, 0f, 0f, 0f, 0f).ToList(0.1f, true));
            Register(_view.OnM3PlusClicked, new LLMachineState(0f, 0f, -amount, 0f, 0f, 0f).ToList(0.1f, true));
            Register(_view.OnM3MinusClicked, new LLMachineState(0f, 0f, amount, 0f, 0f, 0f).ToList(0.1f, true));
            Register(_view.OnM4PlusClicked, new LLMachineState(0f, 0f, 0f, amount, 0f, 0f).ToList(0.1f, true));
            Register(_view.OnM4MinusClicked, new LLMachineState(0f, 0f, 0f, -amount, 0f, 0f).ToList(0.1f, true));
            Register(_view.OnM5PlusClicked, new LLMachineState(0f, 0f, 0f, 0f, -amount, 0f).ToList(0.1f, true));
            Register(_view.OnM5MinusClicked, new LLMachineState(0f, 0f, 0f, 0f, amount, 0f).ToList(0.1f, true));
            Register(_view.OnM6PlusClicked, new LLMachineState(0f, 0f, 0f, 0f, 0f, amount).ToList(0.1f, true));
            Register(_view.OnM6MinusClicked, new LLMachineState(0f, 0f, 0f, 0f, 0f, -amount).ToList(0.1f, true));

            Register(_view.OnApplyOffsetClicked, Constants.OffsetFromTableState.ToList(0.5f, true));
        }

        private void Register(IObservable<Unit> observable, Func<List<LLInstruction>> getInstructions)
        {
            observable
                .Subscribe(_ => _realMachine.Instruct(getInstructions()))
                .AddTo(this);
        }

        private void Register(IObservable<Unit> observable, List<LLInstruction> instructions)
        {
            observable
                .Subscribe(_ => _realMachine.Instruct(instructions))
                .AddTo(this);
        }
    }
}