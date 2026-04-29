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
            string query = "INSERT INTO Score (StudentID, SubjectID, RegularTestScore, MidTermScore, FinalTermScore) " +
                           "VALUES (@StudentID, @SubjectID, @RegularTestScore, @MidTermScore, @FinalTermScore)";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@StudentID", this.StudentId),
                new SqlParameter("@SubjectID", this.SubjectId),
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
                           "RegularTestScore = @RegularTestScore, MidTermScore = @MidTermScore, FinalTermScore = @FinalTermScore " +
                           "WHERE ScoreID = @ScoreID";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@ScoreID", this.ScoreId),
                new SqlParameter("@StudentID", this.StudentId),
                new SqlParameter("@SubjectID", this.SubjectId),
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
