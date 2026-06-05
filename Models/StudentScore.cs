using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.Models
{
    public class StudentScore
    {
        public int ScoreId { get; set; }
        public required string StudentId { get; set; }
        public int SubjectId { get; set; }

        public string Semester { get; set; } = "Học kỳ 1";
        public string AcademicYear { get; set; } = "2025-2026";

        public decimal? RegularScore1 { get; set; }
        public decimal? RegularScore2 { get; set; }
        public decimal? RegularScore3 { get; set; }
        public decimal? RegularScore4 { get; set; }

        public decimal? MidTermScore { get; set; }
        public decimal? FinalTermScore { get; set; }

        public decimal? AverageScore { get; set; }

        // READ
        public static List<StudentScore> GetAllScores()
        {
            List<StudentScore> scores = new List<StudentScore>();
            string query = "SELECT * FROM Score";

            DataTable data = DatabaseHelper.ExecuteQuery(query);

            foreach (DataRow row in data.Rows)
            {
                StudentScore score = new StudentScore()
                {
                    ScoreId = Convert.ToInt32(row["ScoreID"]),
                    StudentId = row["StudentID"].ToString() ?? "",
                    SubjectId = Convert.ToInt32(row["SubjectID"]),
                    Semester = row["Semester"] != DBNull.Value ? row["Semester"].ToString()! : "Học kỳ 1",
                    AcademicYear = row["AcademicYear"] != DBNull.Value ? row["AcademicYear"].ToString()! : "2025-2026",

                    RegularScore1 = row["RegularScore1"] == DBNull.Value ? null : Convert.ToDecimal(row["RegularScore1"]),
                    RegularScore2 = row["RegularScore2"] == DBNull.Value ? null : Convert.ToDecimal(row["RegularScore2"]),
                    RegularScore3 = row["RegularScore3"] == DBNull.Value ? null : Convert.ToDecimal(row["RegularScore3"]),
                    RegularScore4 = row["RegularScore4"] == DBNull.Value ? null : Convert.ToDecimal(row["RegularScore4"]),

                    MidTermScore = row["MidTermScore"] == DBNull.Value ? null : Convert.ToDecimal(row["MidTermScore"]),
                    FinalTermScore = row["FinalTermScore"] == DBNull.Value ? null : Convert.ToDecimal(row["FinalTermScore"]),
                    AverageScore = row["AverageScore"] == DBNull.Value ? null : Convert.ToDecimal(row["AverageScore"])
                };
                scores.Add(score);
            }
            return scores;
        }

        // CREATE
        public bool AddScore()
        {
            string query = @"INSERT INTO Score (StudentID, SubjectID, Semester, AcademicYear, 
                                                RegularScore1, RegularScore2, RegularScore3, RegularScore4, 
                                                MidTermScore, FinalTermScore, AverageScore) 
                             VALUES (@StudentID, @SubjectID, @Semester, @AcademicYear, 
                                     @TX1, @TX2, @TX3, @TX4, 
                                     @MidTermScore, @FinalTermScore, @AverageScore)";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@StudentID", this.StudentId),
                new SqlParameter("@SubjectID", this.SubjectId),
                new SqlParameter("@Semester", this.Semester),
                new SqlParameter("@AcademicYear", this.AcademicYear),
                new SqlParameter("@TX1", this.RegularScore1 ?? (object)DBNull.Value),
                new SqlParameter("@TX2", this.RegularScore2 ?? (object)DBNull.Value),
                new SqlParameter("@TX3", this.RegularScore3 ?? (object)DBNull.Value),
                new SqlParameter("@TX4", this.RegularScore4 ?? (object)DBNull.Value),
                new SqlParameter("@MidTermScore", this.MidTermScore ?? (object)DBNull.Value),
                new SqlParameter("@FinalTermScore", this.FinalTermScore ?? (object)DBNull.Value),
                new SqlParameter("@AverageScore", this.AverageScore ?? (object)DBNull.Value) // Bắt buộc truyền xuống
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // UPDATE
        public bool UpdateScore()
        {
            string query = @"UPDATE Score SET StudentID = @StudentID, SubjectID = @SubjectID, 
                                              Semester = @Semester, AcademicYear = @AcademicYear, 
                                              RegularScore1 = @TX1, RegularScore2 = @TX2, RegularScore3 = @TX3, RegularScore4 = @TX4, 
                                              MidTermScore = @MidTermScore, FinalTermScore = @FinalTermScore, AverageScore = @AverageScore 
                             WHERE ScoreID = @ScoreID";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@ScoreID", this.ScoreId),
                new SqlParameter("@StudentID", this.StudentId),
                new SqlParameter("@SubjectID", this.SubjectId),
                new SqlParameter("@Semester", this.Semester),
                new SqlParameter("@AcademicYear", this.AcademicYear),
                new SqlParameter("@TX1", this.RegularScore1 ?? (object)DBNull.Value),
                new SqlParameter("@TX2", this.RegularScore2 ?? (object)DBNull.Value),
                new SqlParameter("@TX3", this.RegularScore3 ?? (object)DBNull.Value),
                new SqlParameter("@TX4", this.RegularScore4 ?? (object)DBNull.Value),
                new SqlParameter("@MidTermScore", this.MidTermScore ?? (object)DBNull.Value),
                new SqlParameter("@FinalTermScore", this.FinalTermScore ?? (object)DBNull.Value),
                new SqlParameter("@AverageScore", this.AverageScore ?? (object)DBNull.Value) // Bắt buộc truyền xuống
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        public static bool DeleteScore(int scoreId)
        {
            string query = "DELETE FROM Score WHERE ScoreID = @ScoreID";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@ScoreID", scoreId)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }
    }
}