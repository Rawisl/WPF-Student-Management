using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;
using WPF_Student_Management.Services;
using WPF_Student_Management.Views;

namespace WPF_Student_Management.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        private string _username;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        private string _password;

        public LoginViewModel()
        {
        }

        private bool CanLogin() => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

        [RelayCommand(CanExecute = nameof(CanLogin))]
        private void Login(Window loginWindow)
        {
            try
            {
                string hashedPassword = PasswordHasher.HashPassword(Password);

                // ĐÃ FIX: JOIN bảng Role để lấy RoleName thay vì chỉ lấy RoleID
                string query = @"
                    SELECT a.*, r.RoleName 
                    FROM Account a
                    JOIN Role r ON a.RoleID = r.RoleID
                    WHERE a.Username = @Username AND a.PasswordHash = @PasswordHash";

                SqlParameter[] parameters = {
                    new SqlParameter("@Username", Username),
                    new SqlParameter("@PasswordHash", hashedPassword)
                };

                DataTable data = DatabaseHelper.ExecuteQuery(query, parameters);

                if (data.Rows.Count > 0)
                {
                    var row = data.Rows[0];
                    bool isActive = Convert.ToBoolean(row["IsActive"]);

                    if (!isActive)
                    {
                        NotificationHelper.ShowWarning("Tài khoản của bạn đã bị khóa!");
                        return;
                    }

                    int accountId = Convert.ToInt32(row["AccountID"]);
                    bool isRequiredChangePwd = Convert.ToBoolean(row["IsRequiredChangePassword"]);

                    // Lấy chữ RoleName từ DB và dùng hàm MapRole để chuyển thành Enum
                    string roleNameDB = row["RoleName"].ToString();
                    UserRole userRole = MapRoleNameToEnum(roleNameDB);

                    // Khởi tạo CurrentUser an toàn tuyệt đối
                    CurrentUser.Instance.Login(accountId, Username, userRole);

                    if (isRequiredChangePwd)
                    {
                        new ForceChangePasswordWindow().Show();
                    }
                    else
                    {
                        new MainWindow().Show();
                    }

                    loginWindow?.Close();
                }
                else
                {
                    NotificationHelper.ShowError("Thông tin đăng nhập không chính xác!");
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi kết nối cơ sở dữ liệu: " + ex.Message);
            }
        }

        // HÀM BIÊN DỊCH: Dịch từ chữ của Database sang Enum của C# (KHÔNG PHÂN BIỆT HOA THƯỜNG)
        private UserRole MapRoleNameToEnum(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName)) return UserRole.HocSinh;

            // Xóa khoảng trắng 2 đầu
            string cleanRole = roleName.Trim();

            // So sánh không phân biệt in hoa in thường (Bỏ qua dấu câu và kiểu chữ)
            if (cleanRole.Equals("IT Admin", StringComparison.OrdinalIgnoreCase)) return UserRole.ITAdmin;

            if (cleanRole.Equals("Hiệu trưởng", StringComparison.OrdinalIgnoreCase)) return UserRole.HieuTruong;

            if (cleanRole.Equals("Giáo vụ", StringComparison.OrdinalIgnoreCase)) return UserRole.GiaoVu;

            // Xử lý các trường hợp viết tắt cho GVCN
            if (cleanRole.Equals("GVCN", StringComparison.OrdinalIgnoreCase) ||
                cleanRole.Equals("Giáo viên chủ nhiệm", StringComparison.OrdinalIgnoreCase))
                return UserRole.GVCN;

            // Xử lý các trường hợp viết tắt cho GVBM
            if (cleanRole.Equals("GVBM", StringComparison.OrdinalIgnoreCase) ||
                cleanRole.Equals("Giáo viên bộ môn", StringComparison.OrdinalIgnoreCase))
                return UserRole.GVBM;

            if (cleanRole.Equals("Học sinh", StringComparison.OrdinalIgnoreCase)) return UserRole.HocSinh;

            // Mặc định an toàn
            return UserRole.HocSinh;
        }
    }
}