using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Nexiara.Abstractions;
using Nexiara.Protocol.Actions;

namespace Nexiara.Platform.Windows.Executors
{
    public class Win32SystemExecutor : ISystemExecutor
    {
        [DllImport("user32.dll")]
        private static extern bool LockWorkStation();

        public Task SetVolumeAsync(SetVolumeAction action)
        {
            // Изменение громкости системного микшера Windows
            return Task.CompletedTask;
        }

        public Task RunAppAsync(RunAppAction action)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = action.ExecutablePath,
                    Arguments = action.Arguments,
                    UseShellExecute = true
                });
            }
            catch { }
            return Task.CompletedTask;
        }

        public Task LockPcAsync(LockPcAction action)
        {
            LockWorkStation();
            return Task.CompletedTask;
        }

        public Task HandlePowerActionAsync(SystemPowerAction action)
        {
            string flag = action.Mode.ToLower() switch
            {
                "restart" => "/r /t 0",
                _ => "/s /t 0"
            };
            Process.Start("shutdown", flag);
            return Task.CompletedTask;
        }

        public Task<List<ProcessInfo>> GetProcessesAsync()
        {
            var list = Process.GetProcesses()
                .Select(p =>
                {
                    long mem = 0;
                    try { mem = p.WorkingSet64 / (1024 * 1024); } catch { }
                    return new ProcessInfo(p.Id, p.ProcessName, mem);
                })
                .OrderByDescending(p => p.MemoryMb)
                .Take(30)
                .ToList();

            return Task.FromResult(list);
        }

        public Task KillProcessAsync(int processId)
        {
            try
            {
                var p = Process.GetProcessById(processId);
                p.Kill();
            }
            catch { }
            return Task.CompletedTask;
        }
    }
}