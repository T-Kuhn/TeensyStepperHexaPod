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
            _view.OnUpClicked
                .Subscribe(_ => _realMachine.Instruct(new LLMachineState(-20f, 20f, -20f, 20f, -20f, 20f).ToList(1f)))
                .AddTo(this);

            _view.OnDownClicked
                .Subscribe(_ => _realMachine.Instruct(new LLMachineState(0f, 0f, 0f, 0f, 0f, 0f).ToList(1f)))
                .AddTo(this);

            _view.OnM1PlusClicked
                .Subscribe(_ => _realMachine.Instruct(new LLMachineState(1f, 0f, 0f, 0f, 0f, 0f).ToList(0.1f)))
                .AddTo(this);

            _view.OnM1MinusClicked
                .Subscribe(_ => _realMachine.Instruct(new LLMachineState(-1f, 0f, 0f, 0f, 0f, 0f).ToList(0.1f)))
                .AddTo(this);
        }
    }
}