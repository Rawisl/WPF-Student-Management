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
    public class ForceChangePasswordViewModel : INotifyPropertyChanged
    {
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

        public ForceChangePasswordViewModel()
        {
            ChangePasswordCommand = new RelayCommand(ExecuteChangePassword, CanExecuteChangePassword);
        }

        private bool CanExecuteChangePassword(object obj)
        {
            return !string.IsNullOrWhiteSpace(NewPassword) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword);
        }

        private void ExecuteChangePassword(object obj)
        {
            if (NewPassword != ConfirmPassword)
            {
                NotificationHelper.ShowWarning("Mật khẩu xác nhận không khớp!");
                return;
            }

            string regexPattern = @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{6,}$";
            if (!Regex.IsMatch(NewPassword, regexPattern))
            {
                NotificationHelper.ShowWarning("Mật khẩu mới phải dài ít nhất 6 ký tự và bao gồm cả chữ và số.");
                return;
            }

            try
            {
                int currentUserId = CurrentUser.Instance.UserId;
                string hashedNewPwd = PasswordHasher.HashPassword(NewPassword);

                // Cập nhật mật khẩu mới và TẮT cờ bắt buộc đổi
                string updateQuery = "UPDATE Account SET PasswordHash = @NewHash, IsRequiredChangePassword = 0 WHERE AccountID = @AccountID";
                SqlParameter[] updateParams = new SqlParameter[]
                {
                    new SqlParameter("@NewHash", hashedNewPwd),
                    new SqlParameter("@AccountID", currentUserId)
                };

                int rowsAffected = DatabaseHelper.ExecuteNonQuery(updateQuery, updateParams);

                if (rowsAffected > 0)
                {
                    NotificationHelper.ShowSuccess("Đổi mật khẩu thành công! Chào mừng bạn đến với hệ thống.");

                    // Mở MainWindow và đóng cửa sổ hiện tại (Window được truyền vào qua CommandParameter)
                    if (obj is Window currentWindow)
                    {
                        MainWindow main = new MainWindow();
                        main.Show();
                        currentWindow.Close();
                    }
                }
                else
                {
                    NotificationHelper.ShowError("Có lỗi xảy ra khi cập nhật dữ liệu.");
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi hệ thống: " + ex.Message);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}