using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.ViewModels
{
    // Model nhỏ dùng riêng cho DataGrid màn này
    public class PendingRequestItem
    {
        public int STT { get; set; }
        public int RequestId { get; set; }
        public string StudentId { get; set; }
        public string FullName { get; set; }
        public string RequestType { get; set; }
        public int? CurrentClassId { get; set; }
        public string CurrentClassName { get; set; }
        public int? TargetClassId { get; set; }
        public string TargetClassName { get; set; }
        public string Reason { get; set; }

        // Format UI
        public string RequestTypeDisplay => RequestType == "ClassTransfer" ? "Chuyển lớp" : "Thôi học";
        public string RequestTypeBgColor => RequestType == "ClassTransfer" ? "#E3F2FD" : "#FFEBEE";
        public string RequestTypeTextColor => RequestType == "ClassTransfer" ? "#1565C0" : "#C62828";
    }

    public partial class EnrollmentChangeExecutionViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<PendingRequestItem> _pendingRequests = new();

        // Biến phục vụ Popup Trả đơn
        private PendingRequestItem _processingItem;
        [ObservableProperty] private string _rejectReason;

        public EnrollmentChangeExecutionViewModel()
        {
            LoadPendingRequests();
        }

        private void LoadPendingRequests()
        {
            PendingRequests.Clear();
            try
            {
                // Join bảng Application, Student và 2 lần bảng Class (để lấy Lớp cũ và Lớp mới)
                string query = @"
                    SELECT 
                        a.RequestID, a.StudentID, s.FullName, a.RequestType, a.Reason,
                        c_old.ClassID AS CurrentClassID, c_old.ClassName AS CurrentClassName,
                        a.NewClassID, c_new.ClassName AS TargetClassName
                    FROM Application a
                    JOIN Student s ON a.StudentID = s.StudentID
                    LEFT JOIN ClassPlacement cp ON s.StudentID = cp.StudentID
                    LEFT JOIN Class c_old ON cp.ClassID = c_old.ClassID
                    LEFT JOIN Class c_new ON a.NewClassID = c_new.ClassID
                    WHERE a.Status = 'Pending'
                    ORDER BY a.RequestID ASC";

                DataTable dt = DatabaseHelper.ExecuteQuery(query);

                int stt = 1;
                foreach (DataRow row in dt.Rows)
                {
                    PendingRequests.Add(new PendingRequestItem
                    {
                        STT = stt++,
                        RequestId = Convert.ToInt32(row["RequestID"]),
                        StudentId = row["StudentID"].ToString(),
                        FullName = row["FullName"].ToString(),
                        RequestType = row["RequestType"].ToString(),
                        CurrentClassId = row["CurrentClassID"] != DBNull.Value ? Convert.ToInt32(row["CurrentClassID"]) : null,
                        CurrentClassName = row["CurrentClassName"].ToString() ?? "Không rõ",
                        TargetClassId = row["NewClassID"] != DBNull.Value ? Convert.ToInt32(row["NewClassID"]) : null,
                        TargetClassName = row["TargetClassName"].ToString() ?? "-",
                        Reason = row["Reason"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi tải danh sách chờ: " + ex.Message);
            }
        }

        // --- 1. THỰC THI (EXECUTE) ---
        [RelayCommand]
        private void ExecuteRequest(PendingRequestItem item)
        {
            if (item == null) return;

            bool confirm = NotificationHelper.ShowConfirm($"Bạn có chắc chắn muốn thực thi đơn {item.RequestTypeDisplay} của học sinh {item.FullName}?");
            if (!confirm) return;

            // Bọc bằng SqlTransaction để đảm bảo toàn vẹn dữ liệu
            using (SqlConnection conn = new SqlConnection(DatabaseHelper.connectionString))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        if (item.RequestType == "DropOut")
                        {
                            // A. XỬ LÝ THÔI HỌC (Soft Delete)
                            SqlCommand cmdHideStudent = new SqlCommand("UPDATE Student SET Status = 'Inactive' WHERE StudentID = @ID", conn, transaction);
                            cmdHideStudent.Parameters.AddWithValue("@ID", item.StudentId);
                            cmdHideStudent.ExecuteNonQuery();

                            SqlCommand cmdRemoveClass = new SqlCommand("DELETE FROM ClassPlacement WHERE StudentID = @ID", conn, transaction);
                            cmdRemoveClass.Parameters.AddWithValue("@ID", item.StudentId);
                            cmdRemoveClass.ExecuteNonQuery();
                        }
                        else if (item.RequestType == "ClassTransfer")
                        {
                            // B. XỬ LÝ CHUYỂN LỚP
                            // Check max size trước
                            SqlCommand cmdGetMaxSize = new SqlCommand("SELECT CAST(Value AS INT) FROM Parameter WHERE ParameterName = 'MaxClassSize'", conn, transaction);
                            int maxClassSize = (int)(cmdGetMaxSize.ExecuteScalar() ?? 40);

                            SqlCommand cmdGetCurrentSize = new SqlCommand("SELECT COUNT(*) FROM ClassPlacement WHERE ClassID = @NewClassID", conn, transaction);
                            cmdGetCurrentSize.Parameters.AddWithValue("@NewClassID", item.TargetClassId);
                            int currentSize = (int)cmdGetCurrentSize.ExecuteScalar();

                            if (currentSize + 1 > maxClassSize)
                            {
                                transaction.Rollback();
                                NotificationHelper.ShowError("Lớp đích đã đủ sĩ số tối đa, không thể chuyển thêm! Vui lòng chọn Trả đơn.");
                                return;
                            }

                            // Thỏa điều kiện -> Update ClassPlacement
                            SqlCommand cmdUpdateClass = new SqlCommand("UPDATE ClassPlacement SET ClassID = @NewClassID WHERE StudentID = @ID", conn, transaction);
                            cmdUpdateClass.Parameters.AddWithValue("@NewClassID", item.TargetClassId);
                            cmdUpdateClass.Parameters.AddWithValue("@ID", item.StudentId);
                            cmdUpdateClass.ExecuteNonQuery();
                        }

                        // C. UPDATE TRẠNG THÁI ĐƠN VÀO BẢNG APPLICATION CHO CẢ 2 TRƯỜNG HỢP
                        SqlCommand cmdUpdateApp = new SqlCommand("UPDATE Application SET Status = 'Executed', RespondedAt = GETDATE() WHERE RequestID = @ReqID", conn, transaction);
                        cmdUpdateApp.Parameters.AddWithValue("@ReqID", item.RequestId);
                        cmdUpdateApp.ExecuteNonQuery();

                        transaction.Commit();
                        NotificationHelper.ShowSuccess("Thực thi yêu cầu thành công!");
                        LoadPendingRequests(); // Refresh lại lưới
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        NotificationHelper.ShowError("Lỗi hệ thống khi thực thi:\n" + ex.Message);
                    }
                }
            }
        }

        // --- 2. TRẢ ĐƠN (REJECT) ---
        [RelayCommand]
        private async Task OpenRejectDialog(PendingRequestItem item)
        {
            if (item == null) return;

            _processingItem = item;
            RejectReason = string.Empty; // Xóa trắng textbox

            // Khởi tạo cái View Popup vừa tạo ở trên, truyền chính ViewModel này vào làm DataContext
            var rejectDialogView = new WPF_Student_Management.Components.RejectReasonDialog
            {
                DataContext = this
            };

            // Mở Dialog bằng cách nhét cái View đó vào thay vì để null
            await MaterialDesignThemes.Wpf.DialogHost.Show(rejectDialogView, "ExecutionDialogHost");
        }

        [RelayCommand]
        private void CancelReject()
        {
            _processingItem = null;
            MaterialDesignThemes.Wpf.DialogHost.Close("ExecutionDialogHost");
        }

        [RelayCommand]
        private void ConfirmReject()
        {
            if (string.IsNullOrWhiteSpace(RejectReason))
            {
                NotificationHelper.ShowWarning("Vui lòng nhập lý do trả đơn!");
                return;
            }

            try
            {
                string query = "UPDATE Application SET Status = 'Rejected', FeedbackNote = @Reason, RespondedAt = GETDATE() WHERE RequestID = @ReqID";
                DatabaseHelper.ExecuteNonQuery(query, new[] {
                    new SqlParameter("@Reason", RejectReason.Trim()),
                    new SqlParameter("@ReqID", _processingItem.RequestId)
                });

                NotificationHelper.ShowSuccess("Đã trả đơn về cho Giáo viên chủ nhiệm!");
                MaterialDesignThemes.Wpf.DialogHost.Close("ExecutionDialogHost");
                LoadPendingRequests(); // Refresh lại lưới
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi khi trả đơn: " + ex.Message);
            }
        }
    }
}