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
        public required string StudentId { get; set; }
        public int? CreatedByTeacherId { get; set; }
        public int? NewClassId { get; set; }
        public required string RequestType { get; set; }
        public string? Reason { get; set; }
        public string? FeedbackNote { get; set; }
        public int StatusId { get; set; } = 1;
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
                    StudentId = row["StudentID"].ToString() ?? "",
                    CreatedByTeacherId = row["CreatedByTeacherID"] == DBNull.Value ? null : Convert.ToInt32(row["CreatedByTeacherID"]),
                    NewClassId = row["NewClassID"] == DBNull.Value ? null : Convert.ToInt32(row["NewClassID"]),
                    RequestType = row["RequestType"].ToString() ?? "",
                    Reason = row["Reason"] as string,
                    FeedbackNote = row["FeedbackNote"] as string,
                    StatusId = Convert.ToInt32(row["StatusID"]),
                    RespondedAt = row["RespondedAt"] == DBNull.Value ? null : Convert.ToDateTime(row["RespondedAt"])
                };
                requests.Add(req);
            }
            return requests;
        }

        // CREATE
        public bool AddRequest()
        {
            string query = "INSERT INTO Application (StudentID, CreatedByTeacherID, NewClassID, RequestType, Reason, FeedbackNote, StatusID, RespondedAt) " +
                           "VALUES (@StudentID, @CreatedByTeacherID, @NewClassID, @RequestType, @Reason, @FeedbackNote, @StatusID, @RespondedAt)";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@StudentID", this.StudentId),
                new SqlParameter("@CreatedByTeacherID", this.CreatedByTeacherId ?? (object)DBNull.Value),
                new SqlParameter("@NewClassID", this.NewClassId ?? (object)DBNull.Value),
                new SqlParameter("@RequestType", this.RequestType),
                new SqlParameter("@Reason", this.Reason ?? (object)DBNull.Value),
                new SqlParameter("@FeedbackNote", this.FeedbackNote ?? (object)DBNull.Value),
                new SqlParameter("@StatusID", this.StatusId),
                new SqlParameter("@RespondedAt", this.RespondedAt ?? (object)DBNull.Value)
            };

            return DatabaseHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // UPDATE
        public bool UpdateRequest()
        {
            string query = "UPDATE Application SET StudentID = @StudentID, CreatedByTeacherID = @CreatedByTeacherID, " +
                           "NewClassID = @NewClassID, RequestType = @RequestType, Reason = @Reason, " +
                           "FeedbackNote = @FeedbackNote, StatusID = @StatusID, RespondedAt = @RespondedAt " +
                           "WHERE RequestID = @RequestID";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@RequestID", this.RequestId),
                new SqlParameter("@StudentID", this.StudentId),
                new SqlParameter("@CreatedByTeacherID", this.CreatedByTeacherId ?? (object)DBNull.Value),
                new SqlParameter("@NewClassID", this.NewClassId ?? (object)DBNull.Value),
                new SqlParameter("@RequestType", this.RequestType),
                new SqlParameter("@Reason", this.Reason ?? (object)DBNull.Value),
                new SqlParameter("@FeedbackNote", this.FeedbackNote ?? (object)DBNull.Value),
                new SqlParameter("@StatusID", this.StatusId),
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
