using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Data;
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
        public bool IsFailed { get; set; }
    }

    // ĐÃ FIX: Đổi sang kế thừa ObservableObject để UI tự động cập nhật
    public partial class StudentGradeDetailViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _studentName;

        [ObservableProperty]
        private ObservableCollection<GradeDetailItem> _scoreList;

        [ObservableProperty]
        private bool _isCloseButtonVisible;

        public StudentGradeDetailViewModel(string studentId, string studentName, string semester, string academicYear, bool showCloseButton = true)
        {
            IsCloseButtonVisible = showCloseButton; // UI sẽ bắt được cái này lập tức nhờ [ObservableProperty]
            StudentName = studentName + $" ({semester} - {academicYear})";
            LoadScores(studentId, semester, academicYear);
        }

        private void LoadScores(string studentId, string semester, string academicYear)
        {
            ScoreList = new ObservableCollection<GradeDetailItem>();

            try
            {
                string paramQuery = "SELECT ISNULL((SELECT Value FROM Parameter WHERE ParameterName = 'NumPassingGrade'), 5.0) as PassingGrade";
                DataTable dtParam = DatabaseHelper.ExecuteQuery(paramQuery);
                decimal passingGrade = Convert.ToDecimal(dtParam.Rows[0]["PassingGrade"]);

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
                    decimal? avgScore = row["AverageScore"] != DBNull.Value ? Convert.ToDecimal(row["AverageScore"]) : null;
                    bool isFailed = avgScore.HasValue && avgScore.Value < passingGrade;

                    ScoreList.Add(new GradeDetailItem
                    {
                        SubjectName = row["SubjectName"].ToString(),
                        RegularScore = row["RegularTestScore"] != DBNull.Value ? Convert.ToDecimal(row["RegularTestScore"]).ToString("0.0") : "-",
                        MidTermScore = row["MidTermScore"] != DBNull.Value ? Convert.ToDecimal(row["MidTermScore"]).ToString("0.0") : "-",
                        FinalTermScore = row["FinalTermScore"] != DBNull.Value ? Convert.ToDecimal(row["FinalTermScore"]).ToString("0.0") : "-",
                        AverageScore = avgScore.HasValue ? avgScore.Value.ToString("0.0") : "-",
                        IsFailed = isFailed
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi tải chi tiết điểm: " + ex.Message);
            }
        }
    }
}