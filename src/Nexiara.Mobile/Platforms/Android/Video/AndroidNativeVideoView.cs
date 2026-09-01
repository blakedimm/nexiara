using System;
using Android.Content;
using Android.Views;

namespace Nexiara.Mobile.Platforms.Android.Video
{
    public class AndroidNativeVideoView : SurfaceView, ISurfaceHolderCallback
    {
        public event Action<Surface>? OnSurfaceReady;
        public event Action? OnSurfaceDestroyedEvent;

        public Surface? NativeSurface { get; private set; }

        public AndroidNativeVideoView(Context context) : base(context)
        {
            Holder?.AddCallback(this);
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            NativeSurface = holder.Surface;
            if (NativeSurface != null && NativeSurface.IsValid)
            {
                OnSurfaceReady?.Invoke(NativeSurface);
            }
        }

        public void SurfaceChanged(ISurfaceHolder holder, global::Android.Graphics.Format format, int width, int height)
        {
            // Изменение размеров видеоповерхности
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            OnSurfaceDestroyedEvent?.Invoke();
            NativeSurface = null;
        }
    }
}