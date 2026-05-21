using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using WPF_Student_Management.Components;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;

namespace WPF_Student_Management.ViewModels
{
    public class SubjectItem
    {
        public int? SubjectId { get; set; }
        public string SubjectName { get; set; }
    }
    public partial class EmployeeProfileDetailViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private Staff _currentStaff;

        // Cờ cảnh sát: True = Mở từ trang Cá nhân (chỉ xem), False = Mở từ trang Quản lý (được sửa)
        [ObservableProperty]
        private bool _isReadOnly;

        public Visibility ActionVisibility => IsReadOnly ? Visibility.Collapsed : Visibility.Visible;
        public bool IsEditable => !IsReadOnly;

        public ObservableCollection<string> GenderList { get; } = new ObservableCollection<string> { "Nam", "Nữ" };
        public ObservableCollection<string> StatusList { get; } = new ObservableCollection<string> { "Active", "Inactive" };

        [ObservableProperty]
        private ObservableCollection<Role> _roleList = new ObservableCollection<Role>();

        [ObservableProperty]
        private ObservableCollection<SubjectItem> _subjectList = new ObservableCollection<SubjectItem>();

        private readonly Action _onSaveSuccess;
        private readonly bool _isNewStaff;

        // Constructor nhận vào Staff, Cờ phân quyền, và một Callback để báo cho View cha biết khi lưu xong
        public EmployeeProfileDetailViewModel(Staff staff, bool isReadOnly = false, Action onSaveSuccess = null)
        {
            if (staff.Specialization == null)
            {
                staff.Specialization = 0;
            }

            CurrentStaff = staff;
            IsReadOnly = isReadOnly;
            _onSaveSuccess = onSaveSuccess;
            _isNewStaff = (staff.StaffId == 0);

            LoadRoles();
            LoadSubjects();
        }

        private void LoadRoles()
        {
            // Lấy tất cả trừ "Học sinh" (Giả sử RoleName trong DB là "Học sinh")
            var roles = Role.GetAllRoles()
                            .Where(r => !r.RoleName.Equals("Học sinh", StringComparison.OrdinalIgnoreCase))
                            .ToList();

            RoleList = new ObservableCollection<Role>(roles);

            // BỔ SUNG FIX BUG: Nếu tạo mới nhân viên (RoleId = 0), tự động gán cho họ Role đầu tiên trong list (VD: Giáo viên)
            if (_isNewStaff && CurrentStaff.RoleId == 0 && RoleList.Any())
            {
                CurrentStaff.RoleId = RoleList.First().RoleId;
            }
        }

        private void LoadSubjects()
        {
            SubjectList.Clear();
            // ĐÃ SỬA: Đổi null thành 0
            SubjectList.Add(new SubjectItem { SubjectId = 0, SubjectName = "-- Trống --" });

            try
            {
                string query = "SELECT SubjectID, SubjectName FROM Subject WHERE IsDeleted = 0";
                var dt = DatabaseHelper.ExecuteQuery(query);
                foreach (DataRow row in dt.Rows)
                {
                    SubjectList.Add(new SubjectItem
                    {
                        SubjectId = Convert.ToInt32(row["SubjectID"]),
                        SubjectName = row["SubjectName"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi tải danh sách môn học: " + ex.Message);
            }
        }

        private bool CanExecuteSave()
        {
            if (IsReadOnly || CurrentStaff == null) return false;

            if (string.IsNullOrWhiteSpace(CurrentStaff.FullName) || CurrentStaff.FullName.Trim().Split(' ').Length < 2) return false;

            string phonePattern = @"^0\d{9}$";
            if (string.IsNullOrWhiteSpace(CurrentStaff.PhoneNumber) || !Regex.IsMatch(CurrentStaff.PhoneNumber, phonePattern)) return false;

            if (string.IsNullOrWhiteSpace(CurrentStaff.NationalId) || CurrentStaff.NationalId.Length < 9) return false;

            string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (string.IsNullOrWhiteSpace(CurrentStaff.Email) || !Regex.IsMatch(CurrentStaff.Email, emailPattern)) return false;

            return true;
        }

        [RelayCommand(CanExecute = nameof(CanExecuteSave))]
        private void Save()
        {
            try
            {
                if (CurrentStaff.Specialization == 0)
                {
                    CurrentStaff.Specialization = null;
                }
                bool isSuccess = false;

                if (_isNewStaff)
                {
                    var accountInfo = CurrentStaff.ReceiveNewStaff();
                    if (accountInfo != null)
                    {
                        NotificationHelper.ShowSuccess(
                            $"Tiếp nhận giáo viên thành công!\n\n" +
                            $"Tài khoản: {accountInfo.Value.Username}\n" +
                            $"Mật khẩu: {accountInfo.Value.Password}\n\n" +
                            $"Lưu ý: Mật khẩu mặc định là tên + 4 số cuối SĐT.");
                        isSuccess = true;
                    }
                }
                else
                {
                    if (CurrentStaff.AccountId == CurrentUser.Instance.UserId && CurrentStaff.Status == "Inactive")
                    {
                        NotificationHelper.ShowError("Hành động bị từ chối!\nBạn không thể chuyển trạng thái sang Inactive cho tài khoản đang đăng nhập.");
                        return;
                    }

                    isSuccess = CurrentStaff.UpdateStaff();
                    if (isSuccess) NotificationHelper.ShowSuccess("Cập nhật thông tin thành công!");
                }

                if (isSuccess)
                {
                    _onSaveSuccess?.Invoke(); // Báo hiệu cho ViewModel cha Load lại DataGrid
                    DialogHost.Close("RootDialog");
                }
                else if (_isNewStaff)
                {
                    NotificationHelper.ShowError("Tiếp nhận thất bại. Có thể do trùng CCCD hoặc Số điện thoại!");
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi hệ thống: " + ex.Message);
            }
        }

        [RelayCommand]
        private void Cancel() => DialogHost.Close("RootDialog");
    }
}