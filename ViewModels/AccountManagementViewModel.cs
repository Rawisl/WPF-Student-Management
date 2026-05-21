using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Data;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;

namespace WPF_Student_Management.ViewModels
{
    // ĐÃ FIX: Kế thừa ObservableObject thay vì tự viết INotifyPropertyChanged
    public partial class AccountManagementViewModel : ObservableObject
    {
        // --- PROPETIES (Dùng [ObservableProperty] để code tự sinh getter/setter) ---

        [ObservableProperty]
        private ObservableCollection<Staff> _staffList;

        [ObservableProperty]
        private ObservableCollection<Role> _roleList;

        [ObservableProperty]
        private Staff _selectedStaff;

        [ObservableProperty]
        private int _selectedRoleId;

        [ObservableProperty]
        private string _username;

        // --- CONSTRUCTOR ---
        public AccountManagementViewModel()
        {
            // Tự động load dữ liệu khi khởi tạo
            Load();
        }

        // --- HÀM LẮNG NGHE SỰ THAY ĐỔI (Tự động kích hoạt khi SelectedStaff thay đổi) ---
        partial void OnSelectedStaffChanged(Staff value)
        {
            UpdateAccountInfo();
            // Đánh thức hàm kiểm tra điều kiện của nút Save để nó sáng/tối tùy lúc
            SaveAccountCommand.NotifyCanExecuteChanged();
        }

        // --- COMMANDS ---

        [RelayCommand]
        private void Load()
        {
            StaffList = new ObservableCollection<Staff>(Staff.GetAllStaff());
            RoleList = new ObservableCollection<Role>(Role.GetAllRoles());
        }

        private void UpdateAccountInfo()
        {
            if (SelectedStaff == null)
            {
                Username = "";
                return;
            }

            // Đồng bộ RoleID từ Staff sang UI Dropdown
            SelectedRoleId = SelectedStaff.RoleId;

            // Tìm Username của Account này để hiển thị
            string query = "SELECT Username FROM Account WHERE AccountID = @AccID";
            SqlParameter[] param = { new SqlParameter("@AccID", SelectedStaff.AccountId) };
            DataTable dt = DatabaseHelper.ExecuteQuery(query, param);

            if (dt.Rows.Count > 0)
            {
                Username = dt.Rows[0]["Username"].ToString() ?? "";
            }
        }

        // Hàm kiểm tra điều kiện: Nút Save chỉ sáng khi có Staff được chọn
        private bool CanSaveAccount() => SelectedStaff != null;

        [RelayCommand(CanExecute = nameof(CanSaveAccount))]
        private void SaveAccount()
        {
            try
            {
                string updateQuery = "UPDATE Account SET RoleID = @RoleID WHERE AccountID = @AccID";

                SqlParameter[] updateParams = {
                    new SqlParameter("@RoleID", SelectedRoleId),
                    new SqlParameter("@AccID", SelectedStaff.AccountId)
                };

                if (DatabaseHelper.ExecuteNonQuery(updateQuery, updateParams) > 0)
                {
                    NotificationHelper.ShowConfirm("Cập nhật phân quyền thành công!");

                    SelectedStaff.RoleId = SelectedRoleId;
                    Load(); // Reload Grid
                }
                else
                {
                    NotificationHelper.ShowError("Không tìm thấy tài khoản để cập nhật!");
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi hệ thống: " + ex.Message);
            }
        }
    }
}