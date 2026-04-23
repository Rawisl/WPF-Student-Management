using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private string _oldPassword;
        public string OldPassword
        {
            get => _oldPassword;
            set { _oldPassword = value; OnPropertyChanged(); }
        }

        private string _newPassword;
        public string NewPassword
        {
            get => _newPassword;
            set { _newPassword = value; OnPropertyChanged(); }
        }

        private string _confirmPassword;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set { _confirmPassword = value; OnPropertyChanged(); }
        }

        public ICommand ChangePasswordCommand { get; }

        public SettingsViewModel()
        {
            ChangePasswordCommand = new RelayCommand(ExecuteChangePassword, CanExecuteChangePassword);
        }

        private bool CanExecuteChangePassword(object obj)
        {
            return !string.IsNullOrWhiteSpace(OldPassword) &&
                   !string.IsNullOrWhiteSpace(NewPassword) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword);
        }

        private void ExecuteChangePassword(object obj)
        {
            // Kiểm tra xác nhận mật khẩu
            if (NewPassword != ConfirmPassword)
            {
                MessageBox.Show("Mật khẩu xác nhận không khớp!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Báo lỗi nếu MK mới giống MK cũ
            if (NewPassword == OldPassword)
            {
                MessageBox.Show("Mật khẩu mới bắt buộc phải khác mật khẩu cũ!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Kiểm tra độ mạnh mật khẩu bằng Regex
            string regexPattern = @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{6,}$"; 
            if (!Regex.IsMatch(NewPassword, regexPattern))
            {
                MessageBox.Show("Mật khẩu mới phải dài ít nhất 6 ký tự và bao gồm cả chữ và số.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int currentUserId = CurrentUser.Instance.UserId;
                string hashedOldPwd = PasswordHasher.HashPassword(OldPassword);

                // Kiểm tra mật khẩu cũ trong DB xem có khớp không
                string checkQuery = "SELECT AccountID FROM Account WHERE AccountID = @AccountID AND PasswordHash = @PasswordHash";
                SqlParameter[] checkParams = new SqlParameter[]
                {
                    new SqlParameter("@AccountID", currentUserId),
                    new SqlParameter("@PasswordHash", hashedOldPwd)
                };

                var checkData = DatabaseHelper.ExecuteQuery(checkQuery, checkParams);

                if (checkData.Rows.Count == 0)
                {
                    MessageBox.Show("Mật khẩu hiện tại không chính xác!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Cập nhật mật khẩu mới và gỡ cờ bắt buộc đổi
                string hashedNewPwd = PasswordHasher.HashPassword(NewPassword);
                string updateQuery = "UPDATE Account SET PasswordHash = @NewHash, IsRequiredChangePassword = 0 WHERE AccountID = @AccountID";
                SqlParameter[] updateParams = new SqlParameter[]
                {
                    new SqlParameter("@NewHash", hashedNewPwd),
                    new SqlParameter("@AccountID", currentUserId)
                };

                int rowsAffected = DatabaseHelper.ExecuteNonQuery(updateQuery, updateParams);

                if (rowsAffected > 0)
                {
                    MessageBox.Show("Đổi mật khẩu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Kiểm tra xem cửa sổ hiện tại có phải là cửa sổ "Đổi mật khẩu bắt buộc" không
                    if (obj is Window currentWindow && currentWindow.GetType().Name == "ForceChangePasswordWindow")
                    {
                        // Mở Trang chính
                        MainWindow main = new MainWindow();
                        main.Show();

                        // Đóng cửa sổ ép đổi mật khẩu
                        currentWindow.Close();
                    }
                    else
                    {
                        // Nếu chỉ là đổi mật khẩu bình thường trong mục Cài đặt của MainWindow
                        // Thì chỉ cần xóa trắng form là được
                        OldPassword = "";
                        NewPassword = "";
                        ConfirmPassword = "";
                    }
                }
                else
                {
                    MessageBox.Show("Có lỗi xảy ra khi cập nhật dữ liệu.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}