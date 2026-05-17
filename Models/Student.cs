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
        public int STT { get; set; }
        public required string StudentId { get; set; }
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

        // SEARCH FOR STUDENTS
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

            StringBuilder queryBuilder = new StringBuilder(@"
                SELECT DISTINCT s.* FROM Student s
                LEFT JOIN ClassPlacement cp ON s.StudentID = cp.StudentID AND cp.EffectiveTo IS NULL
                WHERE 1=1 "
            );

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
                queryBuilder.Append(" AND s.DateOfBirth = @DateOfBirth");
                parameters.Add(new SqlParameter("@DateOfBirth", dateOfBirth.Value.Date));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                queryBuilder.Append(" AND s.Status = @Status");
                parameters.Add(new SqlParameter("@Status", status.Trim()));
            }

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

            DataTable data = DatabaseHelper.ExecuteQuery(queryBuilder.ToString(), parameters.ToArray());

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

        // --- ĐÃ FIX: LẤY ĐIỂM TỪ BẢNG MỚI StudentAverage ---
        public decimal GetOverallGPA(string semester, string academicYear)
        {
            string query = "SELECT OverallAverage FROM StudentAverage WHERE StudentID = @StudentID AND Semester = @Semester AND AcademicYear = @AcademicYear";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@StudentID", this.StudentId),
                new SqlParameter("@Semester", semester),
                new SqlParameter("@AcademicYear", academicYear)
            };

            DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);

            if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["OverallAverage"] != DBNull.Value)
            {
                return Convert.ToDecimal(dt.Rows[0]["OverallAverage"]);
            }

            return 0;
        }

        // CREATE
        public bool AddStudent()
        {
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
        public static bool DeleteStudent(string studentId)
        {
            string query = "DELETE FROM Student WHERE StudentID = @StudentID";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@StudentID", studentId)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }


        // THÊM MỚI HỌC SINH + TẠO TÀI KHOẢN KÉP
        public string? ReceiveNewStudent()
        {
            string defaultRawPassword = "";
            if (this.DateOfBirth.HasValue && !string.IsNullOrWhiteSpace(this.PhoneNumber) && this.PhoneNumber.Length >= 4)
            {
                string dobStr = this.DateOfBirth.Value.ToString("ddMMyyyy");
                string phoneTail = this.PhoneNumber.Substring(this.PhoneNumber.Length - 4);
                defaultRawPassword = dobStr + phoneTail;
            }
            else
            {
                defaultRawPassword = "Password123";
            }

            string hashedPassword = PasswordHasher.HashPassword(defaultRawPassword);

            string query = @"
                BEGIN TRAN;
                BEGIN TRY
                    INSERT INTO Account (RoleID, Username, PasswordHash, IsRequiredChangePassword, IsActive)
                    VALUES (1, 'TEMP_USERNAME', @PasswordHash, 1, 1);
                    
                    DECLARE @NewAccID INT = SCOPE_IDENTITY();

                    DECLARE @OutputTbl TABLE (ID VARCHAR(10));

                    INSERT INTO Student (AccountID, FullName, Gender, DateOfBirth, PhoneNumber, Email, Address, FamilyBackground, GuardianName, GuardianPhoneNumber, Status)
                    OUTPUT Inserted.StudentID INTO @OutputTbl
                    VALUES (@NewAccID, @FullName, @Gender, @DateOfBirth, @PhoneNumber, @Email, @Address, @FamilyBackground, @GuardianName, @GuardianPhoneNumber, @Status);

                    DECLARE @FinalStudentID VARCHAR(10) = (SELECT TOP 1 ID FROM @OutputTbl);

                    UPDATE Account SET Username = @FinalStudentID WHERE AccountID = @NewAccID;

                    COMMIT TRAN;
                    
                    SELECT @FinalStudentID AS GeneratedID;
                END TRY
                BEGIN CATCH
                    ROLLBACK TRAN;
                    THROW;
                END CATCH
            ";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@PasswordHash", hashedPassword),
                new SqlParameter("@FullName", this.FullName),
                new SqlParameter("@Gender", this.Gender ?? (object)DBNull.Value),
                new SqlParameter("@DateOfBirth", this.DateOfBirth ?? (object)DBNull.Value),
                new SqlParameter("@PhoneNumber", this.PhoneNumber ?? (object)DBNull.Value),
                new SqlParameter("@Email", this.Email ?? (object)DBNull.Value),
                new SqlParameter("@Address", this.Address ?? (object)DBNull.Value),
                new SqlParameter("@FamilyBackground", this.FamilyBackground ?? (object)DBNull.Value),
                new SqlParameter("@GuardianName", this.GuardianName ?? (object)DBNull.Value),
                new SqlParameter("@GuardianPhoneNumber", this.GuardianPhoneNumber ?? (object)DBNull.Value),
                new SqlParameter("@Status", "Active")
            };

            DataTable result = DatabaseHelper.ExecuteQuery(query, parameters);

            if (result.Rows.Count > 0)
            {
                this.StudentId = result.Rows[0]["GeneratedID"].ToString() ?? "";
                return this.StudentId;
            }

            return null;
        }

        // --- ĐÃ FIX: CHỈ TÌM NHỮNG HS CHƯA CÓ PLACEMENT NÀO ĐANG ACTIVE (EffectiveTo IS NULL) ---
        public static List<Student> GetUnassignedStudents(string academicYear)
        {
            List<Student> students = new List<Student>();

            string query = @"
            SELECT * FROM Student 
            WHERE Status = 'Active' AND StudentID NOT IN (
                SELECT StudentID 
                FROM ClassPlacement 
                WHERE AcademicYear = @AcademicYear 
                  AND EffectiveTo IS NULL
            )";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@AcademicYear", academicYear)
            };

            DataTable data = DatabaseHelper.ExecuteQuery(query, parameters);

            foreach (DataRow row in data.Rows)
            {
                Student stu = new Student()
                {
                    StudentId = row["StudentID"].ToString() ?? "",
                    AccountId = Convert.ToInt32(row["AccountID"]),
                    FullName = row["FullName"].ToString() ?? "",
                    Gender = row["Gender"] as string,
                    DateOfBirth = row["DateOfBirth"] == DBNull.Value ? null : Convert.ToDateTime(row["DateOfBirth"]),
                    PhoneNumber = row["PhoneNumber"] as string
                };
                students.Add(stu);
            }

            return students;
        }

        // --- ĐÃ FIX: TRUYỀN THÊM THAM SỐ AcademicYear ĐỂ INSERT VÀO ClassPlacement ---
        public static bool AssignStudentToClass(string studentId, int classId, string academicYear)
        {
            string query = "INSERT INTO ClassPlacement (StudentID, ClassID, AcademicYear) VALUES (@StudentID, @ClassID, @AcademicYear)";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@StudentID", studentId),
                new SqlParameter("@ClassID", classId),
                new SqlParameter("@AcademicYear", academicYear)
            };

            try
            {
                return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi ghi DB xếp lớp: " + ex.Message);
                return false;
            }
        }
    }
}