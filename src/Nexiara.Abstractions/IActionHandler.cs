using System.Threading.Tasks;
using Nexiara.Protocol;

namespace Nexiara.Abstractions
{
    public interface IActionHandler<TAction> where TAction : INexiaraAction
    {
        Task ExecuteAsync(TAction action, string senderNodeId);
    }
}