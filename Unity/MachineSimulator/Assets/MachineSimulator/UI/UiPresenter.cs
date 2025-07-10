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

        private void Awake()
        {
            Register(_view.OnUpClicked, new LLMachineState(-10f, 10f, -10f, 10f, -10f, 10f).ToList(1f));
            Register(_view.OnOriginClicked, Constants.OriginMachineState.ToList(1f));
            Register(_view.OnDownClicked, new LLMachineState(10f, -10f, 10f, -10f, 10f, -10f).ToList(1f));
            
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
        }

        private void Register(IObservable<Unit> observable, List<LLInstruction> instructions)
        {
            observable
                .Subscribe(_ => _realMachine.Instruct(instructions))
                .AddTo(this);
        }
    }
}