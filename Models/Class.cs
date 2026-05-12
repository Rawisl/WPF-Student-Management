using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.Models
{
    // lớp học
    public class Class
    {
        public int ClassId { get; set; }
        public required string ClassName { get; set; }
        public int Grade { get; set; }
        public int ClassSize { get; set; } = 0; // Read from vw_ClassSize, not a DB column
        public int? HomeroomTeacherId { get; set; }
        public string AcademicYear { get; set; } = "2025-2026";

        // READ — join vw_ClassSize to get ClassSize
        public static List<Class> GetAllClasses()
        {
            List<Class> classes = new List<Class>();
            string query = @"
                SELECT c.ClassID, c.ClassName, c.Grade, c.HomeroomTeacherID, c.AcademicYear,
                       ISNULL(v.ClassSize, 0) AS ClassSize
                FROM Class c
                LEFT JOIN vw_ClassSize v
                    ON v.ClassID = c.ClassID AND v.AcademicYear = c.AcademicYear";

            DataTable data = DatabaseHelper.ExecuteQuery(query);

            foreach (DataRow row in data.Rows)
            {
                Class cls = new Class()
                {
                    ClassId = Convert.ToInt32(row["ClassID"]),
                    ClassName = row["ClassName"].ToString() ?? "",
                    Grade = Convert.ToInt32(row["Grade"]),
                    ClassSize = Convert.ToInt32(row["ClassSize"]),
                    HomeroomTeacherId = row["HomeroomTeacherID"] == DBNull.Value ? null : Convert.ToInt32(row["HomeroomTeacherID"]),
                    AcademicYear = row["AcademicYear"] != DBNull.Value ? row["AcademicYear"].ToString()! : "2025-2026"
                };
                classes.Add(cls);
            }
            return classes;
        }

        // CREATE — ClassSize is no longer a column; removed from INSERT
        public bool AddClass()
        {
            string query = "INSERT INTO Class (ClassName, Grade, HomeroomTeacherID, AcademicYear) " +
                           "VALUES (@ClassName, @Grade, @HomeroomTeacherID, @AcademicYear)";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@ClassName", this.ClassName),
                new SqlParameter("@Grade", this.Grade),
                new SqlParameter("@HomeroomTeacherID", this.HomeroomTeacherId ?? (object)DBNull.Value),
                new SqlParameter("@AcademicYear", this.AcademicYear)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // UPDATE — ClassSize is no longer a column; removed from UPDATE
        public bool UpdateClass()
        {
            string query = "UPDATE Class SET ClassName = @ClassName, Grade = @Grade, " +
                           "HomeroomTeacherID = @HomeroomTeacherID, AcademicYear = @AcademicYear " +
                           "WHERE ClassID = @ClassID";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@ClassID", this.ClassId),
                new SqlParameter("@ClassName", this.ClassName),
                new SqlParameter("@Grade", this.Grade),
                new SqlParameter("@HomeroomTeacherID", this.HomeroomTeacherId ?? (object)DBNull.Value),
                new SqlParameter("@AcademicYear", this.AcademicYear)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // DELETE
        public static bool DeleteClass(int classId)
        {
            string query = "DELETE FROM Class WHERE ClassID = @ClassID";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@ClassID", classId)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // Lấy thông tin lớp cùng tên gvcn
        public static DataTable GetAllClassesWithTeacher()
        {
            string query = @"
                SELECT c.ClassID, c.ClassName, c.Grade, c.HomeroomTeacherID, c.AcademicYear,
                       ISNULL(v.ClassSize, 0) AS ClassSize, e.FullName AS TeacherName
                FROM Class c
                LEFT JOIN Employee e ON c.HomeroomTeacherID = e.EmployeeID
                LEFT JOIN vw_ClassSize v
                    ON v.ClassID = c.ClassID AND v.AcademicYear = c.AcademicYear";

            return DatabaseHelper.ExecuteQuery(query);
        }
    }
}
