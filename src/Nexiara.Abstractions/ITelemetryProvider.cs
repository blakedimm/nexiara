using System.Threading.Tasks;

namespace Nexiara.Abstractions
{
    public record SystemTelemetry(
        string CpuLoad,
        string GpuLoad,
        string RamUsage,
        string ActiveWindowTitle,
        int BatteryPercentage
    );

    public interface ITelemetryProvider
    {
        Task<SystemTelemetry> GetCurrentTelemetryAsync();
    }
}