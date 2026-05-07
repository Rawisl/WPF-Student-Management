using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.Models
{
    // BẮT BUỘC KẾ THỪA INotifyPropertyChanged ĐỂ HỖ TRỢ XÁM/SÁNG NÚT LƯU
    public class Staff : INotifyPropertyChanged
    {
        public int StaffId { get; set; }
        public int AccountId { get; set; }

        public int RoleId { get; set; }

        // --- CÁC THUỘC TÍNH CÓ KIỂM TRA ĐIỀU KIỆN LƯU ---
        private string _fullName = "";
        public required string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(); }
        }

        private string? _email;
        public string? Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        private string? _phoneNumber;
        public string? PhoneNumber
        {
            get => _phoneNumber;
            set { _phoneNumber = value; OnPropertyChanged(); }
        }

        private string? _nationalId;
        public string? NationalId
        {
            get => _nationalId;
            set { _nationalId = value; OnPropertyChanged(); }
        }
        // ------------------------------------------------

        public string? Gender { get; set; }
        public string? Specialization { get; set; }
        public DateTime? HireDate { get; set; }
        public string? HometownAddress { get; set; }
        public string? Status { get; set; }

        // --- CÀI ĐẶT SỰ KIỆN INotifyPropertyChanged ---
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static List<Staff> GetAllStaff()
        {
            List<Staff> staffList = new List<Staff>();
            string query = @"
            SELECT e.*, a.RoleID
            FROM Employee e 
            JOIN Account a ON e.AccountID = a.AccountID";

            DataTable data = DatabaseHelper.ExecuteQuery(query);
            if (data == null) return staffList;

            foreach (DataRow row in data.Rows)
            {
                staffList.Add(new Staff()
                {
                    StaffId = Convert.ToInt32(row["EmployeeID"]),
                    AccountId = Convert.ToInt32(row["AccountID"]),
                    RoleId = Convert.ToInt32(row["RoleID"]),
                    FullName = row["FullName"].ToString() ?? "",
                    Gender = row["Gender"] as string,
                    Specialization = row["Specialization"] as string,
                    Email = row["Email"] as string,
                    HireDate = row["HireDate"] == DBNull.Value ? null : Convert.ToDateTime(row["HireDate"]),
                    HometownAddress = row["HometownAddress"] as string,
                    PhoneNumber = row["PhoneNumber"] as string,
                    NationalId = row["NationalID"] as string,
                    Status = row["Status"] as string
                });
            }
            return staffList;
        }

        public bool AddStaff()
        {
            string query = "INSERT INTO Employee (AccountID, FullName, Gender, Specialization, Email, HireDate, HometownAddress, PhoneNumber, NationalID, Status) " +
                           "VALUES (@AccountID, @FullName, @Gender, @Specialization, @Email, @HireDate, @HometownAddress, @PhoneNumber, @NationalID, @Status)";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@AccountID", this.AccountId),
                new SqlParameter("@FullName", this.FullName),
                new SqlParameter("@Gender", this.Gender ?? (object)DBNull.Value),
                new SqlParameter("@Specialization", this.Specialization ?? (object)DBNull.Value),
                new SqlParameter("@Email", this.Email ?? (object)DBNull.Value),
                new SqlParameter("@HireDate", this.HireDate ?? (object)DBNull.Value),
                new SqlParameter("@HometownAddress", this.HometownAddress ?? (object)DBNull.Value),
                new SqlParameter("@PhoneNumber", this.PhoneNumber ?? (object)DBNull.Value),
                new SqlParameter("@NationalID", this.NationalId ?? (object)DBNull.Value),
                new SqlParameter("@Status", this.Status ?? (object)DBNull.Value)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        public bool UpdateStaff()
        {
            string query = @"
            BEGIN TRAN;
            BEGIN TRY
                UPDATE Employee SET 
                    FullName = @FullName, Gender = @Gender, Specialization = @Specialization, 
                    Email = @Email, HireDate = @HireDate, HometownAddress = @HometownAddress, 
                    PhoneNumber = @PhoneNumber, NationalID = @NationalID, Status = @Status 
                WHERE EmployeeID = @EmployeeID;

                UPDATE Account SET RoleID = @RoleID WHERE AccountID = @AccountID;

                COMMIT TRAN;
            END TRY
            BEGIN CATCH
                ROLLBACK TRAN;
                THROW;
            END CATCH";

            SqlParameter[] parameters = new SqlParameter[] {
            new SqlParameter("@EmployeeID", this.StaffId),
            new SqlParameter("@AccountID", this.AccountId),
            new SqlParameter("@FullName", this.FullName),
            new SqlParameter("@Gender", this.Gender ?? (object)DBNull.Value),
            new SqlParameter("@Specialization", this.Specialization ?? (object)DBNull.Value),
            new SqlParameter("@Email", this.Email ?? (object)DBNull.Value),
            new SqlParameter("@HireDate", this.HireDate ?? (object)DBNull.Value),
            new SqlParameter("@HometownAddress", this.HometownAddress ?? (object)DBNull.Value),
            new SqlParameter("@PhoneNumber", this.PhoneNumber ?? (object)DBNull.Value),
            new SqlParameter("@NationalID", this.NationalId ?? (object)DBNull.Value),
            new SqlParameter("@Status", this.Status ?? (object)DBNull.Value),
            new SqlParameter("@RoleID", this.RoleId)
        };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        public static bool DeleteStaff(int staffId)
        {
            string query = "DELETE FROM Employee WHERE EmployeeID = @EmployeeID";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@EmployeeID", staffId)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // SỬA LẠI: Thêm tham số academicYear để lọc theo năm học
        public static List<Staff> GetAvailableTeachers(string academicYear)
        {
            List<Staff> staffList = new List<Staff>();

            string query = @"
            SELECT e.* FROM Employee e
            INNER JOIN Account a ON e.AccountID = a.AccountID
            WHERE a.RoleID = 5 
            AND e.EmployeeID NOT IN (
            SELECT HomeroomTeacherID 
            FROM Class 
            WHERE HomeroomTeacherID IS NOT NULL 
            AND AcademicYear = @AcademicYear -- [THÊM ĐIỀU KIỆN NÀY]
            )";

            SqlParameter[] parameters = new SqlParameter[] {
            new SqlParameter("@AcademicYear", academicYear)
        };

            DataTable data = DatabaseHelper.ExecuteQuery(query, parameters);

            foreach (DataRow row in data.Rows)
            {
                staffList.Add(new Staff()
                {
                    StaffId = Convert.ToInt32(row["EmployeeID"]),
                    AccountId = Convert.ToInt32(row["AccountID"]),
                    FullName = row["FullName"].ToString() ?? "",
                    Gender = row["Gender"] as string,
                    Specialization = row["Specialization"] as string,
                    Email = row["Email"] as string,
                    PhoneNumber = row["PhoneNumber"] as string,
                    HometownAddress = row["HometownAddress"] as string,
                    NationalId = row["NationalID"] as string,
                    Status = row["Status"] as string,
                    HireDate = row["HireDate"] == DBNull.Value ? null : Convert.ToDateTime(row["HireDate"])
                });
            }
            return staffList;
        }

        public (string Username, string Password)? ReceiveNewStaff()
        {
            string username = GenerateStaffUsername(this.FullName);
            string rawPassword = GenerateStaffPassword(this.FullName, this.PhoneNumber);
            string hashedPassword = PasswordHasher.HashPassword(rawPassword);

            string query = @"
        BEGIN TRAN;
        BEGIN TRY
            INSERT INTO Account (RoleID, Username, PasswordHash, IsRequiredChangePassword, IsActive)
            VALUES (4, @Username, @PasswordHash, 1, 1);
            
            DECLARE @NewAccID INT = SCOPE_IDENTITY();

            INSERT INTO Employee (AccountID, FullName, Gender, Specialization, Email, HireDate, HometownAddress, PhoneNumber, NationalID, Status)
            VALUES (@NewAccID, @FullName, @Gender, @Specialization, @Email, @HireDate, @HometownAddress, @PhoneNumber, @NationalID, @Status);

            COMMIT TRAN;
            SELECT @Username AS GeneratedUsername;
        END TRY
        BEGIN CATCH
            ROLLBACK TRAN;
            THROW;
        END CATCH
    ";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@Username", username),
                new SqlParameter("@PasswordHash", hashedPassword),
                new SqlParameter("@FullName", this.FullName),
                new SqlParameter("@Gender", this.Gender ?? (object)DBNull.Value),
                new SqlParameter("@Specialization", this.Specialization ?? (object)DBNull.Value),
                new SqlParameter("@Email", this.Email ?? (object)DBNull.Value),
                new SqlParameter("@HireDate", this.HireDate ?? (object)DBNull.Value),
                new SqlParameter("@HometownAddress", this.HometownAddress ?? (object)DBNull.Value),
                new SqlParameter("@PhoneNumber", this.PhoneNumber ?? (object)DBNull.Value),
                new SqlParameter("@NationalID", this.NationalId ?? (object)DBNull.Value),
                new SqlParameter("@Status", "Active")
            };

            DataTable result = DatabaseHelper.ExecuteQuery(query, parameters);

            if (result.Rows.Count > 0)
            {
                string dbUsername = result.Rows[0]["GeneratedUsername"].ToString();
                return (dbUsername, rawPassword);
            }
            return null;
        }

        // Hàm phụ trợ: Chuyển tiếng Việt có dấu thành không dấu và tạo Username
        private string GenerateStaffUsername(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "gv_unknown";

            // Xóa dấu tiếng Việt (Sử dụng TextHelper bạn đã có hoặc hàm tương đương)
            string unsignedName = TextHelper.RemoveSignForVietnameseString(fullName).ToLower();
            string[] parts = unsignedName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 1) return "gv_user";

            string firstName = parts[parts.Length - 1]; // Tên chính (can)
            string initials = ""; // Viết tắt họ và tên đệm (pv)

            for (int i = 0; i < parts.Length - 1; i++)
            {
                initials += parts[i][0];
            }

            return "gv_" + firstName + initials;
        }

        // Hàm phụ trợ: Tạo Password mặc định
        private string GenerateStaffPassword(string fullName, string? phone)
        {
            string unsignedName = TextHelper.RemoveSignForVietnameseString(fullName).ToLower(); 
            string[] parts = unsignedName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string firstName = parts[parts.Length - 1];

            string lastFourDigits = (phone != null && phone.Length >= 4)
                ? phone.Substring(phone.Length - 4)
                : "1234";

            return firstName + lastFourDigits;
        }
    }
}