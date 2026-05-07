using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.Models
{
    public class StudentScore
    {
        public int ScoreId { get; set; }
        public required string StudentId { get; set; }
        public int SubjectId { get; set; }

        // --- BỔ SUNG CHIỀU THỜI GIAN (BẮT BUỘC ĐỂ SCALE) ---
        public string Semester { get; set; } = "Học kỳ 1";
        public string AcademicYear { get; set; } = "2025-2026";
        // ---------------------------------------------------

        public decimal? RegularTestScore { get; set; }
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

                    // Lấy Semester và AcademicYear từ DB
                    Semester = row["Semester"] != DBNull.Value ? row["Semester"].ToString()! : "Học kỳ 1",
                    AcademicYear = row["AcademicYear"] != DBNull.Value ? row["AcademicYear"].ToString()! : "2025-2026",

                    RegularTestScore = row["RegularTestScore"] == DBNull.Value ? null : Convert.ToDecimal(row["RegularTestScore"]),
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
            // AverageScore is calculated via SQL Trigger, so it is omitted from INSERT
            string query = "INSERT INTO Score (StudentID, SubjectID, Semester, AcademicYear, RegularTestScore, MidTermScore, FinalTermScore) " +
                           "VALUES (@StudentID, @SubjectID, @Semester, @AcademicYear, @RegularTestScore, @MidTermScore, @FinalTermScore)";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@StudentID", this.StudentId),
                new SqlParameter("@SubjectID", this.SubjectId),
                new SqlParameter("@Semester", this.Semester),
                new SqlParameter("@AcademicYear", this.AcademicYear),
                new SqlParameter("@RegularTestScore", this.RegularTestScore ?? (object)DBNull.Value),
                new SqlParameter("@MidTermScore", this.MidTermScore ?? (object)DBNull.Value),
                new SqlParameter("@FinalTermScore", this.FinalTermScore ?? (object)DBNull.Value)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // UPDATE
        public bool UpdateScore()
        {
            // AverageScore is calculated via SQL Trigger, so it is omitted from UPDATE
            string query = "UPDATE Score SET StudentID = @StudentID, SubjectID = @SubjectID, " +
                           "Semester = @Semester, AcademicYear = @AcademicYear, " + // BỔ SUNG UPDATE THỜI GIAN
                           "RegularTestScore = @RegularTestScore, MidTermScore = @MidTermScore, FinalTermScore = @FinalTermScore " +
                           "WHERE ScoreID = @ScoreID";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@ScoreID", this.ScoreId),
                new SqlParameter("@StudentID", this.StudentId),
                new SqlParameter("@SubjectID", this.SubjectId),
                new SqlParameter("@Semester", this.Semester),
                new SqlParameter("@AcademicYear", this.AcademicYear),
                new SqlParameter("@RegularTestScore", this.RegularTestScore ?? (object)DBNull.Value),
                new SqlParameter("@MidTermScore", this.MidTermScore ?? (object)DBNull.Value),
                new SqlParameter("@FinalTermScore", this.FinalTermScore ?? (object)DBNull.Value)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // DELETE
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