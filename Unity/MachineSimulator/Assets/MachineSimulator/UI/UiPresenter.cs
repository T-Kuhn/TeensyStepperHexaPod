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
            _view.OnUpButtonClicked
                .Subscribe(_ =>
                {
                    var commands = new List<LLInstruction>() { new LLInstruction(new LLMachineState(-20f, 20f, -20f, 20f, -20f, 20f), 1f, false) };
                    _realMachine.Instruct(commands);
                })
                .AddTo(this);

            _view.OnDownButtonClicked
                .Subscribe(_ =>
                {
                    var commands = new List<LLInstruction>() { new LLInstruction(new LLMachineState(0f, 0f, 0f, 0f, 0f, 0f), 1f, false) };
                    _realMachine.Instruct(commands);
                })
                .AddTo(this);
        }
    }
}