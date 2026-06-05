using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;
using WPF_Student_Management.Services;

namespace WPF_Student_Management.ViewModels
{
    public partial class StudentProfileDetailViewModel : ObservableObject
    {
        private readonly Student _originalItem;

        [ObservableProperty]
        private bool _isReadOnly;

        public Visibility ActionVisibility => IsReadOnly ? Visibility.Collapsed : Visibility.Visible;
        public bool IsEditable => !IsReadOnly;

        [ObservableProperty] private string _studentID;

        [ObservableProperty]
        private bool _isAccountActive = true;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _fullName;

        [ObservableProperty] private bool _isMale;
        [ObservableProperty] private bool _isFemale;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private DateTime _dateOfBirth;

        [ObservableProperty] private bool _isFamilyNormal = true;
        [ObservableProperty] private bool _isFamilyHard;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _phoneNumber;

        [ObservableProperty] private string _emailPrefix;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _address;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _guardianName;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _guardianPhoneNumber;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _ageErrorMessage = string.Empty;

        private int _minAge = 15;
        private int _maxAge = 20;

        [ObservableProperty]
        private bool _isCreateRequestVisible;

        public StudentProfileDetailViewModel(Student student, bool isReadOnly = false)
        {
            _originalItem = student;
            IsReadOnly = isReadOnly;

            // Nếu form đang ở chế độ read thì ẩn nút Lập Đơn
            if (IsReadOnly)
            {
                IsCreateRequestVisible = false;
            }
            else
            {
                if (CurrentUser.Instance != null && CurrentUser.Instance.Role == (UserRole)5)
                {
                    IsCreateRequestVisible = true;
                }
                else
                {
                    IsCreateRequestVisible = false;
                }
            }

            StudentID = student.StudentId.ToString();
            FullName = student.FullName;
            IsAccountActive = Account.IsAccountActive(student.AccountId);

            if (student.Gender == "Nam")
                IsMale = true;
            else
                IsFemale = true;

            DateOfBirth = student.DateOfBirth ?? DateTime.Now.AddYears(-15);

            if (student.FamilyBackground == "Bình thường")
                IsFamilyNormal = true;
            else
                IsFamilyHard = true;

            Address = student.Address;
            PhoneNumber = student.PhoneNumber;

            if (!string.IsNullOrWhiteSpace(student.Email) && student.Email.EndsWith("@gmail.com"))
            {
                EmailPrefix = student.Email.Replace("@gmail.com", "");
            }
            else
            {
                EmailPrefix = student.Email;
            }

            GuardianName = student.GuardianName;
            GuardianPhoneNumber = student.GuardianPhoneNumber;

            LoadAgeRegulations();
            OnDateOfBirthChanged(DateOfBirth);
        }

        private void LoadAgeRegulations()
        {
            try
            {
                var allRegulations = Regulation.GetAllRegulations();
                if (allRegulations != null && allRegulations.Any())
                {
                    var minAgeParam = allRegulations.FirstOrDefault(r => r.RegulationName == "MinAge");
                    if (minAgeParam != null)
                        _minAge = (int)minAgeParam.Value;

                    var maxAgeParam = allRegulations.FirstOrDefault(r => r.RegulationName == "MaxAge");
                    if (maxAgeParam != null)
                        _maxAge = (int)maxAgeParam.Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Không tải được quy định tuổi: " + ex.Message);
            }
        }

        partial void OnDateOfBirthChanged(DateTime value)
        {
            int age = DateTime.Now.Year - value.Year;
            if (DateTime.Now.DayOfYear < value.DayOfYear)
                age--;

            if (age < _minAge || age > _maxAge)
                AgeErrorMessage = $"Tuổi {age} không hợp lệ (Quy định: {_minAge} - {_maxAge})";
            else
                AgeErrorMessage = string.Empty;
        }

        private bool CanSave()
        {
            if (IsReadOnly)
                return false;

            string phoneRegexPattern = @"^0\d{9}$";

            return string.IsNullOrEmpty(AgeErrorMessage) &&
                   !string.IsNullOrWhiteSpace(FullName) &&
                   !string.IsNullOrWhiteSpace(Address) &&
                   !string.IsNullOrWhiteSpace(PhoneNumber) &&
                   !string.IsNullOrWhiteSpace(GuardianName) &&
                   !string.IsNullOrWhiteSpace(GuardianPhoneNumber) &&
                   Regex.IsMatch(PhoneNumber?.Trim() ?? "", phoneRegexPattern) &&
                   Regex.IsMatch(GuardianPhoneNumber?.Trim() ?? "", phoneRegexPattern);
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void Save()
        {
            try
            {
                string finalGender = IsMale ? "Nam" : "Nữ";
                string finalFamilyBg = IsFamilyNormal ? "Bình thường" : "Khó khăn";
                string finalEmail = string.IsNullOrWhiteSpace(EmailPrefix) ? null : $"{EmailPrefix.Trim()}@gmail.com";

                _originalItem.FullName = FullName.Trim();
                _originalItem.Gender = finalGender;
                _originalItem.DateOfBirth = DateOfBirth;
                _originalItem.FamilyBackground = finalFamilyBg;
                _originalItem.Address = Address.Trim();
                _originalItem.PhoneNumber = PhoneNumber.Trim();
                _originalItem.Email = finalEmail;
                _originalItem.GuardianName = GuardianName.Trim();
                _originalItem.GuardianPhoneNumber = GuardianPhoneNumber.Trim();

                bool success = _originalItem.UpdateStudent();

                if (success)
                {
                    NotificationHelper.ShowSuccess("Cập nhật hồ sơ thành công!");
                    MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
                }
                else
                {
                    NotificationHelper.ShowError("Lỗi: Không thể lưu thông tin xuống CSDL!");
                }
            }
            catch (SqlException sqlEx)
            {
                if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
                {
                    NotificationHelper.ShowError("Lỗi: Email này đã được sử dụng cho một học sinh khác. Vui lòng kiểm tra lại!");
                }
                else
                {
                    NotificationHelper.ShowError("Lỗi cơ sở dữ liệu: " + sqlEx.Message);
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi hệ thống khi cập nhật: " + ex.Message);
            }
        }

        [RelayCommand]
        private void ResetPassword()
        {
            if (!IsAccountActive)
            {
                NotificationHelper.ShowError("Không thể cấp lại mật khẩu do tài khoản của học sinh đang bị khóa. Vui lòng liên hệ IT Admin.");
                return;
            }

            bool isConfirm = NotificationHelper.ShowConfirm($"Bạn có chắc chắn muốn đặt lại mật khẩu của học sinh {_originalItem.FullName} về mặc định không?");
            if (!isConfirm)
                return;

            string defaultRawPassword = "";
            if (_originalItem.DateOfBirth.HasValue && !string.IsNullOrWhiteSpace(_originalItem.PhoneNumber) && _originalItem.PhoneNumber.Length >= 4)
            {
                string dobStr = _originalItem.DateOfBirth.Value.ToString("ddMMyyyy");
                string phoneTail = _originalItem.PhoneNumber.Substring(_originalItem.PhoneNumber.Length - 4);
                defaultRawPassword = dobStr + phoneTail;
            }
            else
            {
                defaultRawPassword = "Password123";
            }

            bool isSuccess = Account.ResetPassword(_originalItem.AccountId, defaultRawPassword);

            if (isSuccess)
            {
                NotificationHelper.ShowSuccess($"Đã cấp lại mật khẩu mặc định thành công cho học sinh {_originalItem.FullName}!\n\nMật khẩu mới: {defaultRawPassword}");
            }
            else
            {
                NotificationHelper.ShowError("Hệ thống lỗi: Không thể reset mật khẩu lúc này!");
            }
        }

        [RelayCommand]
        private void Cancel() => MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");

        [RelayCommand]
        private async Task OpenRequestForm()
        {
            try
            {
                MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");

                var requestVM = new EnrollmentChangeRequestViewModel(_originalItem);
                var requestView = new WPF_Student_Management.Components.EnrollmentChangeRequestUC { DataContext = requestVM };

                await MaterialDesignThemes.Wpf.DialogHost.Show(requestView, "RootDialog");
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi khởi tạo form lập đơn:\n" + ex.Message);
            }
        }

        partial void OnIsMaleChanged(bool value) => IsFemale = !value;
        partial void OnIsFemaleChanged(bool value) => IsMale = !value;
        partial void OnIsFamilyNormalChanged(bool value) => IsFamilyHard = !value;
        partial void OnIsFamilyHardChanged(bool value) => IsFamilyNormal = !value;
    }
}