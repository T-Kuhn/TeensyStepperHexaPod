using MachineSimulator.Sequencing;
using UniRx;
using UnityEngine;

namespace MachineSimulator.UI
{
    public sealed class UiSequencingPresenter : MonoBehaviour
    {
        [SerializeField] private UiView _view;
        [SerializeField] private SequenceCreator _sequenceCreator;
        
        private void Awake()
        {
            _view.OnAddInstructionClicked.Subscribe(_ => Debug.Log("test")).AddTo(this);
        }

    }
}