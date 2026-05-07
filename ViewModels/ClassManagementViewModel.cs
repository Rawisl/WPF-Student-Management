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
        // Sĩ số tối đa mặc định
        private static int _maxClassSize = 40;

        // --- BỔ SUNG: QUẢN LÝ NĂM HỌC HIỆN TẠI ---
        // (Trong thực tế, cái này có thể lấy từ cấu hình hệ thống chung, tạm thời ta gán mặc định)
        [ObservableProperty]
        private string _currentAcademicYear = "2025-2026";
        // -----------------------------------------

        // Lớp Data Transfer Object (DTO)
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

            // --- BỔ SUNG THUỘC TÍNH NĂM HỌC ĐỂ UI CÓ THỂ HIỂN THỊ ---
            [ObservableProperty]
            private string _academicYear = string.Empty;

            public string StatusText => ClassSize >= _maxClassSize ? "Đầy" : (ClassSize == 0 ? "Trống" : "Còn chỗ");

            // Xử lý format tên lớp hiển thị
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
            private bool _isSelected = true;

            public ClassItemUI ClassInfo { get; set; }
        }

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
                        HomeroomTeacherName = row["TeacherName"] == DBNull.Value ? "Chưa phân công" : row["TeacherName"].ToString(),
                        // Map thêm cột AcademicYear từ DB lên
                        AcademicYear = row["AcademicYear"] != DBNull.Value ? row["AcademicYear"].ToString()! : CurrentAcademicYear
                    });
                }

                // Tùy chọn: Nếu bro chỉ muốn hiển thị lớp của năm học HIỆN TẠI, thì dùng thêm .Where(c => c.AcademicYear == CurrentAcademicYear)
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
            // SỬA LỖI COMPILER: Truyền CurrentAcademicYear vào hàm tìm GVCN rảnh
            var teachers = Staff.GetAvailableTeachers(CurrentAcademicYear);

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

            if (string.IsNullOrWhiteSpace(finalSuffix) || finalSuffix.All(char.IsDigit))
            {
                NotificationHelper.ShowError("Tên định danh lớp không được để trống hoặc chỉ chứa số (VD hợp lệ: A1, Tin, Lý...)");
                return;
            }

            string formattedSuffix = char.ToUpper(finalSuffix[0]) + finalSuffix.Substring(1).ToLower();
            string fullClassName = $"{selectedGradeStr}{formattedSuffix}";

            // Chặn trùng tên trong CÙNG 1 NĂM HỌC
            if (ClassList.Any(c => c.AcademicYear == CurrentAcademicYear && c.ClassName.Equals(fullClassName, StringComparison.OrdinalIgnoreCase)))
            {
                NotificationHelper.ShowError($"Lớp '{fullClassName}' năm học {CurrentAcademicYear} đã tồn tại!");
                return;
            }

            int? finalTeacherId = (SelectedTeacher == null || SelectedTeacher.StaffId == -1) ? null : SelectedTeacher.StaffId;

            try
            {
                Class newClass = new Class
                {
                    ClassName = fullClassName,
                    Grade = Grade,
                    ClassSize = 0,
                    HomeroomTeacherId = finalTeacherId,
                    // THÊM: Gán năm học hiện tại lúc tạo lớp
                    AcademicYear = CurrentAcademicYear
                };

                if (newClass.AddClass())
                {
                    NotificationHelper.ShowSuccess("Lập danh sách lớp thành công!");
                    CancelAddClass();
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

            // Truyền năm học của lớp đang edit để lấy đúng danh sách GVCN
            var teachers = Staff.GetAvailableTeachers(selectedClass.AcademicYear);
            teachers.Insert(0, new Staff { StaffId = -1, FullName = "-- Trống (Bỏ phân công) --" });

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

            // Kiểm tra trùng tên trong cùng Năm học
            if (ClassList.Any(c => c.ClassId != _editingClass.ClassId && c.AcademicYear == _editingClass.AcademicYear && c.ClassName.Equals(fullClassName, StringComparison.OrdinalIgnoreCase)))
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
                    HomeroomTeacherId = finalTeacherId,
                    // Giữ nguyên năm học của lớp đang Edit
                    AcademicYear = _editingClass.AcademicYear
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

        [RelayCommand]
        private async Task ScanEmptyClasses()
        {
            var emptyClasses = ClassList.Where(c => c.ClassSize == 0).ToList();

            if (!emptyClasses.Any())
            {
                NotificationHelper.ShowWarning("Không có lớp rỗng nào để xóa.");
                return;
            }

            EmptyClassesToTrash.Clear();
            foreach (var cls in emptyClasses)
            {
                EmptyClassesToTrash.Add(new SelectableClassItem { ClassInfo = cls });
            }

            var dialog = new Components.BatchDeleteEmptyClassesDialog { DataContext = this };
            await MaterialDesignThemes.Wpf.DialogHost.Show(dialog, "RootDialog");
        }

        [RelayCommand]
        private void ConfirmBatchDelete()
        {
            var classesToDelete = EmptyClassesToTrash.Where(x => x.IsSelected).Select(x => x.ClassInfo).ToList();

            if (!classesToDelete.Any())
            {
                MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
                return;
            }

            bool isConfirm = NotificationHelper.ShowConfirm($"Bạn có chắc chắn muốn xóa '{classesToDelete.Count()}' lớp không?\n" + "Các lớp học đã chọn sẽ bị xóa hoàn toàn khỏi hệ thống và không thể hoàn tác!");

            if (!isConfirm) return;

            int successCount = 0;

            foreach (var cls in classesToDelete)
            {
                if (Class.DeleteClass(cls.ClassId))
                {
                    ClassList.Remove(cls);
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