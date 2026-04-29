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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPF_Student_Management.Components
{
    /// <summary>
    /// Interaction logic for CustomTopBarUC.xaml
    /// </summary>
    public partial class CustomTopBarUC : UserControl
    {
        public static readonly DependencyProperty TitleTextProperty =
            DependencyProperty.Register("TitleText", typeof(string), typeof(CustomTopBarUC), new PropertyMetadata("Hệ thống quản lý học sinh"));

        public string TitleText
        {
            get { return (string)GetValue(TitleTextProperty); }
            set { SetValue(TitleTextProperty, value); }
        }

        public static readonly DependencyProperty ShowMaximizeButtonProperty =
            DependencyProperty.Register("ShowMaximizeButton", typeof(Visibility), typeof(CustomTopBarUC), new PropertyMetadata(Visibility.Visible));

        public Visibility ShowMaximizeButton
        {
            get { return (Visibility)GetValue(ShowMaximizeButtonProperty); }
            set { SetValue(ShowMaximizeButtonProperty, value); }
        }

        public CustomTopBarUC()
        {
            InitializeComponent();
        }

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            if (parentWindow == null) return;

            if (e.ClickCount == 2)
            {
                if (ShowMaximizeButton == Visibility.Visible)
                {
                    BtnMaximize_Click(sender, e);
                }
            }
            else if (e.ChangedButton == MouseButton.Left)
            {
                parentWindow.DragMove();
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null) parentWindow.WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                if (parentWindow.WindowState == WindowState.Maximized)
                {
                    parentWindow.WindowState = WindowState.Normal;
                    iconMaximize.Kind = MaterialDesignThemes.Wpf.PackIconKind.WindowMaximize;
                }
                else
                {
                    parentWindow.WindowState = WindowState.Maximized;
                    iconMaximize.Kind = MaterialDesignThemes.Wpf.PackIconKind.WindowRestore;
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            parentWindow?.Close();
        }
    }
}
