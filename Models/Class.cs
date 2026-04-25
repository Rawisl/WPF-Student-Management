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
    //lớp học
    public class Class
    {
        public int ClassId { get; set; }
        public required string ClassName { get; set; }
        public int Grade { get; set; }
        public int ClassSize { get; set; } = 0;
        public int? HomeroomTeacherId { get; set; }

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
                    HomeroomTeacherId = row["HomeroomTeacherID"] == DBNull.Value ? null : Convert.ToInt32(row["HomeroomTeacherID"])
                };
                classes.Add(cls);
            }
            return classes;
        }

        // CREATE
        public bool AddClass()
        {
            string query = "INSERT INTO Class (ClassID, ClassName, Grade, ClassSize, HomeroomTeacherID) " +
                           "VALUES (@ClassID, @ClassName, @Grade, @ClassSize, @HomeroomTeacherID)";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@ClassID", this.ClassId),
                new SqlParameter("@ClassName", this.ClassName),
                new SqlParameter("@Grade", this.Grade),
                new SqlParameter("@ClassSize", this.ClassSize),
                new SqlParameter("@HomeroomTeacherID", this.HomeroomTeacherId ?? (object)DBNull.Value)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // UPDATE
        public bool UpdateClass()
        {
            string query = "UPDATE Class SET ClassName = @ClassName, Grade = @Grade, " +
                           "ClassSize = @ClassSize, HomeroomTeacherID = @HomeroomTeacherID " +
                           "WHERE ClassID = @ClassID";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@ClassID", this.ClassId),
                new SqlParameter("@ClassName", this.ClassName),
                new SqlParameter("@Grade", this.Grade),
                new SqlParameter("@ClassSize", this.ClassSize),
                new SqlParameter("@HomeroomTeacherID", this.HomeroomTeacherId ?? (object)DBNull.Value)
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

        //Lấy thông tin lớp cùng tên gvcn
        public static DataTable GetAllClassesWithTeacher()
        {
            string query = @"
                SELECT c.ClassID, c.ClassName, c.Grade, c.ClassSize, c.HomeroomTeacherID, e.FullName AS TeacherName 
                FROM Class c
                LEFT JOIN Employee e ON c.HomeroomTeacherID = e.EmployeeID";

            return DatabaseHelper.ExecuteQuery(query);
        }
    }
}
