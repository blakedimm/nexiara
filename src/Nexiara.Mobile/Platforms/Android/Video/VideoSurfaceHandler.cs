using Microsoft.Maui.Handlers;
using Nexiara.Mobile.Pages.Controls;

namespace Nexiara.Mobile.Platforms.Android.Video
{
    public class VideoSurfaceHandler : ViewHandler<VideoSurface, AndroidNativeVideoView>
    {
        public static PropertyMapper<VideoSurface, VideoSurfaceHandler> PropertyMapper = new(ViewMapper);

        // Статический экземпляр аппаратного декодера, доступный для передачи кадров из сети
        public static H264HardwareDecoder Decoder { get; } = new H264HardwareDecoder();

        public VideoSurfaceHandler() : base(PropertyMapper) { }

        protected override AndroidNativeVideoView CreatePlatformView()
        {
            var nativeView = new AndroidNativeVideoView(Context);

            // Как только нативная поверхность Android готова — запускаем декодер
            nativeView.OnSurfaceReady += (surface) =>
            {
                Decoder.Initialize(surface, 1280, 720);
            };

            // При уничтожении поверхности — очищаем ресурсы декодера
            nativeView.OnSurfaceDestroyedEvent += () =>
            {
                Decoder.Dispose();
            };

            return nativeView;
        }

        protected override void DisconnectHandler(AndroidNativeVideoView platformView)
        {
            Decoder.Dispose();
            base.DisconnectHandler(platformView);
        }
    }
}