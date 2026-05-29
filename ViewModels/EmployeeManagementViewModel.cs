using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;
using WPF_Student_Management.Services;

namespace WPF_Student_Management.ViewModels
{

    public partial class EmployeeManagementViewModel : ObservableObject
    {
        public Visibility ActionVisibility => PermissionService.HasFeature(PermissionService.Feature.ManageEmployees)
                                      ? Visibility.Visible : Visibility.Collapsed;
        public bool IsReadOnly => !PermissionService.HasFeature(PermissionService.Feature.ManageEmployees);
        private bool CanModify() => !IsReadOnly;

        [ObservableProperty]
        private ObservableCollection<Staff> _staffList;

        public EmployeeManagementViewModel()
        {
            bool isDesignMode = DesignerProperties.GetIsInDesignMode(new DependencyObject());
            if (!isDesignMode)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    LoadData();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        [RelayCommand]
        private void LoadData()
        {
            try
            {
                var list = Staff.GetAllStaff();
                StaffList = new ObservableCollection<Staff>(list);
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi tải dữ liệu:\n" + ex.Message);
            }
        }

        [RelayCommand(CanExecute = nameof(CanModify))]
        private async void OpenAddDialog()
        {
            var newStaff = new Staff
            {
                StaffId = 0,
                AccountId = 0,
                FullName = "",
                Gender = "Nam",
                Status = "Active",
                HireDate = DateTime.Now,
                Specialization = null
            };

            // Khởi tạo ViewModel con, truyền dữ liệu và con trỏ hàm LoadData
            var detailVM = new EmployeeProfileDetailViewModel(newStaff, false, LoadData);
            var dialog = new Components.EmployeeProfileDetailUC { DataContext = detailVM };

            await DialogHost.Show(dialog, "RootDialog");
        }

        [RelayCommand(CanExecute = nameof(CanModify))]
        private async void Edit(Staff staff)
        {
            if (staff == null) return;

            var detailVM = new EmployeeProfileDetailViewModel(staff, false, LoadData);
            var dialog = new Components.EmployeeProfileDetailUC { DataContext = detailVM };

            await DialogHost.Show(dialog, "RootDialog");
        }

        [RelayCommand(CanExecute = nameof(CanModify))]
        private void Delete(Staff staff)
        {
            if (staff == null || staff.StaffId <= 0) return;

            if (staff.AccountId == CurrentUser.Instance.UserId)
            {
                NotificationHelper.ShowError("Không thể xóa nhân viên đang đăng nhập vào hệ thống!");
                return;
            }

            bool result = NotificationHelper.ShowConfirm($"Bạn có chắc chắn muốn vô hiệu hóa nhân viên '{staff.FullName}' không?\n\nTài khoản sẽ không thể đăng nhập, nhưng dữ liệu liên kết vẫn được giữ lại.");

            if (result)
            {
                try
                {
                    // Chặn xung đột: chỉ vô hiệu hóa nếu bản ghi này vẫn còn khớp với dữ liệu đang hiển thị trên lưới.
                    bool isDeactivated = Staff.DeactivateStaff(staff.CreateConcurrencySnapshot());

                    if (isDeactivated)
                    {
                        NotificationHelper.ShowSuccess("Đã vô hiệu hóa nhân viên thành công!");
                        LoadData();
                    }
                    else
                    {
                        NotificationHelper.ShowWarning("Dữ liệu nhân viên đã được thay đổi hoặc vô hiệu hóa bởi người dùng khác. Danh sách sẽ được tải lại.");
                        LoadData();
                    }
                }
                catch (SqlException sqlEx)
                {
                    NotificationHelper.ShowError("Lỗi cơ sở dữ liệu:\n" + sqlEx.Message);
                }
                catch (Exception ex)
                {
                    NotificationHelper.ShowError("Không thể vô hiệu hóa nhân viên.\nLỗi chi tiết: " + ex.Message);
                }
            }
        }
    }
}