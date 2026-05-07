using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;

namespace WPF_Student_Management.ViewModels
{
    // Class trung gian để binding lên từng dòng của DataGrid
    public class AssignmentDisplayItem : INotifyPropertyChanged
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; }

        // Mỗi dòng môn học sẽ tự giữ một danh sách giáo viên riêng
        public ObservableCollection<Staff> AvailableTeachers { get; set; }

        private int? _selectedTeacherId;
        public int? SelectedTeacherId
        {
            get => _selectedTeacherId;
            set { _selectedTeacherId = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class TeachingAssignmentViewModel : INotifyPropertyChanged
    {
        // --- ĐÃ SỬA: Đổi tên biến khớp với Binding UI và gắn lệnh làm mới ---
        private string _selectedSemester = "Học kỳ 1";
        public string SelectedSemester
        {
            get => _selectedSemester;
            set
            {
                _selectedSemester = value;
                OnPropertyChanged();
                LoadAssignmentsForClass(); // Đổi học kỳ -> load lại phân công
            }
        }

        private string _selectedAcademicYear = "2025-2026";
        public string SelectedAcademicYear
        {
            get => _selectedAcademicYear;
            set
            {
                _selectedAcademicYear = value;
                OnPropertyChanged();
                LoadClassesForYear(); // Đổi năm học -> load lại danh sách lớp mới
            }
        }
        // ----------------------------------------

        // ComboBox chọn Lớp
        private ObservableCollection<Class> _classList;
        public ObservableCollection<Class> ClassList
        {
            get => _classList;
            set { _classList = value; OnPropertyChanged(); }
        }

        private Class _selectedClass;
        public Class SelectedClass
        {
            get => _selectedClass;
            set
            {
                _selectedClass = value;
                OnPropertyChanged();
                LoadAssignmentsForClass();
            }
        }

        // Bảng phân công hiển thị trên UI
        private ObservableCollection<AssignmentDisplayItem> _assignmentList;
        public ObservableCollection<AssignmentDisplayItem> AssignmentList
        {
            get => _assignmentList;
            set { _assignmentList = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }

        public TeachingAssignmentViewModel()
        {
            SaveCommand = new RelayCommand(p => ExecuteSave(), p => SelectedClass != null);
            LoadClassesForYear();
        }

        // SỬA: Tách hàm load lớp riêng để gọi lại khi đổi năm học
        private void LoadClassesForYear()
        {
            var classes = Class.GetAllClasses().Where(c => c.AcademicYear == SelectedAcademicYear).ToList();
            ClassList = new ObservableCollection<Class>(classes);

            // Xóa lựa chọn cũ và dọn dẹp bảng phân công
            SelectedClass = null;
            AssignmentList = null;
        }

        private void LoadAssignmentsForClass()
        {
            if (SelectedClass == null)
            {
                AssignmentList = null;
                return;
            }

            var allSubjects = Subject.GetAllSubjects();
            var allStaff = Staff.GetAllStaff();

            // ĐÃ CHUẨN: Lọc theo Học kỳ và Năm học được chọn trên UI
            var currentAssignments = TeachingAssignment.GetAllAssignments()
                                    .Where(a => a.ClassId == SelectedClass.ClassId
                                             && a.Semester == SelectedSemester
                                             && a.AcademicYear == SelectedAcademicYear).ToList();

            var list = new ObservableCollection<AssignmentDisplayItem>();

            foreach (var subject in allSubjects)
            {
                // LỌC GV THEO CHUYÊN MÔN: Sử dụng hàm thông dịch từ đồng nghĩa
                var matchedTeachers = allStaff.Where(t => IsTeacherMatchSubject(t.Specialization, subject.SubjectName)).ToList();

                matchedTeachers.Insert(0, new Staff { StaffId = 0, FullName = "Trống" });

                var item = new AssignmentDisplayItem
                {
                    SubjectId = subject.SubjectId,
                    SubjectName = subject.SubjectName,
                    AvailableTeachers = new ObservableCollection<Staff>(matchedTeachers)
                };

                // Kiểm tra xem môn này đã có ai dạy ở lớp này chưa, nếu có thì gán vào để hiện lên ComboBox
                var existingAssign = currentAssignments.FirstOrDefault(a => a.SubjectId == subject.SubjectId);
                if (existingAssign != null)
                {
                    item.SelectedTeacherId = existingAssign.StaffId;
                }
                else
                {
                    item.SelectedTeacherId = 0;
                }

                list.Add(item);
            }

            AssignmentList = list;
        }

        private void ExecuteSave()
        {
            try
            {
                if (SelectedClass == null) return;

                // Xóa toàn bộ phân công cũ của lớp này TRONG HỌC KỲ VÀ NĂM HỌC HIỆN TẠI
                string deleteQuery = "DELETE FROM TeachingAssignment WHERE ClassID = @ClassID AND Semester = @Semester AND AcademicYear = @AcademicYear";
                DatabaseHelper.ExecuteNonQuery(deleteQuery, new[] {
                    new Microsoft.Data.SqlClient.SqlParameter("@ClassID", SelectedClass.ClassId),
                    new Microsoft.Data.SqlClient.SqlParameter("@Semester", SelectedSemester),
                    new Microsoft.Data.SqlClient.SqlParameter("@AcademicYear", SelectedAcademicYear)
                });

                // Insert lại những môn đã được chọn giáo viên
                foreach (var item in AssignmentList)
                {
                    // Chỉ lưu xuống Database nếu TeacherId > 0
                    if (item.SelectedTeacherId.HasValue && item.SelectedTeacherId.Value > 0)
                    {
                        TeachingAssignment newAssign = new TeachingAssignment
                        {
                            ClassId = SelectedClass.ClassId,
                            SubjectId = item.SubjectId,
                            StaffId = item.SelectedTeacherId.Value,
                            Semester = SelectedSemester,         // Lấy từ UI
                            AcademicYear = SelectedAcademicYear  // Lấy từ UI
                        };
                        newAssign.AddAssignment();
                    }
                }

                NotificationHelper.ShowSuccess("Lưu phân công giảng dạy thành công!");
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi khi lưu: " + ex.Message);
            }
        }

        private bool IsTeacherMatchSubject(string specialization, string subjectName)
        {
            if (string.IsNullOrWhiteSpace(specialization) || string.IsNullOrWhiteSpace(subjectName))
                return false;

            return specialization.Trim().Equals(subjectName.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}