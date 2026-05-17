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
    public class DeletedSubjectDto
    {
        public int SubjectId { get; set; }
        public required string SubjectName { get; set; }
        public required string GradeType { get; set; }
        public int ScoreCount { get; set; }
        public int TeachingCount { get; set; }
        public int ReportCount { get; set; }
    }
    public class Subject
    {
        public int SubjectId { get; set; }
        public required string SubjectName { get; set; }
        public required string GradeType { get; set; }
        public bool IsDeleted { get; set; } = false;

        // READ
        public static List<Subject> GetAllSubjects()
        {
            List<Subject> subjects = new List<Subject>();
            string query = "SELECT * FROM Subject WHERE IsDeleted = 0"; // Exclude logically deleted records if preferred

            DataTable data = DatabaseHelper.ExecuteQuery(query);

            foreach (DataRow row in data.Rows)
            {
                Subject sub = new Subject()
                {
                    SubjectId = Convert.ToInt32(row["SubjectID"]),
                    SubjectName = row["SubjectName"].ToString() ?? "",
                    GradeType = row["GradeType"].ToString() ?? "",
                    IsDeleted = Convert.ToBoolean(row["IsDeleted"])
                };
                subjects.Add(sub);
            }
            return subjects;
        }

        // CREATE
        public bool AddSubject()
        {
            string query = "INSERT INTO Subject (SubjectName, GradeType, IsDeleted) " +
                           "VALUES (@SubjectName, @GradeType, @IsDeleted)";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@SubjectName", this.SubjectName),
                new SqlParameter("@GradeType", this.GradeType),
                new SqlParameter("@IsDeleted", this.IsDeleted)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // UPDATE
        public bool UpdateSubject()
        {
            string query = "UPDATE Subject SET SubjectName = @SubjectName, GradeType = @GradeType, IsDeleted = @IsDeleted " +
                           "WHERE SubjectID = @SubjectID";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@SubjectID", this.SubjectId),
                new SqlParameter("@SubjectName", this.SubjectName),
                new SqlParameter("@GradeType", this.GradeType),
                new SqlParameter("@IsDeleted", this.IsDeleted)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // DELETE — trigger TRG_Subject_SmartDelete handles soft/hard delete automatically
        public static bool DeleteSubject(int subjectId)
        {
            string query = "DELETE FROM Subject WHERE SubjectID = @SubjectID";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@SubjectID", subjectId)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // 1. READ — Lấy danh sách các môn đã bị xóa mềm từ View
        public static List<DeletedSubjectDto> GetDeletedSubjects()
        {
            List<DeletedSubjectDto> deletedSubjects = new List<DeletedSubjectDto>();
            string query = "SELECT * FROM vw_DeletedSubjects";

            DataTable data = DatabaseHelper.ExecuteQuery(query);

            foreach (DataRow row in data.Rows)
            {
                var sub = new DeletedSubjectDto()
                {
                    SubjectId = Convert.ToInt32(row["SubjectID"]),
                    SubjectName = row["SubjectName"].ToString() ?? "",
                    GradeType = row["GradeType"].ToString() ?? "",
                    ScoreCount = Convert.ToInt32(row["ScoreCount"]),
                    TeachingCount = Convert.ToInt32(row["TeachingCount"]),
                    ReportCount = Convert.ToInt32(row["ReportCount"])
                };
                deletedSubjects.Add(sub);
            }
            return deletedSubjects;
        }

        // 2. UPDATE — Khôi phục môn học (bật IsDeleted về 0)
        public static bool RestoreSubject(int subjectId)
        {
            string query = "UPDATE Subject SET IsDeleted = 0 WHERE SubjectID = @SubjectID";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@SubjectID", subjectId)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

    }
}
