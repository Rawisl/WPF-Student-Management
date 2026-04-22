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
            string query = "INSERT INTO Account (AccountID, RoleID, Username, PasswordHash, IsRequiredChangePassword, IsActive) " +
                           "VALUES (@AccountID, @RoleID, @Username, @PasswordHash, @IsRequiredChangePassword, @IsActive)";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@AccountID", this.AccountId),
                new SqlParameter("@RoleID", this.RoleId),
                new SqlParameter("@Username", this.Username),
                new SqlParameter("@PasswordHash", this.PasswordHash),
                new SqlParameter("@IsRequiredChangePassword", this.IsRequiredChangePassword),
                new SqlParameter("@IsActive", this.IsActive)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
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
                new SqlParameter("@PasswordHash", this.PasswordHash),
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
    }
}