using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.Models
{
    public class Student
    {
        public required string StudentId { get; set; } // Changed to string
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
                    StudentId = row["StudentID"].ToString() ?? "", // Cast to string
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
        // SEARCH FOR STUDENTS: Discrete Search
        public static List<Student> SearchStudents(
            string? studentId = null,
            string? fullName = null,
            int? classId = null,
            string? gender = null,
            DateTime? dateOfBirth = null,
            string? phoneNumber = null,
            string? email = null,
            string? address = null,
            string? status = null)
        {
            List<Student> students = new List<Student>();
            List<SqlParameter> parameters = new List<SqlParameter>();

            // Base query using DISTINCT to prevent duplicates if a student has multiple class records somehow
            StringBuilder queryBuilder = new StringBuilder(@"
                SELECT DISTINCT s.* 
                FROM Student s
                LEFT JOIN ClassPlacement cp ON s.StudentID = cp.StudentID
                WHERE 1=1 "
            );

            // --- EXACT MATCHES ---

            if (!string.IsNullOrWhiteSpace(studentId))
            {
                queryBuilder.Append(" AND s.StudentID = @StudentID");
                parameters.Add(new SqlParameter("@StudentID", studentId.Trim()));
            }

            if (classId.HasValue)
            {
                queryBuilder.Append(" AND cp.ClassID = @ClassID");
                parameters.Add(new SqlParameter("@ClassID", classId.Value));
            }

            if (!string.IsNullOrWhiteSpace(gender))
            {
                queryBuilder.Append(" AND s.Gender = @Gender");
                parameters.Add(new SqlParameter("@Gender", gender.Trim()));
            }

            if (dateOfBirth.HasValue)
            {
                // Matches exact Date (ignores time if database is strictly DATE)
                queryBuilder.Append(" AND s.DateOfBirth = @DateOfBirth");
                parameters.Add(new SqlParameter("@DateOfBirth", dateOfBirth.Value.Date));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                queryBuilder.Append(" AND s.Status = @Status");
                parameters.Add(new SqlParameter("@Status", status.Trim()));
            }

            // --- PARTIAL MATCHES (LIKE) ---

            if (!string.IsNullOrWhiteSpace(fullName))
            {
                queryBuilder.Append(" AND s.FullName LIKE @FullName");
                parameters.Add(new SqlParameter("@FullName", "%" + fullName.Trim() + "%"));
            }

            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                queryBuilder.Append(" AND s.PhoneNumber LIKE @PhoneNumber");
                parameters.Add(new SqlParameter("@PhoneNumber", "%" + phoneNumber.Trim() + "%"));
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                queryBuilder.Append(" AND s.Email LIKE @Email");
                parameters.Add(new SqlParameter("@Email", "%" + email.Trim() + "%"));
            }

            if (!string.IsNullOrWhiteSpace(address))
            {
                queryBuilder.Append(" AND s.Address LIKE @Address");
                parameters.Add(new SqlParameter("@Address", "%" + address.Trim() + "%"));
            }

            // Execute the dynamic query
            DataTable data = DatabaseHelper.ExecuteQuery(queryBuilder.ToString(), parameters.ToArray());

            // Map the results back to objects
            foreach (DataRow row in data.Rows)
            {
                Student stu = new Student()
                {
                    StudentId = row["StudentID"].ToString() ?? "",
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
            // Omit StudentID from INSERT; the database's DEFAULT constraint handles it automatically
            string query = "INSERT INTO Student (AccountID, FullName, Gender, DateOfBirth, PhoneNumber, Email, Address, FamilyBackground, GuardianName, GuardianPhoneNumber, Status) " +
                           "VALUES (@AccountID, @FullName, @Gender, @DateOfBirth, @PhoneNumber, @Email, @Address, @FamilyBackground, @GuardianName, @GuardianPhoneNumber, @Status)";

            SqlParameter[] parameters = new SqlParameter[] {
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
                new SqlParameter("@StudentID", this.StudentId), // Now passing a string
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
        public static bool DeleteStudent(string studentId) // Parameter changed to string
        {
            string query = "DELETE FROM Student WHERE StudentID = @StudentID";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@StudentID", studentId)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }
    }
}