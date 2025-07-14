using System;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

namespace MachineSimulator.UI
{
    public sealed class UiView : MonoBehaviour
    {
        private readonly Subject<Unit> _onUpClicked = new Subject<Unit>();
        private readonly Subject<Unit> _onOriginClicked = new Subject<Unit>();
        private readonly Subject<Unit> _onDownClicked = new Subject<Unit>();
        private readonly Subject<Unit> _onNormalSpeedClicked= new Subject<Unit>();
        private readonly Subject<Unit> _onDoubleSpeedClicked= new Subject<Unit>();
        private readonly Subject<Unit> _onQuatrupleSpeedClicked= new Subject<Unit>();
        

        private readonly Subject<Unit> _onM1PlusClicked = new Subject<Unit>();
        private readonly Subject<Unit> _onM1MinusClicked = new Subject<Unit>();
        private readonly Subject<Unit> _onM2PlusClicked = new Subject<Unit>();
        private readonly Subject<Unit> _onM2MinusClicked = new Subject<Unit>();
        private readonly Subject<Unit> _onM3PlusClicked = new Subject<Unit>();
        private readonly Subject<Unit> _onM3MinusClicked = new Subject<Unit>();
        private readonly Subject<Unit> _onM4PlusClicked = new Subject<Unit>();
        private readonly Subject<Unit> _onM4MinusClicked = new Subject<Unit>();
        private readonly Subject<Unit> _onM5PlusClicked = new Subject<Unit>();
        private readonly Subject<Unit> _onM5MinusClicked = new Subject<Unit>();
        private readonly Subject<Unit> _onM6PlusClicked = new Subject<Unit>();
        private readonly Subject<Unit> _onM6MinusClicked = new Subject<Unit>();
        
        private readonly Subject<Unit> _onApplyOffsetClicked = new Subject<Unit>();

        public IObservable<Unit> OnUpClicked => _onUpClicked;
        public IObservable<Unit> OnDownClicked => _onDownClicked;
        public IObservable<Unit> OnOriginClicked => _onOriginClicked;
        
        public IObservable<Unit> OnNormalSpeedClicked => _onNormalSpeedClicked;
        public IObservable<Unit> OnDoubleSpeedClicked => _onDoubleSpeedClicked;
        public IObservable<Unit> OnQuatrupleSpeedClicked => _onQuatrupleSpeedClicked;

        public IObservable<Unit> OnM1PlusClicked => _onM1PlusClicked;
        public IObservable<Unit> OnM1MinusClicked => _onM1MinusClicked;
        public IObservable<Unit> OnM2PlusClicked => _onM2PlusClicked;
        public IObservable<Unit> OnM2MinusClicked => _onM2MinusClicked;
        public IObservable<Unit> OnM3PlusClicked => _onM3PlusClicked;
        public IObservable<Unit> OnM3MinusClicked => _onM3MinusClicked;
        public IObservable<Unit> OnM4PlusClicked => _onM4PlusClicked;
        public IObservable<Unit> OnM4MinusClicked => _onM4MinusClicked;
        public IObservable<Unit> OnM5PlusClicked => _onM5PlusClicked;
        public IObservable<Unit> OnM5MinusClicked => _onM5MinusClicked;
        public IObservable<Unit> OnM6PlusClicked => _onM6PlusClicked;
        public IObservable<Unit> OnM6MinusClicked => _onM6MinusClicked;
        
        public IObservable<Unit> OnApplyOffsetClicked => _onApplyOffsetClicked;

        private void Awake()
        {
            var uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;

            RegisterButton(root.Q<Button>("DownButton"), _onDownClicked);
            RegisterButton(root.Q<Button>("OriginButton"), _onOriginClicked);
            RegisterButton(root.Q<Button>("UpButton"), _onUpClicked);
            RegisterButton(root.Q<Button>("DoubleSpeedButton"), _onDoubleSpeedClicked);
            RegisterButton(root.Q<Button>("QuatrupleSpeedButton"), _onQuatrupleSpeedClicked);
            RegisterButton(root.Q<Button>("NormalSpeedButton"), _onNormalSpeedClicked);

            RegisterButton(root.Q<VisualElement>("M1").Q<Button>("CalibratePlus"), _onM1PlusClicked);
            RegisterButton(root.Q<VisualElement>("M1").Q<Button>("CalibrateMinus"), _onM1MinusClicked);
            RegisterButton(root.Q<VisualElement>("M2").Q<Button>("CalibratePlus"), _onM2PlusClicked);
            RegisterButton(root.Q<VisualElement>("M2").Q<Button>("CalibrateMinus"), _onM2MinusClicked);
            RegisterButton(root.Q<VisualElement>("M3").Q<Button>("CalibratePlus"), _onM3PlusClicked);
            RegisterButton(root.Q<VisualElement>("M3").Q<Button>("CalibrateMinus"), _onM3MinusClicked);
            RegisterButton(root.Q<VisualElement>("M4").Q<Button>("CalibratePlus"), _onM4PlusClicked);
            RegisterButton(root.Q<VisualElement>("M4").Q<Button>("CalibrateMinus"), _onM4MinusClicked);
            RegisterButton(root.Q<VisualElement>("M5").Q<Button>("CalibratePlus"), _onM5PlusClicked);
            RegisterButton(root.Q<VisualElement>("M5").Q<Button>("CalibrateMinus"), _onM5MinusClicked);
            RegisterButton(root.Q<VisualElement>("M6").Q<Button>("CalibratePlus"), _onM6PlusClicked);
            RegisterButton(root.Q<VisualElement>("M6").Q<Button>("CalibrateMinus"), _onM6MinusClicked);
            
            RegisterButton(root.Q<Button>("ApplyOffset"), _onApplyOffsetClicked);
        }

        private void RegisterButton(Button button, Subject<Unit> subject)
        {
            Observable
                .FromEvent(x => button.clicked += x, x => button.clicked -= x)
                .Subscribe(_ => subject.OnNext(Unit.Default))
                .AddTo(this);
        }
    }
}