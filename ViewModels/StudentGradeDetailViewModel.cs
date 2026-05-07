using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.ViewModels
{
    public class GradeDetailItem
    {
        public string SubjectName { get; set; }
        public string RegularScore { get; set; }
        public string MidTermScore { get; set; }
        public string FinalTermScore { get; set; }
        public string AverageScore { get; set; }
    }

    public class StudentGradeDetailViewModel : INotifyPropertyChanged
    {
        public string StudentName { get; set; }
        public ObservableCollection<GradeDetailItem> ScoreList { get; set; }

        // SỬA: Nhận thêm Semester và AcademicYear
        public StudentGradeDetailViewModel(string studentId, string studentName, string semester, string academicYear)
        {
            StudentName = studentName + $" ({semester} - {academicYear})"; // Thêm dòng này để tiêu đề UI rõ ràng hơn
            LoadScores(studentId, semester, academicYear);
        }

        private void LoadScores(string studentId, string semester, string academicYear)
        {
            ScoreList = new ObservableCollection<GradeDetailItem>();

            // SỬA: Thêm điều kiện lọc Semester và AcademicYear vào JOIN
            string query = @"
                SELECT sub.SubjectName, sc.RegularTestScore, sc.MidTermScore, sc.FinalTermScore, sc.AverageScore
                FROM Subject sub
                LEFT JOIN Score sc ON sub.SubjectID = sc.SubjectID 
                                  AND sc.StudentID = @StudentID 
                                  AND sc.Semester = @Semester 
                                  AND sc.AcademicYear = @AcademicYear
                WHERE sub.IsDeleted = 0";

            var parameters = new[] {
                new SqlParameter("@StudentID", studentId),
                new SqlParameter("@Semester", semester),
                new SqlParameter("@AcademicYear", academicYear)
            };

            var dt = DatabaseHelper.ExecuteQuery(query, parameters);

            foreach (DataRow row in dt.Rows)
            {
                ScoreList.Add(new GradeDetailItem
                {
                    SubjectName = row["SubjectName"].ToString(),
                    RegularScore = row["RegularTestScore"] != DBNull.Value ? Convert.ToDecimal(row["RegularTestScore"]).ToString("0.0") : "-",
                    MidTermScore = row["MidTermScore"] != DBNull.Value ? Convert.ToDecimal(row["MidTermScore"]).ToString("0.0") : "-",
                    FinalTermScore = row["FinalTermScore"] != DBNull.Value ? Convert.ToDecimal(row["FinalTermScore"]).ToString("0.0") : "-",
                    AverageScore = row["AverageScore"] != DBNull.Value ? Convert.ToDecimal(row["AverageScore"]).ToString("0.0") : "-"
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}