using UnityEngine;
using UnityEngine.UIElements;

public class KeyboardHandler : MonoBehaviour
{
    public UIDocument uiDocument; // Assign UI Document dari inspector
    private VisualElement root;
    private VisualElement inputContainer;

    private float initialPositionY;
    private bool keyboardVisible = false;

    void Start()
    {
        root = uiDocument.rootVisualElement;
        inputContainer = root.Q<VisualElement>("container");

        // Simpan posisi awal
        initialPositionY = inputContainer.resolvedStyle.top;
    }

    void Update()
    {
        // Cek status keyboard
        if (TouchScreenKeyboard.visible && !keyboardVisible)
        {
            keyboardVisible = true;
            ShiftUI(true);
        }
        else if (!TouchScreenKeyboard.visible && keyboardVisible)
        {
            keyboardVisible = false;
            ShiftUI(false);
        }
    }

    private void ShiftUI(bool isKeyboardVisible)
    {
        float shiftAmount = isKeyboardVisible ? -200f : 0f;
        inputContainer.style.top = initialPositionY + shiftAmount;
    }
}
