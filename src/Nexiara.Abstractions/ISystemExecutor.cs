using System.Collections.Generic;
using System.Threading.Tasks;
using Nexiara.Protocol.Actions;

namespace Nexiara.Abstractions
{
    public record ProcessInfo(int Id, string Name, long MemoryMb);

    public interface ISystemExecutor
    {
        Task SetVolumeAsync(SetVolumeAction action);
        Task RunAppAsync(RunAppAction action);
        Task LockPcAsync(LockPcAction action);
        Task HandlePowerActionAsync(SystemPowerAction action);
        Task<List<ProcessInfo>> GetProcessesAsync();
        Task KillProcessAsync(int processId);
    }
}