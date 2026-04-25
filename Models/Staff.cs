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
    public class Staff
    {
        public int StaffId { get; set; }
        public int AccountId { get; set; }
        public required string FullName { get; set; }
        public string? Gender { get; set; }
        public string? Specialization { get; set; }
        public string? Email { get; set; }
        public DateTime? HireDate { get; set; }
        public string? HometownAddress { get; set; }
        public string? PhoneNumber { get; set; }
        public string? NationalId { get; set; }
        public string? Status { get; set; }

        // READ
        public static List<Staff> GetAllStaff()
        {
            List<Staff> staffList = new List<Staff>();
            string query = "SELECT * FROM Employee";

            DataTable data = DatabaseHelper.ExecuteQuery(query);

            if (data == null)
            {
                return staffList;
            }

            foreach (DataRow row in data.Rows)
            {
                Staff staff = new Staff()
                {
                    StaffId = row["EmployeeID"] != DBNull.Value ? Convert.ToInt32(row["EmployeeID"]) : 0,

                    AccountId = row["AccountID"] != DBNull.Value ? Convert.ToInt32(row["AccountID"]) : 0,

                    FullName = row["FullName"].ToString() ?? "",
                    Gender = row["Gender"] as string,
                    Specialization = row["Specialization"] as string,
                    Email = row["Email"] as string,
                    HireDate = row["HireDate"] == DBNull.Value ? null : Convert.ToDateTime(row["HireDate"]),
                    HometownAddress = row["HometownAddress"] as string,
                    PhoneNumber = row["PhoneNumber"] as string,
                    NationalId = row["NationalID"] as string,
                    Status = row["Status"] as string
                };
                staffList.Add(staff);
            }
            return staffList;
        }

        // CREATE
        public bool AddStaff()
        {
            string query = "INSERT INTO Employee (EmployeeID, AccountID, FullName, Gender, Specialization, Email, HireDate, HometownAddress, PhoneNumber, NationalID, Status) " +
                           "VALUES (@EmployeeID, @AccountID, @FullName, @Gender, @Specialization, @Email, @HireDate, @HometownAddress, @PhoneNumber, @NationalID, @Status)";

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
                new SqlParameter("@Status", this.Status ?? (object)DBNull.Value)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // UPDATE
        public bool UpdateStaff()
        {
            string query = "UPDATE Employee SET AccountID = @AccountID, FullName = @FullName, Gender = @Gender, " +
                           "Specialization = @Specialization, Email = @Email, HireDate = @HireDate, " +
                           "HometownAddress = @HometownAddress, PhoneNumber = @PhoneNumber, NationalID = @NationalID, Status = @Status " +
                           "WHERE EmployeeID = @EmployeeID";

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
                new SqlParameter("@Status", this.Status ?? (object)DBNull.Value)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // DELETE
        public static bool DeleteStaff(int staffId)
        {
            string query = "DELETE FROM Employee WHERE EmployeeID = @EmployeeID";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@EmployeeID", staffId)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        //Hàm lấy staff chưa được phân công chủ nhiệm lớp nào (Staff nào có account role GVCN ấy)
        public static List<Staff> GetAvailableTeachers()
        {
            List<Staff> staffList = new List<Staff>();

            //JOIN với Account để check RoleID = 5 (GVCN)
            string query = @"
            SELECT e.* FROM Employee e
            INNER JOIN Account a ON e.AccountID = a.AccountID
            WHERE a.RoleID = 5 
            AND e.EmployeeID NOT IN (
            SELECT HomeroomTeacherID 
            FROM Class 
            WHERE HomeroomTeacherID IS NOT NULL
            )";

            DataTable data = DatabaseHelper.ExecuteQuery(query);

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
    }
}
