using MachineSimulator.SerialCommunication;
using UniRx;
using UnityEngine;

namespace MachineSimulator.UI
{
    public sealed class UiPresenter : MonoBehaviour
    {
        [SerializeField] private UiView _view;
        [SerializeField] private SerialInterface _serialInterface;

        private void Awake()
        {
            _view.OnUpButtonClicked
                .Subscribe(_ => _serialInterface.Send("0.11941:0.11941:0.11941:0.11941:1.15000\n"))
                .AddTo(this);

            _view.OnDownButtonClicked
                .Subscribe(_ => _serialInterface.Send("0.0:0.0:0.0:0.0:1.15000\n"))
                .AddTo(this);
        }
    }
}