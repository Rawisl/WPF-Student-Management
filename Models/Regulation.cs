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
    public class Regulation
    {
        public int RegulationId { get; set; }
        public required string RegulationName { get; set; }
        public decimal Value { get; set; }

        // READ
        public static List<Regulation> GetAllRegulations()
        {
            List<Regulation> regulations = new List<Regulation>();
            string query = "SELECT * FROM Parameter";

            DataTable data = DatabaseHelper.ExecuteQuery(query);

            foreach (DataRow row in data.Rows)
            {
                Regulation reg = new Regulation()
                {
                    RegulationId = Convert.ToInt32(row["ParameterID"]),
                    RegulationName = row["ParameterName"].ToString() ?? "",
                    Value = Convert.ToDecimal(row["Value"])
                };
                regulations.Add(reg);
            }
            return regulations;
        }

        // CREATE
        public bool AddRegulation()
        {
            string query = "INSERT INTO Parameter (ParameterName, Value) " +
                           "VALUES (@ParameterName, @Value)";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@ParameterName", this.RegulationName),
                new SqlParameter("@Value", this.Value)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // UPDATE
        public bool UpdateRegulation()
        {
            string query = "UPDATE Parameter SET ParameterName = @ParameterName, Value = @Value " +
                           "WHERE ParameterID = @ParameterID";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@ParameterID", this.RegulationId),
                new SqlParameter("@ParameterName", this.RegulationName),
                new SqlParameter("@Value", this.Value)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // DELETE
        public static bool DeleteRegulation(int regulationId)
        {
            string query = "DELETE FROM Parameter WHERE ParameterID = @ParameterID";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@ParameterID", regulationId)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }
    }
}
