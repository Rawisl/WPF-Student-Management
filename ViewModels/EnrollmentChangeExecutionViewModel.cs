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
                if (StatusId == 1)
                    return "Chờ hiệu trưởng duyệt";
                if (StatusId == 2)
                    return "Chờ giáo vụ thực thi";
                if (StatusId == 3)
                    return "Bị từ chối";
                if (StatusId == 4)
                    return "Đã hoàn tất";
                return "Không rõ";
            }
        }
        public string StatusTextColor
        {
            get
            {
                if (StatusId == 1)
                    return "#F57F17"; // Cam
                if (StatusId == 2)
                    return "#0288D1"; // Xanh dương
                if (StatusId == 3)
                    return "#D32F2F"; // Đỏ
                if (StatusId == 4)
                    return "#388E3C"; // Xanh lá
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

        [ObservableProperty]
        private PendingRequestItem _selectedDetailItem;

        private PendingRequestItem _processingItem;

        public Visibility ActionColumnVisibility =>
            (PermissionService.HasFeature(PermissionService.Feature.ApproveRequests) ||
             PermissionService.HasFeature(PermissionService.Feature.ExecuteRequests))
            ? Visibility.Visible : Visibility.Collapsed;

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
            if (dt.Rows.Count > 0)
                return Convert.ToInt32(dt.Rows[0][0]);
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
        private async Task OpenRequestDetailDialog(PendingRequestItem item)
        {
            if (item == null)
                return;

            // Gán dữ liệu của dòng vừa click vào biến SelectedDetailItem
            SelectedDetailItem = item;

            //Sử dụng ID của DialogHost đang có sẵn trong View để mở popup
            var detailDialogView = new WPF_Student_Management.Components.RequestDetailDialogUC { DataContext = this };
            await MaterialDesignThemes.Wpf.DialogHost.Show(detailDialogView, "ExecutionDialogHost");
        }

        [RelayCommand]
        private void CloseRequestDetailDialog()
        {
            SelectedDetailItem = null;
            MaterialDesignThemes.Wpf.DialogHost.Close("ExecutionDialogHost");
        }

        [RelayCommand]
        private void ExecuteRequest(PendingRequestItem item)
        {
            if (item == null)
                return;
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
                            //chiếm quyền xử lý đơn ngay trong transaction để chỉ một giáo vụ thực thi được đơn này.
                            var claimCommand = new SqlCommand(@"
                                UPDATE Application
                                SET StatusID = 4,
                                    RespondedAt = GETDATE()
                                WHERE RequestID = @ReqID AND StatusID = 2", conn, transaction);
                            claimCommand.Parameters.AddWithValue("@ReqID", item.RequestId);

                            if (claimCommand.ExecuteNonQuery() == 0)
                            {
                                transaction.Rollback();
                                NotificationHelper.ShowWarning("Đơn này đã được giáo vụ khác xử lý trước!");
                                LoadPendingRequests();
                                return;
                            }

                            // Tải lại trạng thái đơn ngay trong transaction để tránh dùng dữ liệu cũ còn sót trên UI.
                            var loadCommand = new SqlCommand(@"
                                SELECT a.StudentID, a.RequestType, a.NewClassID,
                                       cp.ClassID AS CurrentClassID
                                FROM Application a
                                LEFT JOIN ClassPlacement cp ON cp.StudentID = a.StudentID AND cp.EffectiveTo IS NULL
                                WHERE a.RequestID = @ReqID", conn, transaction);
                            loadCommand.Parameters.AddWithValue("@ReqID", item.RequestId);

                            string studentId;
                            string requestType;
                            int? currentClassId;
                            int? targetClassId;

                            using (var reader = loadCommand.ExecuteReader())
                            {
                                if (!reader.Read())
                                    throw new Exception("Không tìm thấy thông tin đơn cần xử lý.");
                                studentId = reader["StudentID"].ToString() ?? string.Empty;
                                requestType = reader["RequestType"].ToString() ?? string.Empty;
                                currentClassId = reader["CurrentClassID"] != DBNull.Value ? Convert.ToInt32(reader["CurrentClassID"]) : null;
                                targetClassId = reader["NewClassID"] != DBNull.Value ? Convert.ToInt32(reader["NewClassID"]) : null;
                            }

                            // Kiểm tra lại trạng thái khóa sổ bằng chính transaction hiện tại để tránh race giữa nhiều kết nối.
                            if (currentClassId.HasValue)
                            {
                                var sourceLockCommand = new SqlCommand(@"
                                    SELECT ISNULL(IsLocked, 0)
                                    FROM ClassReport
                                    WHERE ClassID = @ClassID
                                      AND Semester = @Semester
                                      AND AcademicYear = @AcademicYear", conn, transaction);
                                sourceLockCommand.Parameters.AddWithValue("@ClassID", currentClassId.Value);
                                sourceLockCommand.Parameters.AddWithValue("@Semester", currentSemester);
                                sourceLockCommand.Parameters.AddWithValue("@AcademicYear", currentYear);

                                if (Convert.ToBoolean(sourceLockCommand.ExecuteScalar() ?? 0))
                                {
                                    throw new Exception($"Lớp cũ ({item.CurrentClassName}) đã khóa sổ! Không thể thực thi.");
                                }
                            }

                            // Kiểm tra lại khóa sổ của lớp đích trước khi thực hiện chuyển lớp.
                            if (requestType == "ClassTransfer")
                            {
                                if (!targetClassId.HasValue)
                                    throw new Exception("Đơn chuyển lớp không có lớp đích hợp lệ.");

                                var targetLockCommand = new SqlCommand(@"
                                    SELECT ISNULL(IsLocked, 0)
                                    FROM ClassReport
                                    WHERE ClassID = @ClassID
                                      AND Semester = @Semester
                                      AND AcademicYear = @AcademicYear", conn, transaction);
                                targetLockCommand.Parameters.AddWithValue("@ClassID", targetClassId.Value);
                                targetLockCommand.Parameters.AddWithValue("@Semester", currentSemester);
                                targetLockCommand.Parameters.AddWithValue("@AcademicYear", currentYear);

                                if (Convert.ToBoolean(targetLockCommand.ExecuteScalar() ?? 0))
                                {
                                    throw new Exception($"Lớp đích ({item.TargetClassName}) đã được GVCN chốt sổ!\nKhông thể chuyển học sinh sang lớp này.");
                                }
                            }

                            // Giữ lại lịch sử xếp lớp: không ghi đè dòng placement hiện tại mà đóng dòng cũ rồi tạo dòng mới khi cần.
                            if (requestType == "DropOut")
                            {
                                var updateStudentCommand = new SqlCommand(@"
                                    UPDATE Student
                                    SET Status = 'Inactive'
                                    WHERE StudentID = @StudentID AND Status <> 'Inactive'", conn, transaction);
                                updateStudentCommand.Parameters.AddWithValue("@StudentID", studentId);
                                updateStudentCommand.ExecuteNonQuery();

                                var closePlacementCommand = new SqlCommand(@"
                                    UPDATE ClassPlacement
                                    SET EffectiveTo = CAST(GETDATE() AS DATE)
                                    WHERE StudentID = @StudentID AND EffectiveTo IS NULL", conn, transaction);
                                closePlacementCommand.Parameters.AddWithValue("@StudentID", studentId);
                                closePlacementCommand.ExecuteNonQuery();
                            }
                            else if (requestType == "ClassTransfer")
                            {
                                if (!currentClassId.HasValue)
                                    throw new Exception("Học sinh không còn lớp hiện tại hợp lệ để chuyển.");

                                var closePlacementCommand = new SqlCommand(@"
                                    UPDATE ClassPlacement
                                    SET EffectiveTo = CAST(GETDATE() AS DATE)
                                    WHERE StudentID = @StudentID AND EffectiveTo IS NULL", conn, transaction);
                                closePlacementCommand.Parameters.AddWithValue("@StudentID", studentId);

                                if (closePlacementCommand.ExecuteNonQuery() == 0)
                                {
                                    throw new Exception("Học sinh không còn thuộc lớp hiện tại. Đơn có thể đã được xử lý trước đó.");
                                }

                                var insertPlacementCommand = new SqlCommand(@"
                                    INSERT INTO ClassPlacement (StudentID, ClassID, AcademicYear, EffectiveFrom)
                                    VALUES (@StudentID, @ClassID, @AcademicYear, CAST(GETDATE() AS DATE))", conn, transaction);
                                insertPlacementCommand.Parameters.AddWithValue("@StudentID", studentId);
                                insertPlacementCommand.Parameters.AddWithValue("@ClassID", targetClassId!.Value);
                                insertPlacementCommand.Parameters.AddWithValue("@AcademicYear", currentYear);
                                insertPlacementCommand.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            NotificationHelper.ShowSuccess("Thực thi yêu cầu thành công!");
                            LoadPendingRequests();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            NotificationHelper.ShowError("Lỗi: " + ex.Message);
                            LoadPendingRequests();
                        }
                    }
                }
            }
        }

        [RelayCommand]
        private async void OpenRejectDialog(PendingRequestItem item)
        {
            if (item == null)
                return;
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