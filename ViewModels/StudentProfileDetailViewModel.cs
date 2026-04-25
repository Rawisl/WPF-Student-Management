using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        //TẠO 2 BIẾN LƯU TRỮ QUY ĐỊNH LÚC VỪA MỞ FORM(Cho số mặc định lỡ DB lỗi)
        private int _minAge = 15;
        private int _maxAge = 20;

        public StudentProfileDetailViewModel(Student student)
        {
           _originalItem = student;

            //MAP DỮ LIỆU TỪ MODEL VÀO VIEWMODEL
            // Model là int StudentId, mình format thêm "HS" cho nó đẹp trên Popup
            StudentID = "HS" + student.StudentId.ToString(); 
            FullName = student.FullName;
            Gender = student.Gender;

            //KHÔNG CẦN PARSE STRING NỮA, VÌ DATEOFBIRTH CỦA MODEL LÀ DATETIME? RỒI
            DateOfBirth = student.DateOfBirth ?? DateTime.Now.AddYears(-15);

            // Nếu Model có mấy trường này thì map luôn:
            // Address = student.Address;
            // Email = student.Email;

            //KÉO QUY ĐỊNH TỪ DB LÊN NGAY LÚC KHỞI TẠO
            LoadAgeRegulations();

            // Ép nó check lại tuổi ngay khi vừa load lên (lỡ tuổi lúc trước hợp lệ, giờ đổi quy định thành không hợp lệ)
            OnDateOfBirthChanged(DateOfBirth);
        }
        private void LoadAgeRegulations()
        {
            try
            {
                //Kéo toàn bộ list tham số từ DB lên
                var allRegulations = Regulation.GetAllRegulations();

                if (allRegulations != null && allRegulations.Any())
                {
                    //Tìm dòng có tên là "MinAge"
                    var minAgeParam = allRegulations.FirstOrDefault(r => r.RegulationName == "MinAge");
                    if (minAgeParam != null)
                    {
                        // Ép kiểu từ Decimal (trong Model của Long) sang int
                        _minAge = (int)minAgeParam.Value;
                    }

                    //Tìm dòng có tên là "MaxAge"
                    var maxAgeParam = allRegulations.FirstOrDefault(r => r.RegulationName == "MaxAge");
                    if (maxAgeParam != null)
                    {
                        _maxAge = (int)maxAgeParam.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                // Lỗi DB thì nuốt lỗi, UI vẫn xài số 15-20 mặc định, app không sập
                Console.WriteLine("Không tải được quy định tuổi: " + ex.Message);
            }
        }

        // Logic check tuổi bê từ HocSinhViewModel sang
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
                               !string.IsNullOrWhiteSpace(Address);
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void Save()
        {
            // 4. Lưu vào DB 
            bool success = true; // Giả sử thành công

            if (success)
            {
                // 5. Update ngược lại cái dòng trên DataGrid ở màn hình chính
                _originalItem.FullName = FullName;
                _originalItem.Gender = Gender;

                // Gán thẳng DateTime vào luôn, KHÔNG ToString() nữa vì Model nó cần DateTime
                _originalItem.DateOfBirth = DateOfBirth;
                // _originalItem.Address = Address;

                NotificationHelper.ShowSuccess("Cập nhật hồ sơ thành công!");
                MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
            }
        }

        [RelayCommand]
        private void Cancel() => MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
    }
}
