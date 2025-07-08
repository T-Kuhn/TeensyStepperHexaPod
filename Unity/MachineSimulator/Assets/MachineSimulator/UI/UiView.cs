using UnityEngine;
using UnityEngine.UIElements;

namespace MachineSimulator.UI
{
    public sealed class UiView : MonoBehaviour
    {
        private void Awake()
        {
            var uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;
            var button = root.Q<Button>();
            if (button != null)
            {
                button.clicked += OnButtonClicked;
            }
        }

        private void OnButtonClicked()
        {
            Debug.Log("ボタンがクリックされました");
        }
    }
}