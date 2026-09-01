using Microsoft.Maui.Devices;

namespace Nexiara.Mobile.Services
{
    public static class HapticService
    {
        public static void Click()
        {
            try
            {
                if (HapticFeedback.Default.IsSupported)
                {
                    HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                }
            }
            catch { }
        }
    }
}