using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPF_Student_Management
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new WPF_Student_Management.ViewModels.MainViewModel();
        }
        // Nút Phóng to / Phục hồi (Maximize / Restore)
        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                // Đang to thì thu nhỏ lại, đổi icon thành 1 ô vuông
                this.WindowState = WindowState.Normal;
                iconMaximize.Kind = MaterialDesignThemes.Wpf.PackIconKind.WindowMaximize;
            }
            else
            {
                // Đang nhỏ thì phóng to full màn, đổi icon thành 2 ô vuông
                this.WindowState = WindowState.Maximized;
                iconMaximize.Kind = MaterialDesignThemes.Wpf.PackIconKind.WindowRestore;
            }
        }

        // Bổ sung thêm tính năng: Kích đúp chuột vào thanh tiêu đề để phóng to/thu nhỏ
        private void TopBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                BtnMaximize_Click(sender, e); // Gọi luôn hàm trên cho tiện
            }
            else if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                this.DragMove();
            }
        }

        // Nút thu nhỏ
        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // Nút tắt App
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}