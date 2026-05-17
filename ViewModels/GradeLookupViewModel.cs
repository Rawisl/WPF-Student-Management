using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Data;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;

namespace WPF_Student_Management.ViewModels
{
    public partial class GradeLookupViewModel : ObservableObject
    {
        // ComboBox Data
        public ObservableCollection<string> AvailableSemesters { get; } = new() { "Học kỳ 1", "Học kỳ 2" };
        public ObservableCollection<string> AvailableAcademicYears { get; } = new() {"2025-2026"};

        [ObservableProperty]
        private string _selectedSemester = "Học kỳ 1";

        [ObservableProperty]
        private string _selectedAcademicYear = "2025-2026";

        // Biến chứa ViewModel của Component Bảng Điểm (Nếu có data mới nhét vào)
        [ObservableProperty]
        private StudentGradeDetailViewModel _gradeDetailVM;

        // Biến hiển thị thông báo lỗi/trống
        [ObservableProperty]
        private string _emptyMessage = "Vui lòng chọn Năm học, Học kỳ và bấm Xem điểm.";

        [ObservableProperty]
        private bool _isMessageVisible = true;

        [ObservableProperty]
        private bool _isTableVisible = false;

        private string _currentStudentId = "";

        public GradeLookupViewModel()
        {
            // Lấy StudentID từ AccountID đang đăng nhập
            GetStudentIdFromCurrentUser();
        }

        private void GetStudentIdFromCurrentUser()
        {
            if (CurrentUser.Instance == null || CurrentUser.Instance.UserId == 0) return;

            try
            {
                string query = "SELECT StudentID, FullName FROM Student WHERE AccountID = @AccID";
                var dt = DatabaseHelper.ExecuteQuery(query, new[] { new SqlParameter("@AccID", CurrentUser.Instance.UserId) });

                if (dt.Rows.Count > 0)
                {
                    _currentStudentId = dt.Rows[0]["StudentID"].ToString();
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi xác thực học sinh: " + ex.Message);
            }
        }

        [RelayCommand]
        private void ViewGrade()
        {
            if (string.IsNullOrEmpty(_currentStudentId))
            {
                NotificationHelper.ShowError("Không tìm thấy thông tin Học sinh. Vui lòng đăng nhập lại!");
                return;
            }

            try
            {
                // Bước 1: Check xem kỳ này có ĐIỂM THẬT SỰ không (tránh LEFT JOIN in ra toàn dấu -)
                string checkQuery = "SELECT COUNT(*) FROM Score WHERE StudentID = @StudentID AND Semester = @Semester AND AcademicYear = @AcademicYear";
                var parameters = new[] {
                    new SqlParameter("@StudentID", _currentStudentId),
                    new SqlParameter("@Semester", SelectedSemester),
                    new SqlParameter("@AcademicYear", SelectedAcademicYear)
                };

                // Lấy DataTable và đếm số lượng
                DataTable dtCount = DatabaseHelper.ExecuteQuery(checkQuery, parameters);
                int scoreCount = 0;
                if (dtCount != null && dtCount.Rows.Count > 0)
                {
                    scoreCount = Convert.ToInt32(dtCount.Rows[0][0]);
                }

                // Bước 2: Rẽ nhánh hiển thị theo Requirement
                if (scoreCount == 0)
                {
                    // KHÔNG CÓ DATA -> Hiện message, ẩn bảng
                    GradeDetailVM = null;
                    IsTableVisible = false;
                    IsMessageVisible = true;
                    EmptyMessage = "Chưa có dữ liệu điểm cho học kỳ này.";
                }
                else
                {
                    // CÓ DATA -> Nhét ViewModel vào, ẩn message, bung bảng
                    // Query lại tên học sinh
                    string nameQuery = "SELECT FullName FROM Student WHERE StudentID = @StudentID";
                    DataTable dtName = DatabaseHelper.ExecuteQuery(nameQuery, new[] { new SqlParameter("@StudentID", _currentStudentId) });

                    string fullName = "Học sinh";
                    if (dtName != null && dtName.Rows.Count > 0 && dtName.Rows[0]["FullName"] != DBNull.Value)
                    {
                        fullName = dtName.Rows[0]["FullName"].ToString();
                    }

                    //Khởi tạo ở đuôi set false để ẩn nút đóng
                    GradeDetailVM = new StudentGradeDetailViewModel(_currentStudentId, fullName, SelectedSemester, SelectedAcademicYear, false);

                    IsTableVisible = true;
                    IsMessageVisible = false;
                    NotificationHelper.ShowSuccess("Tra cứu điểm thành công!");
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi tra cứu điểm: " + ex.Message);
            }
        }
    }
}