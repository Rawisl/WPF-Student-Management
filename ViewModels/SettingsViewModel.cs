using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using System;
using System.Text.RegularExpressions;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        // Gắn cờ để khi gõ chữ vào TextBox, nút Lưu tự động đánh giá xem có sáng lên không
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ChangePasswordCommand))]
        private string _oldPassword;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ChangePasswordCommand))]
        private string _newPassword;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ChangePasswordCommand))]
        private string _confirmPassword;

        public SettingsViewModel()
        {
            // Constructor giờ trống trơn, sạch sẽ
        }

        // Đổi tên hàm check và bỏ tham số (object obj)
        private bool CanChangePassword()
        {
            return !string.IsNullOrWhiteSpace(OldPassword) &&
                   !string.IsNullOrWhiteSpace(NewPassword) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword);
        }

        // Tự động sinh ra lệnh ChangePasswordCommand
        [RelayCommand(CanExecute = nameof(CanChangePassword))]
        private void ChangePassword()
        {
            if (NewPassword != ConfirmPassword)
            {
                NotificationHelper.ShowWarning("Mật khẩu xác nhận không khớp!");
                return;
            }

            if (NewPassword == OldPassword)
            {
                NotificationHelper.ShowError("Mật khẩu mới bắt buộc phải khác mật khẩu hiện tại!");
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
                string hashedOldPwd = PasswordHasher.HashPassword(OldPassword);

                string checkQuery = "SELECT AccountID FROM Account WHERE AccountID = @AccountID AND PasswordHash = @PasswordHash";
                SqlParameter[] checkParams = new SqlParameter[]
                {
                    new SqlParameter("@AccountID", currentUserId),
                    new SqlParameter("@PasswordHash", hashedOldPwd)
                };

                var checkData = DatabaseHelper.ExecuteQuery(checkQuery, checkParams);

                if (checkData.Rows.Count == 0)
                {
                    NotificationHelper.ShowError("Mật khẩu hiện tại không chính xác!");
                    return;
                }

                string hashedNewPwd = PasswordHasher.HashPassword(NewPassword);
                string updateQuery = "UPDATE Account SET PasswordHash = @NewHash WHERE AccountID = @AccountID";
                SqlParameter[] updateParams = new SqlParameter[]
                {
                    new SqlParameter("@NewHash", hashedNewPwd),
                    new SqlParameter("@AccountID", currentUserId)
                };

                int rowsAffected = DatabaseHelper.ExecuteNonQuery(updateQuery, updateParams);

                if (rowsAffected > 0)
                {
                    NotificationHelper.ShowSuccess("Đổi mật khẩu thành công!");

                    // Reset lại form cho gọn
                    OldPassword = "";
                    NewPassword = "";
                    ConfirmPassword = "";
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