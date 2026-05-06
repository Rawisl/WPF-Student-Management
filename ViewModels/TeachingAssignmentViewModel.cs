using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
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
            set { _selectedClass = value; OnPropertyChanged(); LoadAssignmentsForClass(); }
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
            LoadInitialData();
        }

        private void LoadInitialData()
        {
            // Tải toàn bộ danh sách lớp đưa lên ComboBox
            ClassList = new ObservableCollection<Class>(Class.GetAllClasses());
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
            var currentAssignments = TeachingAssignment.GetAllAssignments().Where(a => a.ClassId == SelectedClass.ClassId).ToList();

            var list = new ObservableCollection<AssignmentDisplayItem>();

            foreach (var subject in allSubjects)
            {
                // LỌC GV THEO CHUYÊN MÔN: Do DB đã đồng nhất tên, hàm lọc sẽ chạy cực kỳ mượt
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

                // Xóa toàn bộ phân công cũ của lớp này trước
                string deleteQuery = "DELETE FROM TeachingAssignment WHERE ClassID = @ClassID";
                DatabaseHelper.ExecuteNonQuery(deleteQuery, new[] { new Microsoft.Data.SqlClient.SqlParameter("@ClassID", SelectedClass.ClassId) });

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
                            StaffId = item.SelectedTeacherId.Value
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

        // HÀM KIỂM TRA ĐƯỢC ĐẶT ĐÚNG VỊ TRÍ TRONG CLASS
        private bool IsTeacherMatchSubject(string specialization, string subjectName)
        {
            if (string.IsNullOrWhiteSpace(specialization) || string.IsNullOrWhiteSpace(subjectName))
                return false;

            // Chỉ cần so sánh trực tiếp, bỏ qua phân biệt hoa/thường và khoảng trắng thừa 2 đầu
            return specialization.Trim().Equals(subjectName.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}