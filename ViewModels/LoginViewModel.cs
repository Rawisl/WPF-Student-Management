using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Services;
using WPF_Student_Management.Views;

namespace WPF_Student_Management.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private string _username;
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        private string _password;
        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
        }

        private bool CanExecuteLogin(object obj)
        {
            return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

        private void ExecuteLogin(object obj)
        {
            try
            {
                // Băm mật khẩu người dùng nhập vào
                string hashedPassword = PasswordHasher.HashPassword(Password);

                string query = "SELECT * FROM Account WHERE Username = @Username AND PasswordHash = @PasswordHash";
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@Username", Username),
                    new SqlParameter("@PasswordHash", hashedPassword)
                };

                var data = DatabaseHelper.ExecuteQuery(query, parameters);

                if (data.Rows.Count > 0)
                {
                    var row = data.Rows[0];
                    bool isActive = Convert.ToBoolean(row["IsActive"]);

                    if (!isActive)
                    {
                        MessageBox.Show("Tài khoản của bạn đã bị khóa!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    int accountId = Convert.ToInt32(row["AccountID"]);
                    int roleId = Convert.ToInt32(row["RoleID"]);
                    bool isRequiredChangePwd = Convert.ToBoolean(row["IsRequiredChangePassword"]);

                    // Map RoleID từ DB sang UserRole Enum (Giả sử 1:Học Sinh, 2:IT Admin, 3:Hiệu Trưởng, 4:GVBM, 5:GVCN, 6:Giáo vụ)
                    UserRole userRole = (UserRole)roleId;

                    // Khởi tạo CurrentUser
                    CurrentUser.Instance.Login(accountId, Username, userRole);

                    if (isRequiredChangePwd)
                    {
                        // Mở cửa sổ bắt buộc đổi mật khẩu
                        ForceChangePasswordWindow forceWindow = new ForceChangePasswordWindow();
                        forceWindow.Show();

                        // Đóng LoginWindow
                        if (obj is Window loginWindow) loginWindow.Close();
                    }
                    else
                    {
                        // Vào thẳng MainWindow như bình thường
                        MainWindow main = new MainWindow();
                        main.Show();
                        if (obj is Window loginWindow) loginWindow.Close();
                    }
                }
                else
                {
                    MessageBox.Show("Thông tin đăng nhập không chính xác!", "Lỗi đăng nhập", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối cơ sở dữ liệu: " + ex.Message, "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}