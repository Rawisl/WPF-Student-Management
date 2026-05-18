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
    public partial class EnrollmentChangeRequestViewModel : ObservableObject
    {
        private readonly Student _student;

        [ObservableProperty] private string _studentId;
        [ObservableProperty] private string _studentName;
        [ObservableProperty] private string _currentClassName;
        private int _currentClassId;
        private int _currentGrade; // Lưu khối lớp (10, 11, 12)

        [ObservableProperty] private ObservableCollection<string> _requestTypes = new() { "Xin chuyển lớp", "Xin thôi học" };

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsTransferClassVisible))]
        private string _selectedRequestType;
        public bool IsTransferClassVisible => SelectedRequestType == "Xin chuyển lớp";

        [ObservableProperty] private ObservableCollection<ComboBoxItem> _availableClasses = new();
        [ObservableProperty] private ComboBoxItem _selectedTargetClass;
        [ObservableProperty] private string _reason;

        public EnrollmentChangeRequestViewModel(Student student)
        {
            _student = student;
            StudentId = student.StudentId.ToString();
            StudentName = student.FullName;

            LoadCurrentClassInfo();
        }

        private void LoadCurrentClassInfo()
        {
            // Lấy thông tin lớp hiện tại và Khối của học sinh
            string query = @"SELECT c.ClassID, c.ClassName, c.Grade 
                             FROM Class c JOIN ClassPlacement cp ON c.ClassID = cp.ClassID 
                             WHERE cp.StudentID = @StudentID AND c.AcademicYear = @AcademicYear";

            // Lấy năm học hiện tại từ config hoặc hardcode tạm
            string currentYear = "2025-2026";

            DataTable dt = DatabaseHelper.ExecuteQuery(query, new[] {
                new SqlParameter("@StudentID", StudentId),
                new SqlParameter("@AcademicYear", currentYear)
            });

            if (dt.Rows.Count > 0)
            {
                _currentClassId = Convert.ToInt32(dt.Rows[0]["ClassID"]);
                CurrentClassName = dt.Rows[0]["ClassName"].ToString();
                _currentGrade = Convert.ToInt32(dt.Rows[0]["Grade"]);
            }

            LoadAvailableClasses(currentYear);
        }

        private void LoadAvailableClasses(string academicYear)
        {
            // Chỉ load các lớp CÙNG KHỐI, trừ lớp hiện tại ra
            string query = "SELECT ClassID, ClassName FROM Class WHERE Grade = @Grade AND AcademicYear = @AcademicYear AND ClassID != @CurrentClassID";
            DataTable dt = DatabaseHelper.ExecuteQuery(query, new[] {
                new SqlParameter("@Grade", _currentGrade),
                new SqlParameter("@AcademicYear", academicYear),
                new SqlParameter("@CurrentClassID", _currentClassId)
            });

            foreach (DataRow row in dt.Rows)
            {
                AvailableClasses.Add(new ComboBoxItem { Id = Convert.ToInt32(row["ClassID"]), Name = row["ClassName"].ToString() });
            }
        }

        [RelayCommand]
        private void Submit()
        {
            if (string.IsNullOrWhiteSpace(SelectedRequestType))
            {
                NotificationHelper.ShowWarning("Vui lòng chọn loại yêu cầu!");
                return;
            }

            if (IsTransferClassVisible && SelectedTargetClass == null)
            {
                NotificationHelper.ShowWarning("Vui lòng chọn lớp muốn chuyển đến!");
                return;
            }

            if (string.IsNullOrWhiteSpace(Reason))
            {
                NotificationHelper.ShowWarning("Vui lòng nhập lý do lập đơn!");
                return;
            }

            try
            {
                // 1. Chống Spam đơn Pending (Sửa lại tên bảng Application)
                string checkQuery = "SELECT COUNT(*) AS Total FROM Application WHERE StudentID = @StudentID AND StatusID = 1";
                DataTable dtCheck = DatabaseHelper.ExecuteQuery(checkQuery, new[] { new SqlParameter("@StudentID", StudentId) });

                int pendingCount = 0;
                if (dtCheck != null && dtCheck.Rows.Count > 0)
                {
                    pendingCount = Convert.ToInt32(dtCheck.Rows[0]["Total"]);
                }

                if (pendingCount > 0)
                {
                    NotificationHelper.ShowError("Học sinh này đang có đơn chờ xử lý. Không thể lập thêm đơn mới!");
                    return;
                }

                bool confirm = NotificationHelper.ShowConfirm($"Bạn có chắc chắn muốn lập đơn {SelectedRequestType} cho học sinh {StudentName} không?");
                if (!confirm) return;

                // 2. Chuyển đổi RequestType UI sang chuẩn DB (ClassTransfer / DropOut)
                string dbRequestType = SelectedRequestType == "Xin chuyển lớp" ? "ClassTransfer" : "DropOut";
                int currentAccountId = CurrentUser.Instance?.UserId ?? 0;

                // 3. Múc xuống CSDL
                string insertQuery = @"
                    DECLARE @EmpID INT = (SELECT TOP 1 EmployeeID FROM Employee WHERE AccountID = @AccountID);

                    INSERT INTO Application (StudentID, CreatedByTeacherID, NewClassID, RequestType, Reason, StatusID)
                    VALUES (@StudentID, @EmpID, @TargetClass, @ReqType, @Reason, 1)";

                SqlParameter[] paras = {
                    new SqlParameter("@StudentID", StudentId),
                    new SqlParameter("@AccountID", currentAccountId),
                    new SqlParameter("@ReqType", dbRequestType),
                    new SqlParameter("@TargetClass", IsTransferClassVisible ? SelectedTargetClass.Id : DBNull.Value),
                    new SqlParameter("@Reason", Reason.Trim())
                };

                DatabaseHelper.ExecuteNonQuery(insertQuery, paras);

                NotificationHelper.ShowSuccess("Lập đơn thành công! Đơn đã được gửi đến Giáo vụ.");
                MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi lưu đơn: " + ex.Message);
            }
        }

        [RelayCommand]
        private void Cancel() => MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
    }
}