namespace Nexiara.Streaming.Models
{
    public enum TouchAction
    {
        Down,
        Move,
        Up,
        Tap,
        RightTap
    }

    public record TouchInput(
        float NormalizedX,
        float NormalizedY,
        TouchAction Action
    );
}