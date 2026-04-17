using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.ViewModels
{
    public partial class StudentProfileDetailViewModel : ObservableObject
    {
        private readonly StudentItem _originalItem; // Giữ tham chiếu để update lại UI bảng chính

        [ObservableProperty] private string _studentID;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _fullName;

        [ObservableProperty] private string _gender;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private DateTime _dateOfBirth;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _address;

        [ObservableProperty] private string _email;

        // Thuộc tính để hiện lỗi tuổi lên UI popup
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _ageErrorMessage = string.Empty;

        public StudentProfileDetailViewModel(StudentItem hs)
        {
            _originalItem = hs;

            // Map dữ liệu từ Item sang ViewModel của Popup
            StudentID = hs.StudentID;
            FullName = hs.FullName;
            Gender = hs.Gender;
            // Parse ngày từ string sang DateTime để dùng DatePicker cho xịn
            if (DateTime.TryParseExact(hs.DateOfBirth, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                DateOfBirth = date;
            }
            else
            {
                DateOfBirth = DateTime.Now.AddYears(-15); // Default nếu lỗi parse
            }
        }

        // Logic check tuổi bê từ HocSinhViewModel sang
        partial void OnDateOfBirthChanged(DateTime value)
        {
            int age = DateTime.Now.Year - value.Year;
            if (DateTime.Now.DayOfYear < value.DayOfYear) age--;

            // Giả sử quy định 15-20 tuổi
            if (age < 15 || age > 20)
                AgeErrorMessage = $"Tuổi {age} không hợp lệ (15-20)";
            else
                AgeErrorMessage = string.Empty;
        }

        // Kiểm tra điều kiện để cho bấm nút Lưu
        private bool CanSave()
        {
            return string.IsNullOrEmpty(AgeErrorMessage) &&
                               !string.IsNullOrWhiteSpace(FullName) &&
                               !string.IsNullOrWhiteSpace(Address);
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void Save()
        {
            // 2. Lưu vào DB (Giả lập hàm Sua() của Long)
            // var success = Services.HocSinh.Sua(new Services.HocSinh { ... });
            bool success = true; // Giả sử thành công

            if (success)
            {
                // 3. Update ngược lại cái dòng trên DataGrid ở màn hình chính
                _originalItem.FullName = FullName;
                _originalItem.Gender = Gender;
                _originalItem.DateOfBirth = DateOfBirth.ToString("dd/MM/yyyy");

                NotificationHelper.ShowSuccess("Cập nhật hồ sơ thành công!");
                MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
            }
        }

        [RelayCommand]
        private void Cancel() => MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
    }
}
