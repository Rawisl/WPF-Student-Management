using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;
using WPF_Student_Management.Services;

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

        // ĐÃ FIX: Tận dụng PermissionService để ẩn/hiện cột Thao tác (Chỉ Hiệu trưởng và Giáo vụ thấy)
        public Visibility ActionColumnVisibility =>
            (PermissionService.HasFeature(PermissionService.Feature.ApproveRequests) ||
             PermissionService.HasFeature(PermissionService.Feature.ExecuteRequests))
            ? Visibility.Visible : Visibility.Collapsed;

        // ĐÃ FIX: Dùng Enum chuẩn thay vì Magic Number
        public string FormTitle => CurrentUser.Instance.Role == UserRole.GVCN ? "Lịch sử đơn từ đã lập" : "Xử lý đơn chuyển lớp / thôi học";
        public string FormDescription => CurrentUser.Instance.Role == UserRole.GVCN ? "Theo dõi tiến độ duyệt các lá đơn do bạn tạo ra." : "Danh sách các đơn yêu cầu đang chờ bạn xét duyệt và thực thi.";

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
                var currentRole = CurrentUser.Instance.Role;
                int empId = GetCurrentEmployeeId();

                string whereClause = "1=0";

                // ĐÃ FIX: So sánh chuẩn bằng Enum UserRole
                if (currentRole == UserRole.GVCN) // GVCN: Xem đơn do mình tạo
                    whereClause = "a.CreatedByTeacherID = @EmpID";
                else if (currentRole == UserRole.HieuTruong) // Hiệu trưởng: Xem các đơn chờ duyệt (Status = 1)
                    whereClause = "a.StatusID = 1";
                else if (currentRole == UserRole.GiaoVu) // Giáo vụ: Xem các đơn HT đã duyệt (Status = 2)
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

        [RelayCommand]
        private void ExecuteRequest(PendingRequestItem item)
        {
            if (item == null) return;
            var currentRole = CurrentUser.Instance.Role;

            if (currentRole == UserRole.HieuTruong)
            {
                string query = "UPDATE Application SET StatusID = 2 WHERE RequestID = @ReqID AND StatusID = 1";
                int rowsAffected = DatabaseHelper.ExecuteNonQuery(query, new[] { new SqlParameter("@ReqID", item.RequestId) });

                if (rowsAffected == 0)
                {
                    NotificationHelper.ShowWarning("Đơn này đã được xử lý hoặc không còn ở trạng thái chờ duyệt!");
                    LoadPendingRequests();
                    return;
                }

                NotificationHelper.ShowSuccess("Đã duyệt! Đơn đã được chuyển cho Giáo vụ.");
                LoadPendingRequests();
            }
            else if (currentRole == UserRole.GiaoVu)
            {
                string currentSemester = "Học kỳ 1";
                string currentYear = "2025-2026";

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.connectionString))
                {
                    conn.Open();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            //Check lớp nguồn
                            if (item.CurrentClassId.HasValue &&
                                ClassReport.IsClassReportLocked(item.CurrentClassId.Value, currentSemester, currentYear))
                            {
                                transaction.Rollback();
                                NotificationHelper.ShowError($"Lớp cũ ({item.CurrentClassName}) đã khóa sổ! Không thể thực thi.");
                                return;
                            }

                            if (item.RequestType == "ClassTransfer" && item.TargetClassId.HasValue)
                            {
                                string targetLockQuery = @"
                            SELECT ISNULL(IsLocked, 0) 
                            FROM ClassReport 
                            WHERE ClassID = @ClassID 
                              AND Semester = @Semester 
                              AND AcademicYear = @AcademicYear";

                                DataTable dtTargetLock = DatabaseHelper.ExecuteQuery(targetLockQuery, new[] {
                            new SqlParameter("@ClassID", item.TargetClassId.Value),
                            new SqlParameter("@Semester", currentSemester),
                            new SqlParameter("@AcademicYear", currentYear)
                        });

                                bool isTargetLocked = dtTargetLock.Rows.Count > 0
                                                      && Convert.ToBoolean(dtTargetLock.Rows[0][0]);
                                if (isTargetLocked)
                                {
                                    transaction.Rollback();
                                    NotificationHelper.ShowError(
                                        $"Lớp đích ({item.TargetClassName}) đã được GVCN chốt sổ!\n" +
                                        $"Không thể chuyển học sinh sang lớp này.");
                                    return;
                                }
                            }

                            // Thực thi thay đổi 
                            if (item.RequestType == "DropOut")
                            {
                                new SqlCommand($"UPDATE Student SET Status = 'Inactive' WHERE StudentID = '{item.StudentId}'", conn, transaction).ExecuteNonQuery();
                                new SqlCommand($"DELETE FROM ClassPlacement WHERE StudentID = '{item.StudentId}' AND EffectiveTo IS NULL", conn, transaction).ExecuteNonQuery();
                            }
                            else if (item.RequestType == "ClassTransfer")
                            {
                                new SqlCommand($"UPDATE ClassPlacement SET ClassID = {item.TargetClassId} WHERE StudentID = '{item.StudentId}' AND EffectiveTo IS NULL", conn, transaction).ExecuteNonQuery();
                            }

                            // Đánh dấu đơn đã xong
                            SqlCommand cmd = new SqlCommand(
                                $"UPDATE Application SET StatusID = 4, RespondedAt = GETDATE() WHERE RequestID = {item.RequestId} AND StatusID = 2",
                                conn, transaction);
                            if (cmd.ExecuteNonQuery() == 0) throw new Exception("Đơn đã được xử lý bởi người khác!");

                            transaction.Commit();
                            NotificationHelper.ShowSuccess("Thực thi yêu cầu thành công!");
                            LoadPendingRequests();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            NotificationHelper.ShowError("Lỗi: " + ex.Message);
                        }
                    }
                }
            }
        }

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