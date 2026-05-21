using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.ViewModels
{
    // Kế thừa ObservableObject thay vì INotifyPropertyChanged dài dòng
    public partial class ForceChangePasswordViewModel : ObservableObject
    {
        // Khai báo biến và dặn C# tự báo hiệu cho nút Lưu (ChangePasswordCommand) mỗi khi gõ chữ
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ChangePasswordCommand))]
        private string _newPassword;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ChangePasswordCommand))]
        private string _confirmPassword;

        // Constructor giờ trống trơn, sạch sẽ!
        public ForceChangePasswordViewModel()
        {
        }

        // Hàm check điều kiện: Đủ 2 ô mới cho bấm nút
        private bool CanChangePassword()
        {
            return !string.IsNullOrWhiteSpace(NewPassword) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword);
        }

        // Tự động sinh RelayCommand<Window> vì tham số truyền vào là Window
        [RelayCommand(CanExecute = nameof(CanChangePassword))]
        private void ChangePassword(Window currentWindow)
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

                    // Chuyển form cực mượt mà không cần ép kiểu "obj is Window" nữa
                    if (currentWindow != null)
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
    }
}