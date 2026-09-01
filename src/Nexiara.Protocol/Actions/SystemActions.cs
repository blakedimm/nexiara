namespace Nexiara.Protocol.Actions
{
    /// <summary>
    /// Изменение уровня громкости системного микшера.
    /// </summary>
    public record SetVolumeAction(int Level, bool IsMuted = false) : INexiaraAction
    {
        public string ActionType => "system.set_volume";
    }

    /// <summary>
    /// Запуск приложения или открытие файла/ссылки.
    /// </summary>
    public record RunAppAction(string ExecutablePath, string Arguments = "") : INexiaraAction
    {
        public string ActionType => "system.run_app";
    }

    /// <summary>
    /// Блокировка сеанса пользователя (Win+L).
    /// </summary>
    public record LockPcAction() : INexiaraAction
    {
        public string ActionType => "system.lock_pc";
    }

    /// <summary>
    /// Перезагрузка или выключение устройства.
    /// </summary>
    public record SystemPowerAction(string Mode = "shutdown", int DelaySeconds = 0) : INexiaraAction
    {
        public string ActionType => "system.power";
    }

    /// <summary>
    /// Запрос списка активных процессов.
    /// </summary>
    public record GetProcessesRequestAction() : INexiaraAction
    {
        public string ActionType => "system.get_processes";
    }

    /// <summary>
    /// Завершение конкретного процесса по PID.
    /// </summary>
    public record KillProcessAction(int ProcessId) : INexiaraAction
    {
        public string ActionType => "system.kill_process";
    }
}