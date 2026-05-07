using Microsoft.Data.SqlClient;
using System;
using System.Data;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.Models
{
    public class SubjectReport
    {
        public int SubjectReportId { get; set; }
        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public string Semester { get; set; } = "Học kỳ 1";
        public string AcademicYear { get; set; } = "2025-2026";
        public int TotalStudents { get; set; }
        public int PassedStudents { get; set; }
        public decimal PassRate { get; set; }
        public bool IsLocked { get; set; } = true;
        public int CreatedByTeacherId { get; set; }
        public DateTime CreatedAt { get; set; }

        // VŨ KHÍ BÍ MẬT: Hàm kiểm tra xem Bảng điểm này đã bị Khóa Sổ chưa?
        // (Tí nữa ViewModel nhập điểm sẽ dùng cái này để Disable nút LƯU)
        public static bool IsSubjectReportLocked(int classId, int subjectId, string semester, string academicYear)
        {
            string query = "SELECT IsLocked FROM SubjectReport WHERE ClassID = @ClassID AND SubjectID = @SubjectID AND Semester = @Semester AND AcademicYear = @AcademicYear";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@ClassID", classId),
                new SqlParameter("@SubjectID", subjectId),
                new SqlParameter("@Semester", semester),
                new SqlParameter("@AcademicYear", academicYear)
            };

            DataTable data = DatabaseHelper.ExecuteQuery(query, parameters);

            // Nếu có record báo cáo rồi thì kiểm tra cột IsLocked, chưa có thì auto là chưa khóa (false)
            if (data != null && data.Rows.Count > 0)
            {
                return Convert.ToBoolean(data.Rows[0]["IsLocked"]);
            }
            return false;
        }
    }
}