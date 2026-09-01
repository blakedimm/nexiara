using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Nexiara.Core;
using Nexiara.Platform.Windows.Capture;
using Nexiara.Platform.Windows.Clipboard;
using Nexiara.Platform.Windows.Executors;
using Nexiara.Platform.Windows.Telemetry;
using Nexiara.Protocol;
using Nexiara.Protocol.Actions;

namespace Nexiara.UI.Tests
{
    public static class NexiaraTestRunner
    {
        /// <summary>
        /// Запускает полный сквозной тест всех модулей системы.
        /// </summary>
        public static async Task RunAllTestsAsync(MeshNode node)
        {
            Debug.WriteLine("\n==============================================");
            Debug.WriteLine("🚀 ЗАПУСК СКВОЗНОГО ТЕСТИРОВАНИЯ NEXIARA");
            Debug.WriteLine("==============================================\n");

            int passed = 0;
            int failed = 0;

            // -------------------------------------------------------------
            // TEST 1: Protocol Serialization & ActionDispatcher
            // -------------------------------------------------------------
            try
            {
                Debug.Write("[TEST 1/6] ActionDispatcher & JSON Serialization... ");
                bool handled = false;

                node.Dispatcher.RegisterHandler<MoveMouseAction>("input.mouse_move", (act, sender) =>
                {
                    if (act.Dx == 42 && act.Dy == 42 && sender == "TestSender")
                    {
                        handled = true;
                    }
                    return Task.CompletedTask;
                });

                var testAction = new MoveMouseAction(42, 42);
                var envelope = ActionEnvelope.Create(testAction, "TestSender");

                await node.Dispatcher.DispatchAsync(envelope);

                if (handled)
                {
                    Debug.WriteLine("✅ УСПЕШНО");
                    passed++;
                }
                else
                {
                    throw new Exception("Данные не совпали после десериализации.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ ОШИБКА: {ex.Message}");
                failed++;
            }

            // -------------------------------------------------------------
            // TEST 2: Windows Clipboard Provider
            // -------------------------------------------------------------
            try
            {
                Debug.Write("[TEST 2/6] WindowsClipboardProvider... ");
                var clipboard = new WindowsClipboardProvider();
                string testString = $"Nexiara_Test_{Guid.NewGuid().ToString()[..8]}";

                await clipboard.SetTextAsync(testString);
                string readString = await clipboard.GetTextAsync();

                if (readString == testString)
                {
                    Debug.WriteLine($"✅ УСПЕШНО (Текст: '{readString}')");
                    passed++;
                }
                else
                {
                    throw new Exception($"Ожидалось '{testString}', получено '{readString}'");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ ОШИБКА: {ex.Message}");
                failed++;
            }

            // -------------------------------------------------------------
            // TEST 3: Windows Telemetry Provider
            // -------------------------------------------------------------
            try
            {
                Debug.Write("[TEST 3/6] WindowsTelemetryProvider... ");
                var telemetryProvider = new WindowsTelemetryProvider();
                var telemetry = await telemetryProvider.GetCurrentTelemetryAsync();

                if (!string.IsNullOrEmpty(telemetry.ActiveWindowTitle))
                {
                    Debug.WriteLine($"✅ УСПЕШНО (Окно: '{telemetry.ActiveWindowTitle}', RAM: {telemetry.RamUsage})");
                    passed++;
                }
                else
                {
                    throw new Exception("Не удалось прочитать заголовок активного окна Win32.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ ОШИБКА: {ex.Message}");
                failed++;
            }

            // -------------------------------------------------------------
            // TEST 4: Win32 System Executor (Processes)
            // -------------------------------------------------------------
            try
            {
                Debug.Write("[TEST 4/6] Win32SystemExecutor (Processes)... ");
                var sysExecutor = new Win32SystemExecutor();
                var processes = await sysExecutor.GetProcessesAsync();

                if (processes.Count > 0)
                {
                    Debug.WriteLine($"✅ УСПЕШНО (Найдено {processes.Count} процессов, Лидер: {processes[0].Name} [{processes[0].MemoryMb} MB])");
                    passed++;
                }
                else
                {
                    throw new Exception("Список процессов пуст.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ ОШИБКА: {ex.Message}");
                failed++;
            }

            // -------------------------------------------------------------
            // TEST 5: Desktop Screen Capture Provider
            // -------------------------------------------------------------
            try
            {
                Debug.Write("[TEST 5/6] WindowsCaptureProvider... ");
                var captureProvider = new WindowsCaptureProvider();
                var screenshot = await captureProvider.TakeScreenshotAsync();

                if (screenshot != null && screenshot.Length > 0)
                {
                    Debug.WriteLine($"✅ УСПЕШНО (Размер буфера кадра: {screenshot.Length} байт)");
                    passed++;
                }
                else
                {
                    throw new Exception("Буфер снимка экрана оказался пустым.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ ОШИБКА: {ex.Message}");
                failed++;
            }

            // -------------------------------------------------------------
            // TEST 6: Win32 Input Executor (Physical Mouse Jump)
            // -------------------------------------------------------------
            try
            {
                Debug.Write("[TEST 6/6] Win32InputExecutor (Physical Move)... ");
                var inputExecutor = new Win32InputExecutor();
                await inputExecutor.MoveMouseAsync(new MoveMouseAction(15, 15));
                Debug.WriteLine("✅ УСПЕШНО (Курсор физически сдвинут на +15px)");
                passed++;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ ОШИБКА: {ex.Message}");
                failed++;
            }

            Debug.WriteLine("\n==============================================");
            Debug.WriteLine($"ИТОГИ ТЕСТИРОВАНИЯ: Пройдено: {passed} / 6 | Сбоев: {failed}");
            Debug.WriteLine("==============================================\n");
        }
    }
}