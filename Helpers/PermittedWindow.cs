using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WPF_Student_Management.Helpers
{
    public abstract class PermittedWindow : Window
    {
        // Define feature required to open this Window, to be implemented by derived classes
        protected abstract PermissionService.Feature? WindowFeature { get; }

        public PermittedWindow()
        {
            // Check permissions in the constructor, before the Window is shown
            if (WindowFeature.HasValue && !PermissionService.HasFeature(WindowFeature.Value))
            {
                // Or whatever app's way of showing permission errors is
                MessageBox.Show("Bạn không có quyền truy cập màn hình này!", "Lỗi quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }
        }
    }
}
