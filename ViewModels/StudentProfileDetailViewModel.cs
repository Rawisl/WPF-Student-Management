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
            StudentID = "HS" + student.StudentId.ToString();
            FullName = student.FullName;

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
            // Chốt data từ UI
            string finalGender = IsMale ? "Nam" : "Nữ";
            string finalFamilyBg = IsFamilyNormal ? "Bình thường" : "Khó khăn";
            string finalEmail = string.IsNullOrWhiteSpace(EmailPrefix) ? "" : $"{EmailPrefix.Trim()}@gmail.com";

            // (Giả sử gọi update DB thành công)
            bool success = true;

            if (success)
            {
                // Update ngược lại UI bảng chính
                _originalItem.FullName = FullName;
                _originalItem.Gender = finalGender;
                _originalItem.DateOfBirth = DateOfBirth;
                _originalItem.FamilyBackground = finalFamilyBg;
                _originalItem.Address = Address;
                _originalItem.PhoneNumber = PhoneNumber;
                _originalItem.Email = finalEmail;
                _originalItem.GuardianName = GuardianName;
                _originalItem.GuardianPhoneNumber = GuardianPhoneNumber;

                NotificationHelper.ShowSuccess("Cập nhật hồ sơ thành công!");
                MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
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