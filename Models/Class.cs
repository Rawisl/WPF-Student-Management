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
        public int ClassSize { get; set; } = 0;
        public int? HomeroomTeacherId { get; set; }

        // BỔ SUNG CHIỀU THỜI GIAN (Năm học)
        public string AcademicYear { get; set; } = "2025-2026";

        // READ
        public static List<Class> GetAllClasses()
        {
            List<Class> classes = new List<Class>();
            string query = "SELECT * FROM Class";

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
                    // Lấy năm học từ DB lên
                    AcademicYear = row["AcademicYear"] != DBNull.Value ? row["AcademicYear"].ToString() : "2025-2026"
                };
                classes.Add(cls);
            }
            return classes;
        }

        // CREATE
        public bool AddClass()
        {
            string query = "INSERT INTO Class (ClassName, Grade, ClassSize, HomeroomTeacherID, AcademicYear) " +
                           "VALUES (@ClassName, @Grade, @ClassSize, @HomeroomTeacherID, @AcademicYear)";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@ClassName", this.ClassName),
                new SqlParameter("@Grade", this.Grade),
                new SqlParameter("@ClassSize", this.ClassSize),
                new SqlParameter("@HomeroomTeacherID", this.HomeroomTeacherId ?? (object)DBNull.Value),
                // Truyền năm học xuống DB
                new SqlParameter("@AcademicYear", this.AcademicYear)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // UPDATE
        public bool UpdateClass()
        {
            string query = "UPDATE Class SET ClassName = @ClassName, Grade = @Grade, " +
                           "ClassSize = @ClassSize, HomeroomTeacherID = @HomeroomTeacherID, AcademicYear = @AcademicYear " +
                           "WHERE ClassID = @ClassID";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@ClassID", this.ClassId),
                new SqlParameter("@ClassName", this.ClassName),
                new SqlParameter("@Grade", this.Grade),
                new SqlParameter("@ClassSize", this.ClassSize),
                new SqlParameter("@HomeroomTeacherID", this.HomeroomTeacherId ?? (object)DBNull.Value),
                // Truyền năm học xuống DB
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
            // Bổ sung c.AcademicYear vào để UI có thông tin
            string query = @"
                SELECT c.ClassID, c.ClassName, c.Grade, c.ClassSize, c.HomeroomTeacherID, c.AcademicYear, e.FullName AS TeacherName 
                FROM Class c
                LEFT JOIN Employee e ON c.HomeroomTeacherID = e.EmployeeID";

            return DatabaseHelper.ExecuteQuery(query);
        }
    }
}