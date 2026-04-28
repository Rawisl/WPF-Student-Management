using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
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

        // Nhận ID và Tên học sinh từ màn hình danh sách truyền sang
        public StudentGradeDetailViewModel(int studentId, string studentName)
        {
            StudentName = studentName;
            LoadScores(studentId);
        }

        private void LoadScores(int studentId)
        {
            ScoreList = new ObservableCollection<GradeDetailItem>();

            string query = @"
                SELECT sub.SubjectName, sc.RegularTestScore, sc.MidTermScore, sc.FinalTermScore, sc.AverageScore
                FROM Score sc
                JOIN Subject sub ON sc.SubjectID = sub.SubjectID
                WHERE sc.StudentID = @StudentID";

            var dt = DatabaseHelper.ExecuteQuery(query, new[] { new SqlParameter("@StudentID", studentId) });

            foreach (DataRow row in dt.Rows)
            {
                ScoreList.Add(new GradeDetailItem
                {
                    SubjectName = row["SubjectName"].ToString(),
                    // Nếu chưa có điểm (NULL) thì hiển thị dấu gạch ngang "-"
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