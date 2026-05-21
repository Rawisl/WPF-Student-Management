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
        public bool IsReadOnly => CurrentUser.Instance.Role != (UserRole)2;

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

        private bool CanModify() => !IsReadOnly;

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

            bool result = NotificationHelper.ShowConfirm($"Bạn có chắc chắn muốn xóa HOÀN TOÀN nhân viên '{staff.FullName}' khỏi hệ thống không?\n\nHành động này không thể hoàn tác!");

            if (result)
            {
                try
                {
                    int accountIdToDelete = staff.AccountId;
                    bool isDeleted = Staff.DeleteStaff(staff.StaffId);

                    if (isDeleted)
                    {
                        Account.DeleteAccount(accountIdToDelete);
                        NotificationHelper.ShowSuccess("Đã xóa nhân viên thành công!");
                        LoadData();
                    }
                }
                catch (SqlException sqlEx)
                {
                    if (sqlEx.Number == 547)
                        NotificationHelper.ShowWarning("Xóa dữ liệu thất bại!\nNhân viên này đang có dữ liệu liên kết ở bảng khác.");
                    else
                        NotificationHelper.ShowError("Lỗi cơ sở dữ liệu:\n" + sqlEx.Message);
                }
                catch (Exception ex)
                {
                    NotificationHelper.ShowError("Không thể xóa nhân viên.\nLỗi chi tiết: " + ex.Message);
                }
            }
        }
    }
}