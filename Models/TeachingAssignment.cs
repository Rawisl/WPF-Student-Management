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
                    SubjectId = Convert.ToInt32(row["SubjectID"])
                };
                assignments.Add(assignment);
            }
            return assignments;
        }

        // CREATE
        public bool AddAssignment()
        {
            string query = "INSERT INTO TeachingAssignment (EmployeeID, ClassID, SubjectID) " +
                           "VALUES (@EmployeeID, @ClassID, @SubjectID)";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@EmployeeID", this.StaffId),
                new SqlParameter("@ClassID", this.ClassId),
                new SqlParameter("@SubjectID", this.SubjectId)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // DELETE
        public static bool DeleteAssignment(int staffId, int classId, int subjectId)
        {
            string query = "DELETE FROM TeachingAssignment WHERE EmployeeID = @EmployeeID AND ClassID = @ClassID AND SubjectID = @SubjectID";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@EmployeeID", staffId),
                new SqlParameter("@ClassID", classId),
                new SqlParameter("@SubjectID", subjectId)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }
    }
}
