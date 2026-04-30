using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Input;
using WPF_Student_Management.Models;
using WPF_Student_Management.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;

namespace WPF_Student_Management.ViewModels
{
    public class AccountManagementViewModel : INotifyPropertyChanged
    {
        // Danh sách hiển thị trên bảng
        private ObservableCollection<Staff> _staffList;
        public ObservableCollection<Staff> StaffList
        {
            get => _staffList;
            set { _staffList = value; OnPropertyChanged(); }
        }

        // Nhân viên đang được chọn để chỉnh sửa
        private Staff _selectedStaff;
        public Staff SelectedStaff
        {
            get => _selectedStaff;
            set { _selectedStaff = value; OnPropertyChanged(); UpdateAccountInfo(); }
        }

        // Danh sách Role để chọn (GVBM, GVCN...)
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

        public ICommand LoadCommand { get; }
        public ICommand SaveAccountCommand { get; }

        public AccountManagementViewModel()
        {
            LoadCommand = new RelayCommand(p => ExecuteLoad());
            SaveAccountCommand = new RelayCommand(p => ExecuteSave(), p => SelectedStaff != null);

            ExecuteLoad();
        }

        private void ExecuteLoad()
        {
            // Tải danh sách nhân viên
            StaffList = new ObservableCollection<Staff>(Staff.GetAllStaff());
            // Tải danh sách Role từ DB
            RoleList = new ObservableCollection<Role>(Role.GetAllRoles());
        }

        private void UpdateAccountInfo()
        {
            if (SelectedStaff == null) return;

            // Tìm thông tin account hiện tại của nhân viên này trong DB
            string query = "SELECT Username, RoleID FROM Account WHERE AccountID = @AccID";
            SqlParameter[] param = { new SqlParameter("@AccID", SelectedStaff.AccountId) };
            DataTable dt = DatabaseHelper.ExecuteQuery(query, param);

            if (dt.Rows.Count > 0)
            {
                Username = dt.Rows[0]["Username"].ToString();
                SelectedRoleId = Convert.ToInt32(dt.Rows[0]["RoleID"]);
            }
            else
            {
                Username = ""; // Chưa có tài khoản
                SelectedRoleId = 4; // Mặc định là GVBM
            }
        }

        private void ExecuteSave()
        {
            try
            {
                // Kiểm tra xem nhân viên đã có account chưa
                string checkQuery = "SELECT COUNT(*) FROM Account WHERE AccountID = @AccID";
                SqlParameter[] checkParam = { new SqlParameter("@AccID", SelectedStaff.AccountId) };
                int exists = (int)DatabaseHelper.ExecuteQuery(checkQuery, checkParam).Rows[0][0];

                if (exists > 0)
                {
                    // UPDATE: ĐÁP ỨNG DoD - Cập nhật Role (Ví dụ chuyển sang GVCN)
                    string updateQuery = "UPDATE Account SET RoleID = @RoleID WHERE AccountID = @AccID";
                    SqlParameter[] updateParams = {
                        new SqlParameter("@RoleID", SelectedRoleId),
                        new SqlParameter("@AccID", SelectedStaff.AccountId)
                    };
                    DatabaseHelper.ExecuteNonQuery(updateQuery, updateParams);
                }
                else
                {
                    // INSERT: Cấp mới tài khoản (Username mặc định là NationalID)
                    // Dùng PasswordHasher đã có của nhóm
                    string hashedDefaultPass = PasswordHasher.HashPassword("123456");

                    // Bạn cần viết thêm logic INSERT Account ở đây...
                }

                NotificationHelper.ShowConfirm("Cập nhật tài khoản thành công!");
                ExecuteLoad();
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi: " + ex.Message);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}