using System.Threading.Tasks;
using Nexiara.Protocol.Actions;

namespace Nexiara.Abstractions
{
    public interface IInputExecutor
    {
        Task MoveMouseAsync(MoveMouseAction action);
        Task ClickMouseAsync(MouseClickAction action);
        Task ScrollMouseAsync(MouseScrollAction action);
        Task SendKeyPressAsync(KeyPressAction action);
        Task ProcessTouchpadGestureAsync(TouchpadGestureAction action);
        Task TypeTextAsync(TypeTextAction action);
    }
}