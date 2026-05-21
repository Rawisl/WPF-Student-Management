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
    // Kế thừa ObservableObject để tự động có INotifyPropertyChanged
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

        // Hàm kiểm tra điều kiện đăng nhập
        private bool CanLogin() => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

        [RelayCommand(CanExecute = nameof(CanLogin))]
        private void Login(Window loginWindow)
        {
            try
            {
                // Băm mật khẩu để so khớp
                string hashedPassword = PasswordHasher.HashPassword(Password);

                string query = "SELECT * FROM Account WHERE Username = @Username AND PasswordHash = @PasswordHash";
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
                    int roleId = Convert.ToInt32(row["RoleID"]);
                    bool isRequiredChangePwd = Convert.ToBoolean(row["IsRequiredChangePassword"]);

                    // Khởi tạo CurrentUser
                    CurrentUser.Instance.Login(accountId, Username, (UserRole)roleId);

                    if (isRequiredChangePwd)
                    {
                        // Mở cửa sổ bắt buộc đổi mật khẩu
                        new ForceChangePasswordWindow().Show();
                    }
                    else
                    {
                        // Vào thẳng MainWindow
                        new MainWindow().Show();
                    }

                    // Đóng cửa sổ Login hiện tại
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
    }
}