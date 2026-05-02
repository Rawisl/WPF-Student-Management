using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Linq;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;

namespace WPF_Student_Management.ViewModels
{
    public partial class StudentProfileDetailViewModel : ObservableObject
    {
        private readonly Student _originalItem; // Giữ tham chiếu để update lại UI bảng chính

        [ObservableProperty] private string _studentID;

        [ObservableProperty]
        private bool _isAccountActive = true;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _fullName;

        // --- CÁC BIẾN MỚI THÊM ĐỂ KHỚP VỚI GIAO DIỆN XAML ---
        [ObservableProperty] private bool _isMale;
        [ObservableProperty] private bool _isFemale;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private DateTime _dateOfBirth;

        [ObservableProperty] private bool _isFamilyNormal = true;
        [ObservableProperty] private bool _isFamilyHard;

        [ObservableProperty] private string _phoneNumber;
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
        // ----------------------------------------------------

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _ageErrorMessage = string.Empty;

        private int _minAge = 15;
        private int _maxAge = 20;

        public StudentProfileDetailViewModel(Student student)
        {
            _originalItem = student;

            // 1. MAP DỮ LIỆU CƠ BẢN
            StudentID = student.StudentId.ToString();
            FullName = student.FullName;
            IsAccountActive = Account.IsAccountActive(student.AccountId);

            // Xử lý Giới tính cho RadioButton
            if (student.Gender == "Nam") IsMale = true;
            else IsFemale = true;

            DateOfBirth = student.DateOfBirth ?? DateTime.Now.AddYears(-15);

            // 2. MAP HOÀN CẢNH GIA ĐÌNH
            if (student.FamilyBackground == "Bình thường") IsFamilyNormal = true;
            else IsFamilyHard = true;

            // 3. MAP THÔNG TIN LIÊN LẠC
            Address = student.Address;
            PhoneNumber = student.PhoneNumber;

            // Xử lý Email: Cắt đuôi @gmail.com để ném lên UI
            if (!string.IsNullOrWhiteSpace(student.Email) && student.Email.EndsWith("@gmail.com"))
            {
                EmailPrefix = student.Email.Replace("@gmail.com", "");
            }
            else
            {
                EmailPrefix = student.Email;
            }

            // 4. MAP NGƯỜI BẢO HỘ
            GuardianName = student.GuardianName;
            GuardianPhoneNumber = student.GuardianPhoneNumber;

            // Kiểm tra tuổi
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
                    if (minAgeParam != null) _minAge = (int)minAgeParam.Value;

                    var maxAgeParam = allRegulations.FirstOrDefault(r => r.RegulationName == "MaxAge");
                    if (maxAgeParam != null) _maxAge = (int)maxAgeParam.Value;
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
            if (DateTime.Now.DayOfYear < value.DayOfYear) age--;

            if (age < _minAge || age > _maxAge)
                AgeErrorMessage = $"Tuổi {age} không hợp lệ (Quy định: {_minAge} - {_maxAge})";
            else
                AgeErrorMessage = string.Empty;
        }

        // Kiểm tra điều kiện để cho bấm nút Lưu
        private bool CanSave()
        {
            return string.IsNullOrEmpty(AgeErrorMessage) &&
                   !string.IsNullOrWhiteSpace(FullName) &&
                   !string.IsNullOrWhiteSpace(Address) &&
                   !string.IsNullOrWhiteSpace(GuardianName) &&
                   !string.IsNullOrWhiteSpace(GuardianPhoneNumber);
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void Save()
        {
            string phoneRegexPattern = @"^0\d{9}$";

            if (!System.Text.RegularExpressions.Regex.IsMatch(PhoneNumber?.Trim() ?? "", phoneRegexPattern))
            {
                NotificationHelper.ShowWarning("Số điện thoại Học sinh chưa hợp lệ!\nVui lòng nhập ĐỦ 10 chữ số và bắt đầu bằng số 0.");
                return;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(GuardianPhoneNumber?.Trim() ?? "", phoneRegexPattern))
            {
                NotificationHelper.ShowWarning("Số điện thoại Người bảo hộ chưa hợp lệ!\nVui lòng nhập ĐỦ 10 chữ số và bắt đầu bằng số 0.");
                return;
            }

            // Chốt data từ UI
            string finalGender = IsMale ? "Nam" : "Nữ";
            string finalFamilyBg = IsFamilyNormal ? "Bình thường" : "Khó khăn";
            string finalEmail = string.IsNullOrWhiteSpace(EmailPrefix) ? "" : $"{EmailPrefix.Trim()}@gmail.com";

            //Cập nhật dữ liệu mới vào object _originalItem
            _originalItem.FullName = FullName;
            _originalItem.Gender = finalGender;
            _originalItem.DateOfBirth = DateOfBirth;
            _originalItem.FamilyBackground = finalFamilyBg;
            _originalItem.Address = Address;
            _originalItem.PhoneNumber = PhoneNumber;
            _originalItem.Email = finalEmail;
            _originalItem.GuardianName = GuardianName;
            _originalItem.GuardianPhoneNumber = GuardianPhoneNumber;

            bool success = _originalItem.UpdateStudent();

            if (success)
            {
                NotificationHelper.ShowSuccess("Cập nhật hồ sơ thành công!");
                MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
            }
            else
            {
                // Nhỡ DB rớt mạng hay gì thì nó văng lỗi
                NotificationHelper.ShowError("Lỗi: Không thể lưu thông tin xuống CSDL!");
            }
        }

        [RelayCommand]
        private void ResetPassword()
        {
            //Rào chắn: Nếu tài khoản bị khóa thì không cho làm gì (UI đã chặn rồi, nhưng chặn thêm lớp code cho chắc)
            if (!IsAccountActive)
            {
                NotificationHelper.ShowError("Không thể cấp lại mật khẩu do tài khoản của học sinh đang bị khóa. Vui lòng liên hệ IT Admin.");
                return;
            }

            //Hiện Pop-up xác nhận
            bool isConfirm = NotificationHelper.ShowConfirm($"Bạn có chắc chắn muốn đặt lại mật khẩu của học sinh {_originalItem.FullName} về mặc định không?");
            if (!isConfirm) return;

            //Tái tạo lại mật khẩu mặc định (ddMMyyyy + 4 số cuối SĐT)
            string defaultRawPassword = "";
            if (_originalItem.DateOfBirth.HasValue && !string.IsNullOrWhiteSpace(_originalItem.PhoneNumber) && _originalItem.PhoneNumber.Length >= 4)
            {
                string dobStr = _originalItem.DateOfBirth.Value.ToString("ddMMyyyy");
                string phoneTail = _originalItem.PhoneNumber.Substring(_originalItem.PhoneNumber.Length - 4);
                defaultRawPassword = dobStr + phoneTail;
            }
            else
            {
                //Fallback nếu data học sinh bị thiếu (VD: Không có SĐT)
                defaultRawPassword = "Password123";
            }

            //Múc xuống CSDL
            bool isSuccess = Account.ResetPassword(_originalItem.AccountId, defaultRawPassword);

            //Báo cáo kết quả
            if (isSuccess)
            {
                //TODO(Note): Chỗ này backend phải có hàm Revoke Token để đá học sinh ra, nhưng tạm thời UI chỉ show thông báo
                NotificationHelper.ShowSuccess($"Đã cấp lại mật khẩu mặc định thành công cho học sinh {_originalItem.FullName}!\n\nMật khẩu mới: {defaultRawPassword}");
            }
            else
            {
                NotificationHelper.ShowError("Hệ thống lỗi: Không thể reset mật khẩu lúc này!");
            }
        }
        [RelayCommand]
        private void Cancel() => MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");

        // Logic đồng bộ RadioButton (chống check cả 2 cái cùng lúc)
        partial void OnIsMaleChanged(bool value) => IsFemale = !value;
        partial void OnIsFemaleChanged(bool value) => IsMale = !value;
        partial void OnIsFamilyNormalChanged(bool value) => IsFamilyHard = !value;
        partial void OnIsFamilyHardChanged(bool value) => IsFamilyNormal = !value;
    }
}