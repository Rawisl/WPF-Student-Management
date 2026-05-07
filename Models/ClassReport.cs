using Microsoft.Data.SqlClient;
using System;
using System.Data;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.Models
{
    public class ClassReport
    {
        public int ClassReportId { get; set; }
        public int ClassId { get; set; }
        public string Semester { get; set; } = "Học kỳ 1";
        public string AcademicYear { get; set; } = "2025-2026";
        public int TotalStudents { get; set; }
        public bool IsLocked { get; set; } = true;
        public int CreatedByTeacherId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Hàm kiểm tra lớp đã bị GVCN khóa sổ chưa (Dùng cho luồng GVCN sau này)
        public static bool IsClassReportLocked(int classId, string semester, string academicYear)
        {
            string query = "SELECT IsLocked FROM ClassReport WHERE ClassID = @ClassID AND Semester = @Semester AND AcademicYear = @AcademicYear";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@ClassID", classId),
                new SqlParameter("@Semester", semester),
                new SqlParameter("@AcademicYear", academicYear)
            };

            DataTable data = DatabaseHelper.ExecuteQuery(query, parameters);

            if (data != null && data.Rows.Count > 0)
            {
                return Convert.ToBoolean(data.Rows[0]["IsLocked"]);
            }
            return false;
        }
    }
}