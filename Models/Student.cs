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
    public class Student
    {
        public int StudentId { get; set; }
        public int AccountId { get; set; }
        public required string FullName { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? FamilyBackground { get; set; }
        public string? GuardianName { get; set; }
        public string? GuardianPhoneNumber { get; set; }
        public string? Status { get; set; }

        // READ
        public static List<Student> GetAllStudents()
        {
            List<Student> students = new List<Student>();
            string query = "SELECT * FROM Student";

            DataTable data = DatabaseHelper.ExecuteQuery(query);

            foreach (DataRow row in data.Rows)
            {
                Student stu = new Student()
                {
                    StudentId = Convert.ToInt32(row["StudentID"]),
                    AccountId = Convert.ToInt32(row["AccountID"]),
                    FullName = row["FullName"].ToString() ?? "",
                    Gender = row["Gender"] as string,
                    DateOfBirth = row["DateOfBirth"] == DBNull.Value ? null : Convert.ToDateTime(row["DateOfBirth"]),
                    PhoneNumber = row["PhoneNumber"] as string,
                    Email = row["Email"] as string,
                    Address = row["Address"] as string,
                    FamilyBackground = row["FamilyBackground"] as string,
                    GuardianName = row["GuardianName"] as string,
                    GuardianPhoneNumber = row["GuardianPhoneNumber"] as string,
                    Status = row["Status"] as string
                };
                students.Add(stu);
            }
            return students;
        }

        // CREATE
        public bool AddStudent()
        {
            string query = "INSERT INTO Student (StudentID, AccountID, FullName, Gender, DateOfBirth, PhoneNumber, Email, Address, FamilyBackground, GuardianName, GuardianPhoneNumber, Status) " +
                           "VALUES (@StudentID, @AccountID, @FullName, @Gender, @DateOfBirth, @PhoneNumber, @Email, @Address, @FamilyBackground, @GuardianName, @GuardianPhoneNumber, @Status)";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@StudentID", this.StudentId),
                new SqlParameter("@AccountID", this.AccountId),
                new SqlParameter("@FullName", this.FullName),
                new SqlParameter("@Gender", this.Gender ?? (object)DBNull.Value),
                new SqlParameter("@DateOfBirth", this.DateOfBirth ?? (object)DBNull.Value),
                new SqlParameter("@PhoneNumber", this.PhoneNumber ?? (object)DBNull.Value),
                new SqlParameter("@Email", this.Email ?? (object)DBNull.Value),
                new SqlParameter("@Address", this.Address ?? (object)DBNull.Value),
                new SqlParameter("@FamilyBackground", this.FamilyBackground ?? (object)DBNull.Value),
                new SqlParameter("@GuardianName", this.GuardianName ?? (object)DBNull.Value),
                new SqlParameter("@GuardianPhoneNumber", this.GuardianPhoneNumber ?? (object)DBNull.Value),
                new SqlParameter("@Status", this.Status ?? (object)DBNull.Value)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // UPDATE
        public bool UpdateStudent()
        {
            string query = "UPDATE Student SET AccountID = @AccountID, FullName = @FullName, Gender = @Gender, " +
                           "DateOfBirth = @DateOfBirth, PhoneNumber = @PhoneNumber, Email = @Email, Address = @Address, " +
                           "FamilyBackground = @FamilyBackground, GuardianName = @GuardianName, " +
                           "GuardianPhoneNumber = @GuardianPhoneNumber, Status = @Status " +
                           "WHERE StudentID = @StudentID";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@StudentID", this.StudentId),
                new SqlParameter("@AccountID", this.AccountId),
                new SqlParameter("@FullName", this.FullName),
                new SqlParameter("@Gender", this.Gender ?? (object)DBNull.Value),
                new SqlParameter("@DateOfBirth", this.DateOfBirth ?? (object)DBNull.Value),
                new SqlParameter("@PhoneNumber", this.PhoneNumber ?? (object)DBNull.Value),
                new SqlParameter("@Email", this.Email ?? (object)DBNull.Value),
                new SqlParameter("@Address", this.Address ?? (object)DBNull.Value),
                new SqlParameter("@FamilyBackground", this.FamilyBackground ?? (object)DBNull.Value),
                new SqlParameter("@GuardianName", this.GuardianName ?? (object)DBNull.Value),
                new SqlParameter("@GuardianPhoneNumber", this.GuardianPhoneNumber ?? (object)DBNull.Value),
                new SqlParameter("@Status", this.Status ?? (object)DBNull.Value)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // DELETE
        public static bool DeleteStudent(int studentId)
        {
            string query = "DELETE FROM Student WHERE StudentID = @StudentID";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@StudentID", studentId)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }
    }
}
