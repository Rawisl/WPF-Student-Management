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
    public partial class AccountManagementViewModel : ObservableObject
    {

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

        public AccountManagementViewModel()
        {
            Load();
        }

        partial void OnSelectedStaffChanged(Staff value)
        {
            UpdateAccountInfo();
            SaveAccountCommand.NotifyCanExecuteChanged();
        }


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

        //Nút Save chỉ sáng khi có Staff được chọn
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