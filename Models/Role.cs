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
    public class Role
    {
        public int RoleId { get; set; }
        public required string RoleName { get; set; }

        // READ
        public static List<Role> GetAllRoles()
        {
            List<Role> roles = new List<Role>();
            string query = "SELECT * FROM Role";

            DataTable data = DatabaseHelper.ExecuteQuery(query);

            foreach (DataRow row in data.Rows)
            {
                Role role = new Role()
                {
                    RoleId = Convert.ToInt32(row["RoleID"]),
                    RoleName = row["RoleName"].ToString() ?? ""
                };
                roles.Add(role);
            }
            return roles;
        }

        // CREATE
        public bool AddRole()
        {
            string query = "INSERT INTO Role (RoleName) VALUES (@RoleName)";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@RoleName", this.RoleName)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // UPDATE
        public bool UpdateRole()
        {
            string query = "UPDATE Role SET RoleName = @RoleName WHERE RoleID = @RoleID";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@RoleID", this.RoleId),
                new SqlParameter("@RoleName", this.RoleName)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // DELETE
        public static bool DeleteRole(int roleId)
        {
            string query = "DELETE FROM Role WHERE RoleID = @RoleID";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@RoleID", roleId)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }
    }
}
