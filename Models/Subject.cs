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
            string query = "INSERT INTO Subject (SubjectID, SubjectName, GradeType, IsDeleted) " +
                           "VALUES (@SubjectID, @SubjectName, @GradeType, @IsDeleted)";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@SubjectID", this.SubjectId),
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

        // DELETE (Logical Delete based on IsDeleted column)
        public static bool DeleteSubject(int subjectId)
        {
            // Instead of physical deletion, we update the IsDeleted flag as schema structure.
            string query = "UPDATE Subject SET IsDeleted = 1 WHERE SubjectID = @SubjectID";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@SubjectID", subjectId)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }
    }
}
