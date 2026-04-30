using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.Models
{
    public class Account
    {
        public int AccountId { get; set; }
        public int RoleId { get; set; }
        public required string Username { get; set; }
        public required string PasswordHash { get; set; }
        public bool IsRequiredChangePassword { get; set; } = true;
        public bool IsActive { get; set; } = true;

        // READ
        public static List<Account> GetAllAccounts()
        {
            List<Account> accounts = new List<Account>();
            string query = "SELECT * FROM Account";

            DataTable data = DatabaseHelper.ExecuteQuery(query);

            foreach (DataRow row in data.Rows)
            {
                Account acc = new Account()
                {
                    AccountId = Convert.ToInt32(row["AccountID"]),
                    RoleId = Convert.ToInt32(row["RoleID"]),
                    Username = row["Username"].ToString() ?? "",
                    PasswordHash = row["PasswordHash"].ToString() ?? "",
                    IsRequiredChangePassword = Convert.ToBoolean(row["IsRequiredChangePassword"]),
                    IsActive = Convert.ToBoolean(row["IsActive"])
                };
                accounts.Add(acc);
            }
            return accounts;
        }

        // CREATE
        public bool AddAccount()
        {
            string query = "INSERT INTO Account (RoleID, Username, PasswordHash, IsRequiredChangePassword, IsActive) " +
                           "VALUES (@RoleID, @Username, @PasswordHash, @IsRequiredChangePassword, @IsActive)";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@RoleID", this.RoleId),
                new SqlParameter("@Username", this.Username),
                // Hash the password inline before saving
                new SqlParameter("@PasswordHash", PasswordHasher.HashPassword(this.PasswordHash)),
                new SqlParameter("@IsRequiredChangePassword", this.IsRequiredChangePassword),
                new SqlParameter("@IsActive", this.IsActive)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        public int AddAccountAndGetId()
        {
            string query = "INSERT INTO Account (RoleID, Username, PasswordHash, IsRequiredChangePassword, IsActive) " +
                           "OUTPUT INSERTED.AccountID " +
                           "VALUES (@RoleID, @Username, @PasswordHash, @IsRequiredChangePassword, @IsActive)";

            SqlParameter[] parameters = new SqlParameter[] {
        new SqlParameter("@RoleID", this.RoleId),
        new SqlParameter("@Username", this.Username),
        new SqlParameter("@PasswordHash", PasswordHasher.HashPassword(this.PasswordHash)),
        new SqlParameter("@IsRequiredChangePassword", this.IsRequiredChangePassword),
        new SqlParameter("@IsActive", this.IsActive)
    };

            DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);
            if (dt != null && dt.Rows.Count > 0)
            {
                return Convert.ToInt32(dt.Rows[0]["AccountID"]);
            }
            return 0;
        }

        // UPDATE
        public bool UpdateAccount()
        {
            string query = "UPDATE Account SET RoleID = @RoleID, Username = @Username, PasswordHash = @PasswordHash, " +
                           "IsRequiredChangePassword = @IsRequiredChangePassword, IsActive = @IsActive " +
                           "WHERE AccountID = @AccountID";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@AccountID", this.AccountId),
                new SqlParameter("@RoleID", this.RoleId),
                new SqlParameter("@Username", this.Username),
                // Hash the password inline before saving
                new SqlParameter("@PasswordHash", PasswordHasher.HashPassword(this.PasswordHash)),
                new SqlParameter("@IsRequiredChangePassword", this.IsRequiredChangePassword),
                new SqlParameter("@IsActive", this.IsActive)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // DELETE
        public static bool DeleteAccount(int accountId)
        {
            string query = "DELETE FROM Account WHERE AccountID = @AccountID";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@AccountID", accountId)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }
        public static Account? Login(string username, string rawPassword)
        {
            /// Hash the incoming attempt
            string hashedAttempt = PasswordHasher.HashPassword(rawPassword);

            // Search for a matching username AND matching hash in the DB
            string query = "SELECT * FROM Account WHERE Username = @Username AND PasswordHash = @PasswordHash AND IsActive = 1";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@Username", username),
                new SqlParameter("@PasswordHash", hashedAttempt)
            };

            DataTable data = DatabaseHelper.ExecuteQuery(query, parameters);

            if (data.Rows.Count > 0)
            {
                DataRow row = data.Rows[0];
                return new Account()
                {
                    AccountId = Convert.ToInt32(row["AccountID"]),
                    Username = row["Username"].ToString() ?? "",
                    PasswordHash = row["PasswordHash"].ToString() ?? "", // Compare hashes, not raw passwords
                    RoleId = Convert.ToInt32(row["RoleID"])
                };
            }

            return null; // Login failed
        }
    }
}