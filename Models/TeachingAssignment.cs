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
    public class TeachingAssignment
    {
        public int StaffId { get; set; } // Mapped to EmployeeID
        public int ClassId { get; set; }
        public int SubjectId { get; set; }

        // --- BỔ SUNG CHIỀU THỜI GIAN ---
        public string Semester { get; set; } = "Học kỳ 1";
        public string AcademicYear { get; set; } = "2025-2026";
        // -------------------------------

        // READ
        public static List<TeachingAssignment> GetAllAssignments()
        {
            List<TeachingAssignment> assignments = new List<TeachingAssignment>();
            string query = "SELECT * FROM TeachingAssignment";

            DataTable data = DatabaseHelper.ExecuteQuery(query);

            foreach (DataRow row in data.Rows)
            {
                TeachingAssignment assignment = new TeachingAssignment()
                {
                    StaffId = Convert.ToInt32(row["EmployeeID"]),
                    ClassId = Convert.ToInt32(row["ClassID"]),
                    SubjectId = Convert.ToInt32(row["SubjectID"]),

                    // Map thêm 2 trường này
                    Semester = row["Semester"] != DBNull.Value ? row["Semester"].ToString()! : "Học kỳ 1",
                    AcademicYear = row["AcademicYear"] != DBNull.Value ? row["AcademicYear"].ToString()! : "2025-2026"
                };
                assignments.Add(assignment);
            }
            return assignments;
        }

        // CREATE
        public bool AddAssignment()
        {
            string query = "INSERT INTO TeachingAssignment (EmployeeID, ClassID, SubjectID, Semester, AcademicYear) " +
                           "VALUES (@EmployeeID, @ClassID, @SubjectID, @Semester, @AcademicYear)";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@EmployeeID", this.StaffId),
                new SqlParameter("@ClassID", this.ClassId),
                new SqlParameter("@SubjectID", this.SubjectId),
                new SqlParameter("@Semester", this.Semester),
                new SqlParameter("@AcademicYear", this.AcademicYear)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // DELETE
        // CẬP NHẬT: Thêm Semester và AcademicYear vào điều kiện xóa (Bắt buộc do Khóa chính đã thay đổi)
        public static bool DeleteAssignment(int staffId, int classId, int subjectId, string semester, string academicYear)
        {
            string query = "DELETE FROM TeachingAssignment " +
                           "WHERE EmployeeID = @EmployeeID AND ClassID = @ClassID AND SubjectID = @SubjectID " +
                           "AND Semester = @Semester AND AcademicYear = @AcademicYear";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@EmployeeID", staffId),
                new SqlParameter("@ClassID", classId),
                new SqlParameter("@SubjectID", subjectId),
                new SqlParameter("@Semester", semester),
                new SqlParameter("@AcademicYear", academicYear)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }
    }
}