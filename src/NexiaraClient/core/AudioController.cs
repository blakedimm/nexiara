using System;
using System.Runtime.InteropServices;

namespace NexiaraClient.Core
{
    public static class AudioController
    {
        [ComImport]
        [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioEndpointVolume
        {
            int NotImpl1(); int NotImpl2(); int NotImpl3(); int NotImpl4();
            int NotImpl5(); int NotImpl6(); int NotImpl7();
            int SetMasterVolumeLevelScalar(float fLevel, Guid pguidEventContext);
        }

        public static void SetVolume(int level)
        {
            try
            {
                float scalar = Math.Max(0.0f, Math.Min(1.0f, level / 100.0f));
                Type mmDeviceEnumeratorType = Type.GetTypeFromProgID("MMDeviceEnumerator.MMDeviceEnumerator");
                dynamic mmDeviceEnumerator = Activator.CreateInstance(mmDeviceEnumeratorType);
                dynamic defaultDevice = mmDeviceEnumerator.GetDefaultAudioEndpoint(0, 1);
                
                Guid iid = typeof(IAudioEndpointVolume).GUID;
                defaultDevice.Activate(ref iid, 23, 0, out object endpointVolume);
                
                var volumeManager = (IAudioEndpointVolume)endpointVolume;
                volumeManager.SetMasterVolumeLevelScalar(scalar, Guid.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка аудио: {ex.Message}");
            }
        }
    }
}