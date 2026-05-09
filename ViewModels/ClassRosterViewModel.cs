using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;

namespace WPF_Student_Management.ViewModels
{
    public partial class SelectableStudentItem : ObservableObject
    {
        [ObservableProperty] private bool _isSelected = false;
        [ObservableProperty] private string _studentId = string.Empty;
        [ObservableProperty] private string _fullName = string.Empty;
        [ObservableProperty] private string _gender = string.Empty;
        [ObservableProperty] private string _dateOfBirth = string.Empty;
        [ObservableProperty] private string _phoneNumber = string.Empty;
    }

    public partial class ClassRosterViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _currentAcademicYear = "2025-2026";

        [ObservableProperty]
        private ObservableCollection<SelectableStudentItem> _currentClassStudents;

        [ObservableProperty]
        private ObservableCollection<SelectableStudentItem> _availableStudents;

        [ObservableProperty]
        private string _selectedGrade;

        [ObservableProperty]
        private string _selectedClass;

        private List<Class> _allClassesFromDb = new List<Class>();

        [ObservableProperty]
        private ObservableCollection<string> _availableGrades;

        [ObservableProperty]
        private ObservableCollection<Class> _availableClasses;

        private int _maxClassSize = 40;

        // CỜ KIỂM SOÁT KHÓA SỔ
        private bool _isClassLocked = false;

        public string ClassSizeText
        {
            get
            {
                if (string.IsNullOrEmpty(SelectedClass)) return "";

                // Báo cho Giáo vụ biết lớp đã bị khóa sổ
                if (_isClassLocked)
                    return $"Sĩ số: {CurrentClassStudents?.Count ?? 0} / {_maxClassSize}\nLớp đã được GVCN lập báo cáo học kỳ!";

                return $"Sĩ số: {CurrentClassStudents?.Count ?? 0} / {_maxClassSize}";
            }
        }

        public string ClassSizeColor
        {
            get
            {
                if (string.IsNullOrEmpty(SelectedClass)) return "#2C3E50";

                if (_isClassLocked) return "#FF4757"; // Hiện MÀU ĐỎ nếu lớp đã bị khóa sổ

                int current = CurrentClassStudents?.Count ?? 0;
                double ratio = (double)current / _maxClassSize;

                if (ratio >= 1.0) return "#FF4757"; // Lớp đã Full
                if (ratio >= 0.8) return "#F39C12"; // Sắp Full
                return "#00B894";                   // Còn trống nhiều
            }
        }

        public ClassRosterViewModel()
        {
            AvailableStudents = new ObservableCollection<SelectableStudentItem>();
            CurrentClassStudents = new ObservableCollection<SelectableStudentItem>();
            AvailableGrades = new ObservableCollection<string>();
            AvailableClasses = new ObservableCollection<Class>();
        }

        private void LoadRegulations()
        {
            try
            {
                var allRegulations = Regulation.GetAllRegulations();
                if (allRegulations != null && allRegulations.Any())
                {
                    var maxSizeParam = allRegulations.FirstOrDefault(r => r.RegulationName == "MaxClassSize");
                    if (maxSizeParam != null) _maxClassSize = (int)maxSizeParam.Value;
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi tải quy định sĩ số: " + ex.Message);
            }
        }

        private void LoadStudentsForSelectedClass()
        {
            CurrentClassStudents.Clear();
            _isClassLocked = false; // Reset cờ khóa sổ mỗi khi đổi lớp

            if (string.IsNullOrEmpty(SelectedClass)) return;

            if (int.TryParse(SelectedClass, out int classId))
            {
                try
                {
                    // 1. KIỂM TRA LỚP ĐÃ CÓ BÁO CÁO KHÓA SỔ CHƯA
                    string lockQuery = "SELECT COUNT(*) FROM ClassReport WHERE ClassID = @ClassID AND AcademicYear = @AcademicYear AND IsLocked = 1";
                    DataTable dtLock = DatabaseHelper.ExecuteQuery(lockQuery, new[] {
                        new SqlParameter("@ClassID", classId),
                        new SqlParameter("@AcademicYear", CurrentAcademicYear)
                    });

                    if (dtLock.Rows.Count > 0 && Convert.ToInt32(dtLock.Rows[0][0]) > 0)
                    {
                        _isClassLocked = true; // Bật cờ khóa nếu có báo cáo đã chốt
                    }

                    // 2. KÉO HỌC SINH CỦA LỚP
                    var dbStudents = Student.SearchStudents(classId: classId);
                    foreach (var hs in dbStudents)
                    {
                        CurrentClassStudents.Add(new SelectableStudentItem
                        {
                            StudentId = hs.StudentId,
                            FullName = hs.FullName,
                            Gender = hs.Gender ?? "Không rõ",
                            DateOfBirth = hs.DateOfBirth?.ToString("dd/MM/yyyy") ?? "Không rõ",
                            PhoneNumber = hs.PhoneNumber ?? ""
                        });
                    }

                    OnPropertyChanged(nameof(ClassSizeText));
                    OnPropertyChanged(nameof(ClassSizeColor));
                    OpenAddStudentDialogCommand.NotifyCanExecuteChanged();
                }
                catch (System.Exception ex)
                {
                    NotificationHelper.ShowError("Lỗi tải danh sách lớp: " + ex.Message);
                }
            }
        }

        // ĐIỀU KIỆN MỞ KHÓA NÚT "+ THÊM HỌC SINH"
        private bool CanOpenAddStudent()
        {
            return !string.IsNullOrEmpty(SelectedGrade) &&              // Đã chọn Khối
                   !string.IsNullOrEmpty(SelectedClass) &&              // Đã chọn Lớp
                   !_isClassLocked &&                                   // Lớp CHƯA bị khóa sổ
                   CurrentClassStudents != null &&
                   CurrentClassStudents.Count < _maxClassSize;          // Lớp chưa Full sĩ số
        }

        [RelayCommand(CanExecute = nameof(CanOpenAddStudent))]
        private async Task OpenAddStudentDialog()
        {
            AvailableStudents.Clear();

            try
            {
                var bovoStudents = Student.GetUnassignedStudents(CurrentAcademicYear);
                foreach (var hs in bovoStudents)
                {
                    AvailableStudents.Add(new SelectableStudentItem
                    {
                        StudentId = hs.StudentId,
                        FullName = hs.FullName,
                        Gender = hs.Gender ?? "Không rõ",
                        DateOfBirth = hs.DateOfBirth?.ToString("dd/MM/yyyy") ?? "Không rõ",
                        PhoneNumber = hs.PhoneNumber ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi kéo dữ liệu học sinh vãng lai: " + ex.Message);
                return;
            }

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
            if (selectedStudents.Count == 0) return;

            if (string.IsNullOrEmpty(SelectedClass) || !int.TryParse(SelectedClass, out int classId))
            {
                NotificationHelper.ShowError("Lỗi: Không xác định được Lớp để xếp vào!");
                return;
            }

            int projectedSize = CurrentClassStudents.Count + selectedStudents.Count;
            if (projectedSize > _maxClassSize)
            {
                NotificationHelper.ShowError($"Vượt quá sĩ số quy định!\nHiện tại lớp đã có {CurrentClassStudents.Count}/{_maxClassSize} HS.\nChỉ được thêm tối đa {_maxClassSize - CurrentClassStudents.Count} HS nữa.");
                return;
            }

            int successCount = 0;
            foreach (var hs in selectedStudents)
            {
                bool isSavedToDb = Student.AssignStudentToClass(hs.StudentId, classId);
                if (isSavedToDb)
                {
                    hs.IsSelected = false;
                    CurrentClassStudents.Add(hs);
                    AvailableStudents.Remove(hs);
                    successCount++;
                }
            }

            if (successCount > 0)
            {
                NotificationHelper.ShowSuccess($"Đã xếp lớp thành công cho {successCount} học sinh!");
                OnPropertyChanged(nameof(ClassSizeText));
                OnPropertyChanged(nameof(ClassSizeColor));
                OpenAddStudentDialogCommand.NotifyCanExecuteChanged();
                MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
            }
            else
            {
                NotificationHelper.ShowError("Lỗi hệ thống: Không thể lưu dữ liệu xuống Database!");
            }
        }

        partial void OnSelectedClassChanged(string value)
        {
            OnPropertyChanged(nameof(ClassSizeText));
            OnPropertyChanged(nameof(ClassSizeColor));
            OpenAddStudentDialogCommand.NotifyCanExecuteChanged();

            if (string.IsNullOrEmpty(value))
            {
                CurrentClassStudents?.Clear();
                return;
            }
            LoadStudentsForSelectedClass();
        }

        partial void OnSelectedGradeChanged(string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            AvailableClasses.Clear();

            if (int.TryParse(value, out int selectedGradeInt))
            {
                var filteredClasses = _allClassesFromDb.Where(c => c.Grade == selectedGradeInt).ToList();
                foreach (var cls in filteredClasses) AvailableClasses.Add(cls);
            }

            SelectedClass = null;
            OpenAddStudentDialogCommand.NotifyCanExecuteChanged(); // Ép check lại nút khi bị reset
        }

        public void RefreshData()
        {
            SelectedGrade = null;
            SelectedClass = null;
            AvailableClasses.Clear();
            AvailableGrades.Clear();

            LoadRegulations();

            _allClassesFromDb = Class.GetAllClasses().Where(c => c.AcademicYear == CurrentAcademicYear).ToList();

            var distinctGrades = _allClassesFromDb.Select(c => c.Grade.ToString()).Distinct().OrderBy(g => g).ToList();
            foreach (var grade in distinctGrades) AvailableGrades.Add(grade);
        }
    }
}