using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;

namespace WPF_Student_Management.ViewModels
{
    public class AccountManagementViewModel : INotifyPropertyChanged
    {
        // --- PROPETIES ---
        private ObservableCollection<Staff> _staffList;
        public ObservableCollection<Staff> StaffList
        {
            get => _staffList;
            set { _staffList = value; OnPropertyChanged(); }
        }

        private Staff _selectedStaff;
        public Staff SelectedStaff
        {
            get => _selectedStaff;
            set
            {
                _selectedStaff = value;
                OnPropertyChanged();
                UpdateAccountInfo();
            }
        }

        public ObservableCollection<Role> RoleList { get; set; }

        private int _selectedRoleId;
        public int SelectedRoleId
        {
            get => _selectedRoleId;
            set { _selectedRoleId = value; OnPropertyChanged(); }
        }

        private string _username;
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        // --- COMMANDS ---
        public ICommand LoadCommand { get; }
        public ICommand SaveAccountCommand { get; }

        // --- CONSTRUCTOR ---
        public AccountManagementViewModel()
        {
            LoadCommand = new RelayCommand(p => ExecuteLoad());
            SaveAccountCommand = new RelayCommand(p => ExecuteSave(), p => SelectedStaff != null);

            ExecuteLoad();
        }

        // --- METHODS ---
        private void ExecuteLoad()
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

            // Tìm Username của Account này để hiển thị lên TextBlock/TextBox
            string query = "SELECT Username FROM Account WHERE AccountID = @AccID";
            SqlParameter[] param = { new SqlParameter("@AccID", SelectedStaff.AccountId) };
            DataTable dt = DatabaseHelper.ExecuteQuery(query, param);

            if (dt.Rows.Count > 0)
            {
                Username = dt.Rows[0]["Username"].ToString() ?? "";
            }
        }

        private void ExecuteSave()
        {
            try
            {
                // Bảng Employee bắt buộc có AccountID (NOT NULL). 
                // Do đó, ta chỉ cần duy nhất logic UPDATE RoleID.
                string updateQuery = "UPDATE Account SET RoleID = @RoleID WHERE AccountID = @AccID";

                SqlParameter[] updateParams = {
                    new SqlParameter("@RoleID", SelectedRoleId),
                    new SqlParameter("@AccID", SelectedStaff.AccountId)
                };

                if (DatabaseHelper.ExecuteNonQuery(updateQuery, updateParams) > 0)
                {
                    NotificationHelper.ShowConfirm("Cập nhật phân quyền thành công!");

                    // Cập nhật lại Object local để UI ko bị lag
                    SelectedStaff.RoleId = SelectedRoleId;

                    // Reload lại Grid để đảm bảo đồng bộ
                    ExecuteLoad();
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

        // --- INOTIFYPROPERTYCHANGED ---
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}