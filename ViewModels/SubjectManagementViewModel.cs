using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;

namespace WPF_Student_Management.ViewModels
{
    public partial class SubjectManagementViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Subject> _subjectsList = new ObservableCollection<Subject>();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveSubjectCommand))]
        private string _newSubjectName = string.Empty;

        // Dùng bool cho RadioButton (Mặc định chọn Điểm số)
        [ObservableProperty]
        private bool _isScoreGradeType = true;

        [ObservableProperty]
        private bool _isPassFailGradeType = false;

        // Logic đồng bộ 2 nút Radio
        partial void OnIsScoreGradeTypeChanged(bool value) => IsPassFailGradeType = !value;
        partial void OnIsPassFailGradeTypeChanged(bool value) => IsScoreGradeType = !value;


        // 1. THÊM CONSTRUCTOR NÀY VÀO ĐỂ LOAD DATA NGAY KHI MỞ TRANG
        public SubjectManagementViewModel()
        {
            LoadSubjectsData();
        }

        private void LoadSubjectsData()
        {
            try
            {
                // Gọi hàm GetAllSubjects của Long
                var dataFromDb = Subject.GetAllSubjects();

                // Đổ dữ liệu vào ObservableCollection
                SubjectsList = new ObservableCollection<Subject>(dataFromDb);
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Có lỗi khi tải danh sách môn học: " + ex.Message);
            }
        }

        [RelayCommand]
        private async Task AddSubject()
        {
            var dialog = new Components.AddSubjectDialog { DataContext = this };
            await MaterialDesignThemes.Wpf.DialogHost.Show(dialog, "RootDialog");
        }

        // Điều kiện cho phép bấm nút Lưu (Tên môn không được để trống)
        private bool CanSaveSubject() => !string.IsNullOrWhiteSpace(NewSubjectName);

        [RelayCommand(CanExecute = nameof(CanSaveSubject))]
        private void SaveSubject()
        {
            // Chuẩn hóa tên (Cắt khoảng trắng thừa)
            string cleanName = NewSubjectName.Trim();

            // KIỂM TRA NGOẠI LỆ (EXCEPTION): Môn đã tồn tại chưa? (So sánh không phân biệt hoa thường)
            if (SubjectsList.Any(s => s.SubjectName.Equals(cleanName, StringComparison.OrdinalIgnoreCase)))
            {
                NotificationHelper.ShowWarning($"Môn học '{cleanName}' đã tồn tại trong hệ thống!\nVui lòng chọn tên khác.");
                return;
            }

            // Chuyển đổi loại điểm chuẩn bị ném xuống DB
            string dbGradeType = IsScoreGradeType ? "Score" : "PassFail";

            // Khởi tạo Model (Không cần truyền SubjectId nữa, DB tự lo)
            var newSubject = new Subject
            {
                SubjectName = cleanName,
                GradeType = dbGradeType,
                IsDeleted = false
            };

            // Lưu xuống DB bằng hàm của Model
            if (newSubject.AddSubject())
            {
                NotificationHelper.ShowSuccess("Thêm môn học mới thành công!");

                // Load lại bảng danh sách để lấy được SubjectId xịn do CSDL vừa tự sinh ra
                LoadSubjectsData();

                // Đóng form và dọn dẹp
                CancelAddSubject();
            }
            else
            {
                NotificationHelper.ShowError("Lỗi CSDL: Không thể thêm môn học!");
            }
        }

        // Hàm Đóng Form và làm sạch dữ liệu
        [RelayCommand]
        private void CancelAddSubject()
        {
            NewSubjectName = string.Empty;
            IsScoreGradeType = true; // Trả về mặc định
            MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
        }

        [RelayCommand]
        private void DeleteSubject(Subject selectedSubject)
        {
            if (selectedSubject == null) return;

            // 1. Hiện thông báo xác nhận (Đổi văn phong cho hợp với Soft Delete)
            bool isConfirm = NotificationHelper.ShowConfirm(
                $"Bạn có chắc chắn muốn ngừng hoạt động môn '{selectedSubject.SubjectName}' không?\n" +
                "Môn học này sẽ được ẩn khỏi hệ thống phân công và nhập điểm.");

            if (isConfirm)
            {
                // 2. Gọi hàm STATIC của Model truyền ID vào
                if (Subject.DeleteSubject(selectedSubject.SubjectId))
                {
                    // 3. Xóa ngay trên UI để người dùng thấy mất luôn không cần load lại
                    SubjectsList.Remove(selectedSubject);
                    NotificationHelper.ShowSuccess("Đã xóa môn học thành công!");
                }
                else
                {
                    NotificationHelper.ShowError("Xóa thất bại! Lỗi kết nối CSDL.");
                }
            }
        }
    }
}