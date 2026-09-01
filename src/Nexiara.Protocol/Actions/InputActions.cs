using Nexiara.Protocol;

namespace Nexiara.Protocol.Actions
{
    /// <summary>
    /// Перемещение курсора мыши (относительное или абсолютное).
    /// </summary>
    public record MoveMouseAction(float Dx, float Dy, bool IsAbsolute = false) : INexiaraAction
    {
        public string ActionType => "input.mouse_move";
    }

    /// <summary>
    /// Клик кнопкой мыши.
    /// </summary>
    public record MouseClickAction(string Button = "Left", bool IsDoubleClick = false, bool IsDown = false, bool IsUp = false) : INexiaraAction
    {
        public string ActionType => "input.mouse_click";
    }

    /// <summary>
    /// Прокрутка колёсика мыши.
    /// </summary>
    public record MouseScrollAction(int DeltaX, int DeltaY) : INexiaraAction
    {
        public string ActionType => "input.mouse_scroll";
    }

    /// <summary>
    /// Нажатие или отпускание клавиши клавиатуры.
    /// </summary>
    public record KeyPressAction(string Key, bool IsKeyDown = true, bool Control = false, bool Alt = false, bool Shift = false) : INexiaraAction
    {
        public string ActionType => "input.key_press";
    }

    /// <summary>
    /// Сенсорный жест или ввод с тачпада смартфона/ноутбука.
    /// </summary>
    public record TouchpadGestureAction(string GestureType, float Dx, float Dy, float Scale = 1.0f) : INexiaraAction
    {
        public string ActionType => "input.touchpad_gesture";
    }
}

    /// <summary>
    /// Прямой ввод текста с клавиатуры смартфона.
    /// </summary>
    public record TypeTextAction(string Text) : INexiaraAction
    {
        public string ActionType => "input.type_text";
    }