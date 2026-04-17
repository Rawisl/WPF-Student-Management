using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Effects;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.ViewModels
{
    //hiện tại đang dùng tạm cái student item này
    public partial class StudentItem : ObservableObject
    {
        [ObservableProperty] private int _ordinalNumber;
        [ObservableProperty] private string _studentID = string.Empty;
        [ObservableProperty] private string _fullName = string.Empty;
        [ObservableProperty] private string _gender = string.Empty;
        [ObservableProperty] private string _dateOfBirth = string.Empty;
    }

    // Lớp trung gian để hiển thị danh sách lớp lên giao diện
    public class ClassItem
    {
        public string MaLop { get; set; } = string.Empty;
        public string TenLop { get; set; } = string.Empty;
    }

    public partial class GlobalStudentManagementViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<StudentItem> _allStudent;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _fullName = string.Empty;

        [ObservableProperty] 
        private bool _isMale = true;
        [ObservableProperty] 
        private bool _isFemale;

        // Logic đồng bộ giới tính (Optional nhưng nên có cho chắc)
        partial void OnIsMaleChanged(bool value) => IsFemale = !value;
        partial void OnIsFemaleChanged(bool value) => IsMale = !value;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _diaChi = string.Empty;

        [ObservableProperty] 
        private string _email = string.Empty;

        // Thông báo lỗi nếu sai tuổi quy định
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _ageErrorMessage = string.Empty;

        // Danh sách lớp nạp từ Database
        [ObservableProperty]
        private ObservableCollection<ClassItem> _danhSachLop = new ObservableCollection<ClassItem>();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private ClassItem? _selected;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private DateTime _dateOfBirth = DateTime.Now;
        public GlobalStudentManagementViewModel()
        {
            AllStudent = new ObservableCollection<StudentItem>();
            LoadMockData();
        }

        private void LoadMockData()
        {
            // Bơm dữ liệu giả vào để xem DataGrid lên hình có đẹp không
            AllStudent.Add(new StudentItem { OrdinalNumber = 1, StudentID = "HS001", FullName = "Nguyễn Văn A", Gender = "Nam", DateOfBirth = "15/05/2008" });
            AllStudent.Add(new StudentItem { OrdinalNumber = 2, StudentID = "HS002", FullName = "Trần Thị B", Gender = "Nữ", DateOfBirth = "22/08/2008" });
            AllStudent.Add(new StudentItem { OrdinalNumber = 3, StudentID = "HS003", FullName = "Lê Hoàng C", Gender = "Nam", DateOfBirth = "01/12/2008" });
        }

        private void LoadDataFromDatabase()
        {
            //AllStudent.Clear();
            //try
            //{
            //    var listHS = Services.HocSinh.LayDanhSach();
            //    int OrdinalNumber = 1;
            //    foreach (var hs in listHS)
            //    {
            //        AllStudent.Add(new StudentItem
            //        {
            //            OrdinalNumber = OrdinalNumber++,
            //            StudentID = hs.StudentID,
            //            FullName = hs.FullName,
            //            Gender = hs.Gender,
            //            DateOfBirth = hs.DateOfBirth.ToString("dd/MM/yyyy")
            //        });
            //    }
            //}
            //catch (System.Exception ex)
            //{
            //    NotificationHelper.ShowError($"Lỗi kết nối CSDL:\n{ex.Message}");
            //}
        }

        [RelayCommand]
        private async Task AddStudent()
        {
            var dialogContent = new WPF_Student_Management.Components.AddStudentDialog
            {
                DataContext = this // Quan trọng: Dùng chung bộ não này
            };
            await MaterialDesignThemes.Wpf.DialogHost.Show(dialogContent, "RootDialog");
        }

        [RelayCommand]
        private async Task EditStudent(StudentItem hs)
        {
            if (hs == null) return;

            // Khởi tạo "Bộ não" cho popup
            var detailVM = new StudentProfileDetailViewModel(hs);

            // Khởi tạo "Cái xác" popup
            var view = new Components.StudentProfileDetailUC
            {
                DataContext = detailVM
            };

            // Bật lên!
            await MaterialDesignThemes.Wpf.DialogHost.Show(view, "RootDialog");
        }

        [RelayCommand]
        private void DeleteStudent(StudentItem hs)
        {
            if (hs != null)
            {
                bool isChonOK = NotificationHelper.ShowConfirm($"Bạn có chắc chắn muốn xóa học sinh '{hs.FullName}' khỏi hệ thống không?\nHành động này không thể hoàn tác!");

                if (isChonOK)
                {
                    // Xóa thẳng trên UI để test cảm giác bấm nút
                    AllStudent.Remove(hs);
                    NotificationHelper.ShowSuccess("Xóa học sinh thành công (Mock)!");
                }
            }
        }

        // Logic kiểm tra tuổi mỗi khi thay đổi ngày sinh
        partial void OnDateOfBirthChanged(DateTime value)
        {
            int age = DateTime.Now.Year - value.Year;
            if (DateTime.Now.DayOfYear < value.DayOfYear) age--;

            // Giả sử quy định là 15-20 tuổi
            if (age < 15 || age > 20)
                AgeErrorMessage = $"Tuổi {age} không hợp lệ (Quy định 15-20)";
            else
                AgeErrorMessage = string.Empty;
        }

        // Kiểm tra điều kiện để kích hoạt nút Lưu
        private bool CanSave()
        {
            // Bắt buộc nhập Họ Tên và Địa Chỉ, và không có lỗi tuổi
            return string.IsNullOrEmpty(AgeErrorMessage) &&
                   !string.IsNullOrWhiteSpace(FullName) &&
                   !string.IsNullOrWhiteSpace(DiaChi);
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void Save()
        {
            // Thêm vào list Mock
            AllStudent.Add(new StudentItem
            {
                OrdinalNumber = AllStudent.Count + 1,
                StudentID = "HS" + DateTime.Now.ToString("ssmm"),
                FullName = this.FullName,
                Gender = IsMale ? "Nam" : "Nữ", // Lúc này IsMale đã được đồng bộ chuẩn
                DateOfBirth = DateOfBirth.ToString("dd/MM/yyyy")
            });

            NotificationHelper.ShowSuccess("Tiếp nhận thành công!");

            // Reset form - Gọi Cancel() để nó đóng Dialog luôn
            Cancel();
        }

        [RelayCommand]
        private void Cancel()
        {
            // Reset form sạch sẽ
            FullName = DiaChi = Email = string.Empty;
            DateOfBirth = DateTime.Now;
            AgeErrorMessage = string.Empty;

            // Đóng Dialog
            MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
        }


    }
}
