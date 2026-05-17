using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Student_Management.Helpers
{
    class DatabaseHelper
    {
        /// <summary>
        /// REQUIREMENTS: SQL Server Express LocalDB 2019+
        /// </summary>
        /// // ĐÃ FIX: Bơm thêm "Initial Catalog=StudentManagementDB;" để né lỗi độ dài tên 128 ký tự của LocalDB
        public static string connectionString = @"Server=(localdb)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\Services\StudentManagementDB.mdf;Initial Catalog=StudentManagementDB;Integrated Security=True;TrustServerCertificate=True;";     /// </summary>
        public static DataTable ExecuteQuery(string query, SqlParameter[]? parameters = null)
        {
            DataTable data = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);

                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                adapter.Fill(data);
            }
            return data;
        }
        /// <summary>
        /// Executes a SQL query and returns the number of rows affected (INSERT, DELETE, UPDATE operation).
        /// </summary>
        public static int ExecuteNonQuery(string query, SqlParameter[]? parameters = null)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);

                if (parameters != null)
                {
                    // Replaced the manual string parsing logic
                    command.Parameters.AddRange(parameters);
                }

                return command.ExecuteNonQuery();
            }
        }
    }
}
