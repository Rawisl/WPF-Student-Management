using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;

namespace WPF_Student_Management.ViewModels
{
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
        public int StatusId { get; set; }

        // Format UI cho Loại yêu cầu
        public string RequestTypeDisplay => RequestType == "ClassTransfer" ? "Chuyển lớp" : "Thôi học";
        public string RequestTypeTextColor => RequestType == "ClassTransfer" ? "#1565C0" : "#C62828";

        // Format UI cho Trạng thái
        public string StatusDisplay
        {
            get
            {
                if (StatusId == 1) return "Chờ hiệu trưởng duyệt";
                if (StatusId == 2) return "Chờ giáo vụ thực thi";
                if (StatusId == 3) return "Bị từ chối";
                if (StatusId == 4) return "Đã hoàn tất";
                return "Không rõ";
            }
        }
        public string StatusTextColor
        {
            get
            {
                if (StatusId == 1) return "#F57F17"; // Cam
                if (StatusId == 2) return "#0288D1"; // Xanh dương
                if (StatusId == 3) return "#D32F2F"; // Đỏ
                if (StatusId == 4) return "#388E3C"; // Xanh lá
                return "#000000";
            }
        }
    }

    public partial class EnrollmentChangeExecutionViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<PendingRequestItem> _pendingRequests = new();

        [ObservableProperty]
        private string _rejectReason;

        private PendingRequestItem _processingItem;

        // Biến điều khiển UI: Hiện cột Thao tác nếu là HT (Role 3) hoặc GV (Role 6)
        public Visibility ActionColumnVisibility => ((int)CurrentUser.Instance.Role == 3 || (int)CurrentUser.Instance.Role == 6) ? Visibility.Visible : Visibility.Collapsed;

        // Tiêu đề form thay đổi theo Role
        public string FormTitle => (int)CurrentUser.Instance.Role == 5 ? "Lịch sử đơn từ đã lập" : "Xử lý đơn chuyển lớp / thôi học";
        public string FormDescription => (int)CurrentUser.Instance.Role == 5 ? "Theo dõi tiến độ duyệt các lá đơn do bạn tạo ra." : "Danh sách các đơn yêu cầu đang chờ bạn xét duyệt và thực thi.";

        public EnrollmentChangeExecutionViewModel()
        {
            LoadPendingRequests();
        }

        private int GetCurrentEmployeeId()
        {
            string query = "SELECT EmployeeID FROM Employee WHERE AccountID = @AccID";
            var dt = DatabaseHelper.ExecuteQuery(query, new[] { new SqlParameter("@AccID", CurrentUser.Instance.UserId) });
            if (dt.Rows.Count > 0) return Convert.ToInt32(dt.Rows[0][0]);
            return -1;
        }

        private void LoadPendingRequests()
        {
            PendingRequests.Clear();
            try
            {
                int roleId = (int)CurrentUser.Instance.Role;
                int empId = GetCurrentEmployeeId();

                string whereClause = "1=0"; // Mặc định không cho xem gì cả

                if (roleId == 5) // GVCN: Xem tất cả đơn do mình tạo (Mọi trạng thái)
                    whereClause = "a.CreatedByTeacherID = @EmpID";
                else if (roleId == 3) // Hiệu trưởng: Xem các đơn chờ duyệt (Status = 1)
                    whereClause = "a.StatusID = 1";
                else if (roleId == 6) // Giáo vụ: Xem các đơn HT đã duyệt (Status = 2)
                    whereClause = "a.StatusID = 2";

                string query = $@"
                    SELECT 
                        a.RequestID, a.StudentID, s.FullName, a.RequestType, a.Reason, a.StatusID,
                        c_old.ClassID AS CurrentClassID, c_old.ClassName AS CurrentClassName,
                        a.NewClassID, c_new.ClassName AS TargetClassName
                    FROM Application a
                    JOIN Student s ON a.StudentID = s.StudentID
                    LEFT JOIN ClassPlacement cp ON s.StudentID = cp.StudentID AND cp.EffectiveTo IS NULL
                    LEFT JOIN Class c_old ON cp.ClassID = c_old.ClassID
                    LEFT JOIN Class c_new ON a.NewClassID = c_new.ClassID
                    WHERE {whereClause}
                    ORDER BY a.RequestID DESC";

                DataTable dt = DatabaseHelper.ExecuteQuery(query, new[] { new SqlParameter("@EmpID", empId) });

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
                        StatusId = Convert.ToInt32(row["StatusID"]),
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

        // --- 1. THỰC THI HOẶC DUYỆT ĐƠN ---
        [RelayCommand]
        private void ExecuteRequest(PendingRequestItem item)
        {
            if (item == null) return;
            int roleId = (int)CurrentUser.Instance.Role;

            if (roleId == 3) // LÀ HIỆU TRƯỞNG (Chỉ chuyển status sang 2)
            {
                bool confirm = NotificationHelper.ShowConfirm($"Bạn có duyệt đơn {item.RequestTypeDisplay} của học sinh {item.FullName} để chuyển xuống cho Giáo vụ thực thi không?");
                if (!confirm) return;

                string query = "UPDATE Application SET StatusID = 2 WHERE RequestID = @ReqID";
                DatabaseHelper.ExecuteNonQuery(query, new[] { new SqlParameter("@ReqID", item.RequestId) });

                NotificationHelper.ShowSuccess("Đã duyệt! Đơn đã được chuyển cho Giáo vụ.");
                LoadPendingRequests();
            }
            else if (roleId == 6) // LÀ GIÁO VỤ (Thực thi Data và chuyển status sang 4)
            {
                bool confirm = NotificationHelper.ShowConfirm($"Bạn có chắc chắn muốn THỰC THI đơn {item.RequestTypeDisplay} của học sinh {item.FullName}?");
                if (!confirm) return;

                string currentSemester = "Học kỳ 1";
                string currentYear = "2025-2026";

                // Kiểm tra khóa sổ lớp cũ
                if (item.CurrentClassId.HasValue && ClassReport.IsClassReportLocked(item.CurrentClassId.Value, currentSemester, currentYear))
                {
                    NotificationHelper.ShowError($"Lớp cũ ({item.CurrentClassName}) đã khóa sổ!\nKhông thể rút học sinh. Vui lòng chọn Trả đơn.");
                    return;
                }

                // Kiểm tra khóa sổ lớp mới
                if (item.RequestType == "ClassTransfer" && item.TargetClassId.HasValue && ClassReport.IsClassReportLocked(item.TargetClassId.Value, currentSemester, currentYear))
                {
                    NotificationHelper.ShowError($"Lớp đích ({item.TargetClassName}) đã khóa sổ!\nKhông thể thêm học sinh. Vui lòng chọn Trả đơn.");
                    return;
                }

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.connectionString))
                {
                    conn.Open();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            if (item.RequestType == "DropOut")
                            {
                                new SqlCommand($"UPDATE Student SET Status = 'Inactive' WHERE StudentID = '{item.StudentId}'", conn, transaction).ExecuteNonQuery();
                                new SqlCommand($"DELETE FROM ClassPlacement WHERE StudentID = '{item.StudentId}'", conn, transaction).ExecuteNonQuery();
                            }
                            else if (item.RequestType == "ClassTransfer")
                            {
                                int maxClassSize = (int)(new SqlCommand("SELECT CAST(Value AS INT) FROM Parameter WHERE ParameterName = 'MaxClassSize'", conn, transaction).ExecuteScalar() ?? 40);
                                int currentSize = (int)new SqlCommand($"SELECT COUNT(*) FROM ClassPlacement WHERE ClassID = {item.TargetClassId} AND EffectiveTo IS NULL", conn, transaction).ExecuteScalar();

                                if (currentSize + 1 > maxClassSize)
                                {
                                    transaction.Rollback();
                                    NotificationHelper.ShowError("Lớp đích đã đủ sĩ số tối đa! Vui lòng chọn Trả đơn.");
                                    return;
                                }

                                new SqlCommand($"UPDATE ClassPlacement SET ClassID = {item.TargetClassId} WHERE StudentID = '{item.StudentId}'", conn, transaction).ExecuteNonQuery();
                            }

                            new SqlCommand($"UPDATE Application SET StatusID = 4, RespondedAt = GETDATE() WHERE RequestID = {item.RequestId}", conn, transaction).ExecuteNonQuery();

                            transaction.Commit();
                            NotificationHelper.ShowSuccess("Thực thi yêu cầu thành công!");
                            LoadPendingRequests();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            NotificationHelper.ShowError("Lỗi hệ thống khi thực thi:\n" + ex.Message);
                        }
                    }
                }
            }
        }

        // --- 2. TRẢ ĐƠN (REJECT) ---
        [RelayCommand]
        private async void OpenRejectDialog(PendingRequestItem item)
        {
            if (item == null) return;
            _processingItem = item;
            RejectReason = string.Empty;

            var rejectDialogView = new WPF_Student_Management.Components.RejectReasonDialog { DataContext = this };
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
                // Cả HT và Giáo vụ khi từ chối đều chuyển StatusID về 3
                string query = "UPDATE Application SET StatusID = 3, FeedbackNote = @Reason, RespondedAt = GETDATE() WHERE RequestID = @ReqID";
                DatabaseHelper.ExecuteNonQuery(query, new[] {
                    new SqlParameter("@Reason", RejectReason.Trim()),
                    new SqlParameter("@ReqID", _processingItem.RequestId)
                });

                NotificationHelper.ShowSuccess("Đã từ chối và trả đơn về cho Giáo viên chủ nhiệm!");
                MaterialDesignThemes.Wpf.DialogHost.Close("ExecutionDialogHost");
                LoadPendingRequests();
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi khi trả đơn: " + ex.Message);
            }
        }
    }
}