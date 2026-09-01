using System.Windows;
using System.Windows.Input;

namespace Nexiara.UI
{
    public partial class MascotOverlayWindow : Window
    {
        public MascotOverlayWindow()
        {
            InitializeComponent();

            // Ставим окно в правый нижний угол экрана
            double desktopWorkingAreaRight = SystemParameters.WorkArea.Right;
            double desktopWorkingAreaBottom = SystemParameters.WorkArea.Bottom;
            Left = desktopWorkingAreaRight - Width - 20;
            Top = desktopWorkingAreaBottom - Height - 20;

            // Подписываемся на события обнаружения устройств в сети
            if (App.Node != null)
            {
                App.Node.Registry.OnNodeDiscovered += node =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        StatusText.Text = node.NodeId;
                    });
                };
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Позволяет свободно перетаскивать Элизу мышкой по всему экрану
            DragMove();
        }
    }
}