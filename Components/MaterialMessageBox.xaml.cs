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
    /// Interaction logic for MaterialMessageBox.xaml
    /// </summary>
    public enum MsgType { Info, Success, Warning, Error, Confirm }

    public partial class MaterialMessageBox : Window
    {
        public bool Result { get; private set; } = false;

        public MaterialMessageBox(string title, string message, MsgType type)
        {
            InitializeComponent();
            txtTitle.Text = title.ToUpper();
            txtMessage.Text = message;

            // Đổi màu theo loại thông báo chuẩn Material Colors
            switch (type)
            {
                case MsgType.Success:
                    HeaderBorder.Background = new BrushConverter().ConvertFrom("#4CAF50") as Brush; // Xanh lá
                    btnOK.Background = new BrushConverter().ConvertFrom("#4CAF50") as Brush;
                    break;
                case MsgType.Error:
                    HeaderBorder.Background = new BrushConverter().ConvertFrom("#F44336") as Brush; // Đỏ
                    btnOK.Background = new BrushConverter().ConvertFrom("#F44336") as Brush;
                    break;
                case MsgType.Warning:
                    HeaderBorder.Background = new BrushConverter().ConvertFrom("#FF9800") as Brush; // Cam
                    btnOK.Background = new BrushConverter().ConvertFrom("#FF9800") as Brush;
                    break;
                case MsgType.Confirm:
                    HeaderBorder.Background = new BrushConverter().ConvertFrom("#2196F3") as Brush; // Xanh dương
                    btnOK.Background = new BrushConverter().ConvertFrom("#2196F3") as Brush;
                    btnCancel.Visibility = Visibility.Visible; // Hiện nút Hủy
                    break;
                default: // Info
                    HeaderBorder.Background = new BrushConverter().ConvertFrom("#00BCD4") as Brush; // Cyan
                    btnOK.Background = new BrushConverter().ConvertFrom("#00BCD4") as Brush;
                    break;
            }
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            this.Close();
        }
    }
}
