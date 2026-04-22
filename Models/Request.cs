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
    //Application table
    public class Request
    {
        public int RequestId { get; set; }
        public int StudentId { get; set; }
        public int? CreatedByTeacherId { get; set; }
        public int? NewClassId { get; set; }
        public required string RequestType { get; set; }
        public string? Reason { get; set; }
        public string? FeedbackNote { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime? RespondedAt { get; set; }

        // READ
        public static List<Request> GetAllRequests()
        {
            List<Request> requests = new List<Request>();
            string query = "SELECT * FROM Application";

            DataTable data = DatabaseHelper.ExecuteQuery(query);

            foreach (DataRow row in data.Rows)
            {
                Request req = new Request()
                {
                    RequestId = Convert.ToInt32(row["RequestID"]),
                    StudentId = Convert.ToInt32(row["StudentID"]),
                    CreatedByTeacherId = row["CreatedByTeacherID"] == DBNull.Value ? null : Convert.ToInt32(row["CreatedByTeacherID"]),
                    NewClassId = row["NewClassID"] == DBNull.Value ? null : Convert.ToInt32(row["NewClassID"]),
                    RequestType = row["RequestType"].ToString() ?? "",
                    Reason = row["Reason"] as string,
                    FeedbackNote = row["FeedbackNote"] as string,
                    Status = row["Status"].ToString() ?? "Pending",
                    RespondedAt = row["RespondedAt"] == DBNull.Value ? null : Convert.ToDateTime(row["RespondedAt"])
                };
                requests.Add(req);
            }
            return requests;
        }

        // CREATE
        public bool AddRequest()
        {
            string query = "INSERT INTO Application (RequestID, StudentID, CreatedByTeacherID, NewClassID, RequestType, Reason, FeedbackNote, Status, RespondedAt) " +
                           "VALUES (@RequestID, @StudentID, @CreatedByTeacherID, @NewClassID, @RequestType, @Reason, @FeedbackNote, @Status, @RespondedAt)";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@RequestID", this.RequestId),
                new SqlParameter("@StudentID", this.StudentId),
                new SqlParameter("@CreatedByTeacherID", this.CreatedByTeacherId ?? (object)DBNull.Value),
                new SqlParameter("@NewClassID", this.NewClassId ?? (object)DBNull.Value),
                new SqlParameter("@RequestType", this.RequestType),
                new SqlParameter("@Reason", this.Reason ?? (object)DBNull.Value),
                new SqlParameter("@FeedbackNote", this.FeedbackNote ?? (object)DBNull.Value),
                new SqlParameter("@Status", this.Status),
                new SqlParameter("@RespondedAt", this.RespondedAt ?? (object)DBNull.Value)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // UPDATE
        public bool UpdateRequest()
        {
            string query = "UPDATE Application SET StudentID = @StudentID, CreatedByTeacherID = @CreatedByTeacherID, " +
                           "NewClassID = @NewClassID, RequestType = @RequestType, Reason = @Reason, " +
                           "FeedbackNote = @FeedbackNote, Status = @Status, RespondedAt = @RespondedAt " +
                           "WHERE RequestID = @RequestID";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@RequestID", this.RequestId),
                new SqlParameter("@StudentID", this.StudentId),
                new SqlParameter("@CreatedByTeacherID", this.CreatedByTeacherId ?? (object)DBNull.Value),
                new SqlParameter("@NewClassID", this.NewClassId ?? (object)DBNull.Value),
                new SqlParameter("@RequestType", this.RequestType),
                new SqlParameter("@Reason", this.Reason ?? (object)DBNull.Value),
                new SqlParameter("@FeedbackNote", this.FeedbackNote ?? (object)DBNull.Value),
                new SqlParameter("@Status", this.Status),
                new SqlParameter("@RespondedAt", this.RespondedAt ?? (object)DBNull.Value)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // DELETE
        public static bool DeleteRequest(int requestId)
        {
            string query = "DELETE FROM Application WHERE RequestID = @RequestID";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@RequestID", requestId)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }
    }
}
