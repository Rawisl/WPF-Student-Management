using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.ViewModels
{
    // Class bọc thông tin học sinh, có thêm biến IsSelected cho CheckBox
    public partial class SelectableStudentItem : ObservableObject
    {
        [ObservableProperty] private bool _isSelected = false; // Thuộc tính ăn tiền ở đây
        [ObservableProperty] private string _studentID = string.Empty;
        [ObservableProperty] private string _fullName = string.Empty;
        [ObservableProperty] private string _gender = string.Empty;
        [ObservableProperty] private string _dateOfBirth = string.Empty;
    }

    public partial class ClassRosterViewModel : ObservableObject
    {
        // 1. Danh sách học sinh ĐÃ VÀO LỚP (Hiện ở bảng ngoài)
        [ObservableProperty]
        private ObservableCollection<SelectableStudentItem> _currentClassStudents;

        // 2. Danh sách học sinh CHƯA CÓ LỚP (Hiện trong Dialog)
        [ObservableProperty]
        private ObservableCollection<SelectableStudentItem> _availableStudents;

        [ObservableProperty]
        private string _selectedGrade;

        [ObservableProperty]
        private string _selectedClass;

        public string ClassSizeText => $"Sĩ số: {CurrentClassStudents?.Count ?? 0} / 40"; // Sửa lại logic lấy max size sau

        public ClassRosterViewModel()
        {
            AvailableStudents = new ObservableCollection<SelectableStudentItem>();
            CurrentClassStudents = new ObservableCollection<SelectableStudentItem>(); // Khởi tạo bảng chính
            LoadMockData();
        }

        private void LoadMockData()
        {
            // Bưng nguyên mâm dữ liệu giả từ file cũ qua đây
            CurrentClassStudents.Add(new SelectableStudentItem { FullName = "Phạm Thị D", StudentID = "HS004", Gender = "Nữ", DateOfBirth = "15/08/2008" });
            CurrentClassStudents.Add(new SelectableStudentItem { FullName = "Hoàng Văn E", StudentID = "HS005", Gender = "Nam", DateOfBirth = "22/11/2008" });
            CurrentClassStudents.Add(new SelectableStudentItem { FullName = "Vũ Thị F", StudentID = "HS006", Gender = "Nữ", DateOfBirth = "05/01/2009" });
            AvailableStudents.Add(new SelectableStudentItem { FullName = "Trần Văn G", StudentID = "HS007", Gender = "Nam", DateOfBirth = "12/03/2008" });
            AvailableStudents.Add(new SelectableStudentItem { FullName = "Lê Thị H", StudentID = "HS008", Gender = "Nữ", DateOfBirth = "29/12/2008" });

            // Cập nhật sĩ số lần đầu khi load form
            OnPropertyChanged(nameof(ClassSizeText));
        }

        [RelayCommand]
        private async Task OpenAddStudentDialog()
        {
            // RÀO CHẮN: Kiểm tra xem đã chọn lớp ở ComboBox chưa?
            //if (string.IsNullOrEmpty(_selectedClass))
            //{
            //    NotificationHelper.ShowWarning("Bro phải chọn Lớp trước khi thêm học sinh nhé!");
            //    return;
            //}

            // (Tương lai bro sẽ viết code ở đây để query DB: Lấy danh sách những đứa rảnh ném vào AvailableStudents)
            // LoadAvailableStudentsForClass(SelectedLop);

            var dialogContent = new WPF_Student_Management.Components.AddStudentToClassDialog
            {
                DataContext = this
            };

            await MaterialDesignThemes.Wpf.DialogHost.Show(dialogContent, "RootDialog");
        }

        [RelayCommand]
        private void SaveSelection()
        {
            var selectedStudents = AvailableStudents.Where(s => s.IsSelected).ToList();

            if (selectedStudents.Count == 0)
            {
                NotificationHelper.ShowWarning("Bro chưa chọn học sinh nào cả!");
                return;
            }

            // CHUYỂN DATA: Thêm vào lớp hiện tại và xóa khỏi danh sách chờ
            foreach (var hs in selectedStudents)
            {
                hs.IsSelected = false; // Reset lại trạng thái tick
                CurrentClassStudents.Add(hs); // Đẩy ra bảng ngoài
                AvailableStudents.Remove(hs); // Xóa khỏi dialog
            }

            NotificationHelper.ShowSuccess($"Đã xếp lớp thành công cho {selectedStudents.Count} học sinh!");

            OnPropertyChanged(nameof(ClassSizeText));

            MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
        }
    }
}
