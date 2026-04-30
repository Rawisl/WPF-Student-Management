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
using WPF_Student_Management.ViewModels;

namespace WPF_Student_Management.Views
{
    /// <summary>
    /// Interaction logic for ClassRosterView.xaml
    /// </summary>
    public partial class ClassRosterView : UserControl
    {
        public ClassRosterView()
        {
            InitializeComponent();
            this.Loaded += ClassRosterView_Loaded;
        }

        private void ClassRosterView_Loaded(object sender, RoutedEventArgs e)
        {
            // Kiểm tra xem Não (DataContext) đã được gắn chưa, nếu gắn rồi thì ép chạy RefreshData
            if (this.DataContext is ClassRosterViewModel vm)
            {
                vm.RefreshData();
            }
        }
    }
}
