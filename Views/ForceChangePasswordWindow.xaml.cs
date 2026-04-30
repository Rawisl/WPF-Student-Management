using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPF_Student_Management.Views
{
    /// <summary>
    /// Interaction logic for ForceChangePasswordWindow.xaml
    /// </summary>
    public partial class ForceChangePasswordWindow : Window
    {
        public ForceChangePasswordWindow()
        {
            InitializeComponent();
        }
        private void ForceChangePasswordWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Tìm component TopBar (đang nằm ở vị trí đầu tiên trong Grid)
            var topBar = (Components.CustomTopBarUC)((System.Windows.Controls.Grid)((System.Windows.Controls.Border)this.Content).Child).Children[0];

            // Ẩn nút Maximize đi
            topBar.btnMaximize.Visibility = Visibility.Collapsed;
            // Đổi lại title của topbar
            topBar.txtTitle.Text = "Đăng nhập phần mềm";
        }
    }
}
