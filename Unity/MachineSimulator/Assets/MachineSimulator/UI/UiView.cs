using System;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

namespace MachineSimulator.UI
{
    public sealed class UiView : MonoBehaviour
    {
        private readonly Subject<Unit> _onUpButtonClicked = new Subject<Unit>();
        private readonly Subject<Unit> _onDownButtonClicked = new Subject<Unit>();

        public IObservable<Unit> OnUpButtonClicked => _onUpButtonClicked;
        public IObservable<Unit> OnDownButtonClicked => _onDownButtonClicked;

        private void Awake()
        {
            var uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;
            var upButton = root.Q<Button>("UpButton");

            Observable
                .FromEvent(x => upButton.clicked += x, x => upButton.clicked -= x)
                .Subscribe(_ => _onUpButtonClicked.OnNext(Unit.Default))
                .AddTo(this);

            var downButton = root.Q<Button>("DownButton");
            Observable
                .FromEvent(x => downButton.clicked += x, x => downButton.clicked -= x)
                .Subscribe(_ => _onDownButtonClicked.OnNext(Unit.Default))
                .AddTo(this);
        }
    }
}