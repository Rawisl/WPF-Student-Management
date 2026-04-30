using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;
using System.Globalization;

namespace WPF_Student_Management.ViewModels
{
    public partial class ClassManagementViewModel : ObservableObject
    {
        // Sĩ số tối đa mặc định (đề phòng lỗi mất kết nối DB khi load quy định)
        private static int _maxClassSize = 40;

        // Lớp Data Transfer Object (DTO) phục vụ riêng cho hiển thị UI và Binding
        public partial class ClassItemUI : ObservableObject
        {
            [ObservableProperty]
            private int _classId;

            [ObservableProperty]
            [NotifyPropertyChangedFor(nameof(DisplayClassName))]
            private string _className = string.Empty;

            [ObservableProperty]
            private int _grade;

            [ObservableProperty]
            [NotifyPropertyChangedFor(nameof(StatusText))]
            private int _classSize;

            [ObservableProperty]
            private int? _homeroomTeacherId;

            [ObservableProperty]
            private string _homeroomTeacherName = string.Empty;

            public string StatusText => ClassSize >= _maxClassSize ? "Đầy" : (ClassSize == 0 ? "Trống" : "Còn chỗ");

            // Xử lý format tên lớp hiển thị (VD: Lớp số "10A1" viết dính, lớp chữ "10 Tin" viết cách)
            public string DisplayClassName
            {
                get
                {
                    if (string.IsNullOrEmpty(ClassName)) return string.Empty;

                    string gradeStr = Grade.ToString();
                    string suffix = ClassName.StartsWith(gradeStr)
                                    ? ClassName.Substring(gradeStr.Length)
                                    : ClassName;

                    bool isSpecialClass = suffix.All(c => char.IsLetter(c)) && suffix.Length > 1;

                    return isSpecialClass ? $"{gradeStr} {suffix}" : $"{gradeStr}{suffix}";
                }
            }
        }

        // Class vỏ bọc để chứa trạng thái CheckBox lúc xóa hàng loạt
        public partial class SelectableClassItem : ObservableObject
        {
            [ObservableProperty]
            private bool _isSelected = true; //Mặc định checked hết

            public ClassItemUI ClassInfo { get; set; }
        }

        // Biến lưu danh sách lớp rỗng hiển thị lên Dialog
        [ObservableProperty]
        private ObservableCollection<SelectableClassItem> _emptyClassesToTrash = new();

        [ObservableProperty]
        private ObservableCollection<ClassItemUI> _classList = new ObservableCollection<ClassItemUI>();

        [ObservableProperty]
        private ClassItemUI? _editingClass;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveClassCommand))]
        private string _newClassName = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveClassCommand))]
        private string _newClassId = string.Empty;

        [ObservableProperty]
        private string _newHomeroomTeacher = string.Empty;

        [ObservableProperty]
        private int _grade = 10;

        [ObservableProperty]
        private ObservableCollection<Staff> _availableTeachers = new ObservableCollection<Staff>();

        [ObservableProperty]
        private Staff? _selectedTeacher;

        public ClassManagementViewModel()
        {
            LoadClassSizeRegulations();
            LoadClassesFromDatabase();
        }

        private void LoadClassesFromDatabase()
        {
            try
            {
                DataTable data = Class.GetAllClassesWithTeacher();
                var tempCollection = new ObservableCollection<ClassItemUI>();

                foreach (DataRow row in data.Rows)
                {
                    tempCollection.Add(new ClassItemUI
                    {
                        ClassId = Convert.ToInt32(row["ClassID"]),
                        ClassName = row["ClassName"].ToString() ?? "",
                        Grade = Convert.ToInt32(row["Grade"]),
                        ClassSize = Convert.ToInt32(row["ClassSize"]),
                        HomeroomTeacherId = row["HomeroomTeacherID"] == DBNull.Value ? null : Convert.ToInt32(row["HomeroomTeacherID"]),
                        HomeroomTeacherName = row["TeacherName"] == DBNull.Value ? "Chưa phân công" : row["TeacherName"].ToString()
                    });
                }

                ClassList = tempCollection;
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Có lỗi khi tải danh sách lớp học: " + ex.Message);
            }
        }

        private void LoadClassSizeRegulations()
        {
            try
            {
                var allRegulations = Regulation.GetAllRegulations();
                if (allRegulations != null && allRegulations.Any())
                {
                    var maxClassSizeParam = allRegulations.FirstOrDefault(r => r.RegulationName == "MaxClassSize");
                    if (maxClassSizeParam != null)
                    {
                        _maxClassSize = (int)maxClassSizeParam.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Không tải được sĩ số tối đa: " + ex.Message);
            }
        }

        [RelayCommand]
        private async Task CreateClass()
        {
            var teachers = Staff.GetAvailableTeachers();

            // Chèn Dummy Item (StaffId = -1) để hỗ trợ thao tác bỏ trống GVCN
            teachers.Insert(0, new Staff { StaffId = -1, FullName = "-- Trống (Không chọn) --" });

            AvailableTeachers = new ObservableCollection<Staff>(teachers);
            SelectedTeacher = AvailableTeachers[0];

            var dialog = new Components.CreateClassDialog { DataContext = this };
            await MaterialDesignThemes.Wpf.DialogHost.Show(dialog, "RootDialog");
        }

        private bool CanSaveClass() => !string.IsNullOrWhiteSpace(NewClassName);

        [RelayCommand(CanExecute = nameof(CanSaveClass))]
        private void SaveClass()
        {
            string input = NewClassName.Trim();
            string finalSuffix = "";
            string selectedGradeStr = Grade.ToString();

            // Validate và bóc tách tiền tố khối
            if (input.Length >= 2 && char.IsDigit(input[0]) && char.IsDigit(input[1]))
            {
                string inputGrade = input.Substring(0, 2);
                if (inputGrade != selectedGradeStr)
                {
                    NotificationHelper.ShowError($"Lớp không hợp lệ! Bạn đang tạo lớp cho Khối {selectedGradeStr}.");
                    return;
                }
                finalSuffix = input.Substring(2);
            }
            else
            {
                finalSuffix = input;
            }

            // Chặn tên lớp rỗng hoặc chỉ chứa số
            if (string.IsNullOrWhiteSpace(finalSuffix) || finalSuffix.All(char.IsDigit))
            {
                NotificationHelper.ShowError("Tên định danh lớp không được để trống hoặc chỉ chứa số (VD hợp lệ: A1, Tin, Lý...)");
                return;
            }

            // Chuẩn hóa định dạng: Ký tự đầu viết hoa, phần còn lại viết thường (VD: a7 -> A7, lý -> Lý)
            string formattedSuffix = char.ToUpper(finalSuffix[0]) + finalSuffix.Substring(1).ToLower();
            string fullClassName = $"{selectedGradeStr}{formattedSuffix}";

            if (ClassList.Any(c => c.ClassName.Equals(fullClassName, StringComparison.OrdinalIgnoreCase)))
            {
                NotificationHelper.ShowError($"Lớp '{fullClassName}' đã tồn tại!");
                return;
            }

            // Xử lý id giáo viên nếu người dùng chọn tùy chọn bỏ trống (Dummy Item -1)
            int? finalTeacherId = (SelectedTeacher == null || SelectedTeacher.StaffId == -1) ? null : SelectedTeacher.StaffId;
            string finalTeacherName = (SelectedTeacher == null || SelectedTeacher.StaffId == -1) ? "Chưa phân công" : SelectedTeacher.FullName;

            try
            {
                //Tạo object không cần gán ClassId nữa (để DB tự lo)
                Class newClass = new Class
                {
                    ClassName = fullClassName,
                    Grade = Grade,
                    ClassSize = 0,
                    HomeroomTeacherId = finalTeacherId
                };

                //Lưu xuống CSDL
                if (newClass.AddClass())
                {
                    NotificationHelper.ShowSuccess("Lập danh sách lớp thành công!");
                    CancelAddClass(); // Đóng Pop-up và xóa form

                    //Kéo lại mẻ lưới từ DB để UI nhận được ClassId thật do DB tự sinh
                    LoadClassesFromDatabase();
                }
                else
                {
                    NotificationHelper.ShowError("Thêm mới thất bại! Lỗi Model/DB.");
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi DB: " + ex.Message);
            }
        }

        [RelayCommand]
        private void CancelAddClass()
        {
            NewClassId = string.Empty;
            NewClassName = string.Empty;
            NewHomeroomTeacher = string.Empty;
            SelectedTeacher = null;
            Grade = 10;
            MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
        }

        [RelayCommand]
        private async Task OpenEditClassDialog(ClassItemUI selectedClass)
        {
            if (selectedClass == null) return;
            _editingClass = selectedClass;

            var teachers = Staff.GetAvailableTeachers();
            teachers.Insert(0, new Staff { StaffId = -1, FullName = "-- Trống (Bỏ phân công) --" });

            // Bổ sung GVCN hiện tại vào danh sách để giữ được Select state trên ComboBox
            if (selectedClass.HomeroomTeacherId.HasValue)
            {
                teachers.Add(new Staff
                {
                    StaffId = selectedClass.HomeroomTeacherId.Value,
                    FullName = selectedClass.HomeroomTeacherName
                });
            }

            AvailableTeachers = new ObservableCollection<Staff>(teachers);
            Grade = selectedClass.Grade;

            // Tách tiền tố khối để hiển thị phần hậu tố lên TextBox
            string gradeStr = Grade.ToString();
            NewClassName = selectedClass.ClassName.StartsWith(gradeStr)
                           ? selectedClass.ClassName.Substring(gradeStr.Length)
                           : selectedClass.ClassName;

            SelectedTeacher = AvailableTeachers.FirstOrDefault(t => t.StaffId == selectedClass.HomeroomTeacherId);

            var dialog = new Components.EditClassDialog { DataContext = this };
            await MaterialDesignThemes.Wpf.DialogHost.Show(dialog, "RootDialog");
        }

        private bool CanSaveEditClass() => !string.IsNullOrWhiteSpace(NewClassName);

        [RelayCommand(CanExecute = nameof(CanSaveEditClass))]
        private void SaveEditClass()
        {
            string input = NewClassName.Trim();
            string finalSuffix = input.Length >= 2 && char.IsDigit(input[0]) && char.IsDigit(input[1])
                                 ? input.Substring(2) : input;

            if (string.IsNullOrWhiteSpace(finalSuffix) || finalSuffix.All(char.IsDigit))
            {
                NotificationHelper.ShowError("Tên định danh lớp không hợp lệ!");
                return;
            }

            string formattedSuffix = char.ToUpper(finalSuffix[0]) + finalSuffix.Substring(1).ToLower();
            string fullClassName = $"{Grade}{formattedSuffix}";

            // Kiểm tra trùng tên (Bỏ qua chính lớp đang được sửa)
            if (ClassList.Any(c => c.ClassId != _editingClass.ClassId && c.ClassName.Equals(fullClassName, StringComparison.OrdinalIgnoreCase)))
            {
                NotificationHelper.ShowError($"Lớp '{fullClassName}' đã tồn tại!");
                return;
            }

            int? finalTeacherId = (SelectedTeacher == null || SelectedTeacher.StaffId == -1) ? null : SelectedTeacher.StaffId;
            string finalTeacherName = (SelectedTeacher == null || SelectedTeacher.StaffId == -1) ? "Chưa phân công" : SelectedTeacher.FullName;

            try
            {
                Class updateClass = new Class
                {
                    ClassId = _editingClass.ClassId,
                    ClassName = fullClassName,
                    Grade = Grade,
                    ClassSize = _editingClass.ClassSize,
                    HomeroomTeacherId = finalTeacherId
                };

                if (updateClass.UpdateClass())
                {
                    _editingClass.ClassName = fullClassName;
                    _editingClass.HomeroomTeacherId = finalTeacherId;
                    _editingClass.HomeroomTeacherName = finalTeacherName;

                    NotificationHelper.ShowSuccess("Cập nhật thông tin lớp thành công!");
                    CancelEditClass();
                }
                else
                {
                    NotificationHelper.ShowError("Cập nhật thất bại! Lỗi Model/DB.");
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi DB: " + ex.Message);
            }
        }

        [RelayCommand]
        private void CancelEditClass()
        {
            _editingClass = null;
            NewClassName = string.Empty;
            SelectedTeacher = null;
            Grade = 10;
            MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
        }

        [RelayCommand]
        private void DeleteClass(ClassItemUI selectedClass)
        {
            if (selectedClass == null) return;

            // Quy định: Chỉ cho phép xóa lớp khi sĩ số trống
            if (selectedClass.ClassSize > 0)
            {
                NotificationHelper.ShowError($"Lớp '{selectedClass.ClassName}' đang có {selectedClass.ClassSize} học sinh. Không thể xóa!");
                return;
            }

            bool isConfirm = NotificationHelper.ShowConfirm(
                $"Bạn có chắc chắn muốn xóa lớp '{selectedClass.ClassName}' không?\n" +
                "Lớp học sẽ bị xóa hoàn toàn khỏi hệ thống và không thể hoàn tác!");

            if (isConfirm)
            {
                if (Class.DeleteClass(selectedClass.ClassId))
                {
                    ClassList.Remove(selectedClass);
                    NotificationHelper.ShowSuccess("Xóa lớp học thành công!");
                }
                else
                {
                    NotificationHelper.ShowError("Xóa thất bại! Lỗi kết nối CSDL.");
                }
            }
        }


        // Lệnh Quét lớp rỗng
        [RelayCommand]
        private async Task ScanEmptyClasses()
        {
            // 1. Tìm tất cả lớp có ClassSize == 0
            var emptyClasses = ClassList.Where(c => c.ClassSize == 0).ToList();

            // 2. Exception: Không có lớp nào
            if (!emptyClasses.Any())
            {
                NotificationHelper.ShowWarning("Không có lớp rỗng nào để xóa.");
                return;
            }

            // 3. Đổ dữ liệu vào list của Dialog (Bọc nó vào SelectableClassItem)
            EmptyClassesToTrash.Clear();
            foreach (var cls in emptyClasses)
            {
                EmptyClassesToTrash.Add(new SelectableClassItem { ClassInfo = cls });
            }

            // 4. Mở Custom Dialog
            var dialog = new Components.BatchDeleteEmptyClassesDialog { DataContext = this };
            await MaterialDesignThemes.Wpf.DialogHost.Show(dialog, "RootDialog");
        }

        // Lệnh Xác nhận Xóa
        [RelayCommand]
        private void ConfirmBatchDelete()
        {
            // Lọc ra những lớp mà user tick CheckBox
            var classesToDelete = EmptyClassesToTrash.Where(x => x.IsSelected).Select(x => x.ClassInfo).ToList();

            bool isConfirm = NotificationHelper.ShowConfirm( $"Bạn có chắc chắn muốn xóa '{classesToDelete.Count()}' lớp không?\n" + "Các lớp học đã chọn sẽ bị xóa hoàn toàn khỏi hệ thống và không thể hoàn tác!");

            if (!isConfirm) return;

            if (!classesToDelete.Any())
            {
                MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
                return;
            }

            int successCount = 0;

            // Chạy vòng lặp xóa từng lớp dưới DB
            foreach (var cls in classesToDelete)
            {
                if (Class.DeleteClass(cls.ClassId)) // Tận dụng lại hàm DeleteClass có sẵn của bro
                {
                    ClassList.Remove(cls); // Xóa khỏi UI
                    successCount++;
                }
            }

            MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");

            if (successCount > 0)
            {
                NotificationHelper.ShowSuccess($"Đã dọn dẹp thành công {successCount} lớp rỗng!");
            }
            else
            {
                NotificationHelper.ShowError("Có lỗi xảy ra khi xóa lớp dưới CSDL.");
            }
        }

        [RelayCommand]
        private void CancelBatchDelete()
        {
            MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
        }
    }
}