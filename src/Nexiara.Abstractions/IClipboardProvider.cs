using System;
using System.Threading.Tasks;

namespace Nexiara.Abstractions
{
    public interface IClipboardProvider
    {
        event Action<string>? OnClipboardChanged;
        Task<string> GetTextAsync();
        Task SetTextAsync(string text);
    }
}