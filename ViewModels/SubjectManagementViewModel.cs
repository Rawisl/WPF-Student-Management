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
    public partial class SelectableDeletedSubject : ObservableObject
    {
        public DeletedSubjectDto Data { get; }

        [ObservableProperty]
        private bool _isSelected = false;

        // Bọc các thuộc tính của DTO ra ngoài để XAML dễ dàng Binding
        public int SubjectId => Data.SubjectId;
        public string SubjectName => Data.SubjectName;
        public string GradeType => Data.GradeType;
        public int ScoreCount => Data.ScoreCount;
        public int TeachingCount => Data.TeachingCount;
        public int ReportCount => Data.ReportCount;

        public SelectableDeletedSubject(DeletedSubjectDto data)
        {
            Data = data;
        }
    }

    public partial class SubjectManagementViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Subject> _subjectsList = new ObservableCollection<Subject>();
        [ObservableProperty]
        private ObservableCollection<SelectableDeletedSubject> _deletedSubjects = new ObservableCollection<SelectableDeletedSubject>();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveSubjectCommand))]
        private string _newSubjectName = string.Empty;

        [ObservableProperty]
        private bool _isScoreGradeType = true;

        [ObservableProperty]
        private bool _isPassFailGradeType = false;

        partial void OnIsScoreGradeTypeChanged(bool value) => IsPassFailGradeType = !value;
        partial void OnIsPassFailGradeTypeChanged(bool value) => IsScoreGradeType = !value;

        public SubjectManagementViewModel()
        {
            LoadSubjectsData();
        }

        private void LoadSubjectsData()
        {
            try
            {
                var dataFromDb = Subject.GetAllSubjects();
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

            // Môn đã tồn tại chưa? (So sánh không phân biệt hoa thường)
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
                LoadSubjectsData();
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
            if (selectedSubject == null)
                return;

            //Hiện thông báo xác nhận
            bool isConfirm = NotificationHelper.ShowConfirm(
                $"Bạn có chắc chắn muốn ngừng hoạt động môn '{selectedSubject.SubjectName}' không?\n" +
                "Môn học này sẽ được ẩn khỏi hệ thống phân công và nhập điểm.");

            if (isConfirm)
            {
                //Gọi hàm STATIC của Model truyền ID vào
                if (Subject.DeleteSubject(selectedSubject.SubjectId))
                {
                    //Xóa trên UI để người dùng thấy mất luôn không cần load lại
                    SubjectsList.Remove(selectedSubject);
                    NotificationHelper.ShowSuccess("Đã xóa môn học thành công!");
                }
                else
                {
                    NotificationHelper.ShowError("Xóa thất bại! Lỗi kết nối CSDL.");
                }
            }
        }
        [RelayCommand]
        private async Task OpenRestoreDialog()
        {
            LoadDeletedSubjectsData();

            if (DeletedSubjects.Count == 0)
            {
                NotificationHelper.ShowWarning("Thùng rác trống!\nKhông có môn học nào cần khôi phục.");
                return;
            }

            var dialog = new Components.SubjectRestoreDialog { DataContext = this };
            await MaterialDesignThemes.Wpf.DialogHost.Show(dialog, "RootDialog");
        }
        private void LoadDeletedSubjectsData()
        {
            try
            {
                DeletedSubjects.Clear();
                var dataFromDb = Subject.GetDeletedSubjects();

                foreach (var dto in dataFromDb)
                {
                    DeletedSubjects.Add(new SelectableDeletedSubject(dto));
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi tải danh sách môn đã xóa: " + ex.Message);
            }
        }

        [RelayCommand]
        private void CancelRestore()
        {
            MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
        }

        [RelayCommand]
        private void ConfirmRestore()
        {
            // Lọc ra những môn được User tích CheckBox
            var selectedItems = DeletedSubjects.Where(x => x.IsSelected).ToList();

            if (selectedItems.Count == 0)
            {
                NotificationHelper.ShowWarning("Vui lòng chọn ít nhất 1 môn học để khôi phục!");
                return;
            }

            int successCount = 0;
            foreach (var item in selectedItems)
            {
                // Gọi hàm Restore dưới Model
                if (Subject.RestoreSubject(item.SubjectId))
                {
                    successCount++;
                }
            }

            if (successCount > 0)
            {
                NotificationHelper.ShowSuccess($"Đã khôi phục thành công {successCount} môn học!");
                LoadSubjectsData();
                MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
            }
            else
            {
                NotificationHelper.ShowError("Lỗi hệ thống: Không thể khôi phục môn học!");
            }
        }
    }
}