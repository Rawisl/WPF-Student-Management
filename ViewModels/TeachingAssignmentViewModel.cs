using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using WPF_Student_Management.Components;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;

namespace WPF_Student_Management.ViewModels
{
    // ĐÃ FIX: Class trung gian cũng kế thừa ObservableObject và phải có chữ 'partial'
    public partial class AssignmentDisplayItem : ObservableObject
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; }

        public ObservableCollection<Staff> AvailableTeachers { get; set; }

        [ObservableProperty]
        private int? _selectedTeacherId;
    }

    // ĐÃ FIX: Kế thừa ObservableObject
    public partial class TeachingAssignmentViewModel : ObservableObject
    {
        // Tự động gọi LoadAssignmentsForClass khi SelectedSemester thay đổi
        [ObservableProperty]
        private string _selectedSemester = "Học kỳ 1";
        partial void OnSelectedSemesterChanged(string value) => LoadAssignmentsForClass();

        // Tự động gọi LoadClassesForYear khi SelectedAcademicYear thay đổi
        [ObservableProperty]
        private string _selectedAcademicYear = "2025-2026";
        partial void OnSelectedAcademicYearChanged(string value) => LoadClassesForYear();

        [ObservableProperty]
        private ObservableCollection<Class> _classList;

        // Tự động gọi LoadAssignmentsForClass và đánh thức nút Save khi SelectedClass thay đổi
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private Class _selectedClass;
        partial void OnSelectedClassChanged(Class value) => LoadAssignmentsForClass();

        [ObservableProperty]
        private ObservableCollection<AssignmentDisplayItem> _assignmentList;

        public TeachingAssignmentViewModel()
        {
            // Constructor siêu sạch
            LoadClassesForYear();
        }

        private void LoadClassesForYear()
        {
            var classes = Class.GetAllClasses().Where(c => c.AcademicYear == SelectedAcademicYear).ToList();
            ClassList = new ObservableCollection<Class>(classes);

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

            var currentAssignments = TeachingAssignment.GetAllAssignments()
                                    .Where(a => a.ClassId == SelectedClass.ClassId
                                             && a.Semester == SelectedSemester
                                             && a.AcademicYear == SelectedAcademicYear).ToList();

            var list = new ObservableCollection<AssignmentDisplayItem>();

            foreach (var subject in allSubjects)
            {
                var matchedTeachers = allStaff.Where(t => t.Specialization == subject.SubjectId).ToList();
                matchedTeachers.Insert(0, new Staff { StaffId = 0, FullName = "Trống" });

                var item = new AssignmentDisplayItem
                {
                    SubjectId = subject.SubjectId,
                    SubjectName = subject.SubjectName,
                    AvailableTeachers = new ObservableCollection<Staff>(matchedTeachers)
                };

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

        // Điều kiện để nút Lưu sáng lên
        private bool CanSave() => SelectedClass != null;

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void Save()
        {
            try
            {
                if (SelectedClass == null) return;

                // Xóa toàn bộ phân công cũ của lớp này TRONG HỌC KỲ VÀ NĂM HỌC HIỆN TẠI
                string deleteQuery = "DELETE FROM TeachingAssignment WHERE ClassID = @ClassID AND Semester = @Semester AND AcademicYear = @AcademicYear";
                DatabaseHelper.ExecuteNonQuery(deleteQuery, new[] {
                    new SqlParameter("@ClassID", SelectedClass.ClassId),
                    new SqlParameter("@Semester", SelectedSemester),
                    new SqlParameter("@AcademicYear", SelectedAcademicYear)
                });

                // Insert lại những môn đã được chọn giáo viên
                foreach (var item in AssignmentList)
                {
                    if (item.SelectedTeacherId.HasValue && item.SelectedTeacherId.Value > 0)
                    {
                        TeachingAssignment newAssign = new TeachingAssignment
                        {
                            ClassId = SelectedClass.ClassId,
                            SubjectId = item.SubjectId,
                            StaffId = item.SelectedTeacherId.Value,
                            Semester = SelectedSemester,
                            AcademicYear = SelectedAcademicYear
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
    }
}