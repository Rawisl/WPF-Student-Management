using System;
using System.Collections;
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
    /// Interaction logic for StudentProfileUC.xaml
    /// </summary>
    public partial class StudentProfileUC : UserControl
    {
        public StudentProfileUC()
        {
            InitializeComponent();
        }

        //khúc này để ẩn/hiện cái thùng rác
        public Visibility DeleteButtonVisibility
        {
            get { return (Visibility)GetValue(DeleteButtonVisibilityProperty); }
            set { SetValue(DeleteButtonVisibilityProperty, value); }
        }
        public static readonly DependencyProperty DeleteButtonVisibilityProperty =
            DependencyProperty.Register("DeleteButtonVisibility", typeof(Visibility), typeof(StudentProfileUC), new PropertyMetadata(Visibility.Visible));

        // 1. Lỗ cắm cho Danh sách học sinh
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(StudentProfileUC));

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        // 2. Lỗ cắm cho lệnh Sửa
        public static readonly DependencyProperty EditCommandProperty =
            DependencyProperty.Register("EditCommand", typeof(ICommand), typeof(StudentProfileUC));

        public ICommand EditCommand
        {
            get { return (ICommand)GetValue(EditCommandProperty); }
            set { SetValue(EditCommandProperty, value); }
        }

        // 3. Lỗ cắm cho lệnh Xóa
        public static readonly DependencyProperty DeleteCommandProperty =
            DependencyProperty.Register("DeleteCommand", typeof(ICommand), typeof(StudentProfileUC));

        public ICommand DeleteCommand
        {
            get { return (ICommand)GetValue(DeleteCommandProperty); }
            set { SetValue(DeleteCommandProperty, value); }
        }

        // 4. Lỗ cắm Ẩn/Hiện cột Thao Tác (Mặc định là True - Có hiện)
        public static readonly DependencyProperty ShowActionColumnProperty =
            DependencyProperty.Register(
                "ShowActionColumn",
                typeof(bool),
                typeof(StudentProfileUC),
                new PropertyMetadata(true, OnShowActionChanged));

        public bool ShowActionColumn
        {
            get { return (bool)GetValue(ShowActionColumnProperty); }
            set { SetValue(ShowActionColumnProperty, value); }
        }

        // Hàm này sẽ tự chạy khi công tắc bị gạt
        private static void OnShowActionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StudentProfileUC uc)
            {
                bool isShow = (bool)e.NewValue;
                // Gọi thẳng tên cột ra và cho nó "bay màu" nếu isShow = false
                uc.colAction.Visibility = isShow ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
