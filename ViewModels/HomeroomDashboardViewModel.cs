using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;

namespace WPF_Student_Management.ViewModels
{
    public class HomeroomStudentGradeItem
    {
        public int STT { get; set; }
        public string StudentId { get; set; }
        public string FullName { get; set; }
        public string Gender { get; set; }
        public string ClassName { get; set; }
        public string AverageScore { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class ReportItem
    {
        public int STT { get; set; }
        public string StudentId { get; set; }
        public string FullName { get; set; }
        public string Status { get; set; } // Đạt / Không đạt
    }

    public class FailedSubjectItem
    {
        public string SubjectName { get; set; }
        public string RegularTestScore { get; set; }
        public string MidTermScore { get; set; }
        public string FinalTermScore { get; set; }
        public string AverageScore { get; set; }
    }

    public class FailedSubjectViewModel
    {
        public string StudentName { get; set; }
        public ObservableCollection<FailedSubjectItem> FailedSubjectsList { get; set; }
    }

    // ĐÃ FIX: Kế thừa ObservableObject thay vì INotifyPropertyChanged
    public partial class HomeroomDashboardViewModel : ObservableObject
    {
        // --- PROPETIES TỰ ĐỘNG BẰNG [ObservableProperty] ---

        [ObservableProperty]
        private string _currentSemester = "Học kỳ 1";
        partial void OnCurrentSemesterChanged(string value) => LoadHomeroomData();

        [ObservableProperty]
        private string _currentAcademicYear = "2025-2026";
        partial void OnCurrentAcademicYearChanged(string value) => LoadHomeroomData();

        private ObservableCollection<HomeroomStudentGradeItem> _allStudents;

        [ObservableProperty]
        private ObservableCollection<HomeroomStudentGradeItem> _displayStudents;

        [ObservableProperty]
        private string _searchText;
        partial void OnSearchTextChanged(string value) => FilterData();

        [ObservableProperty]
        private ObservableCollection<string> _genderList;

        [ObservableProperty]
        private string _selectedGender;
        partial void OnSelectedGenderChanged(string value) => FilterData();

        [ObservableProperty]
        private string _classTitle;

        private int _currentClassId = 0;
        private int _currentTeacherId = 0;

        [ObservableProperty]
        private ObservableCollection<ReportItem> _reportList;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowConfirmButton))]
        private bool _isReportGenerated = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowConfirmButton))]
        private bool _isClassLocked = false;

        public bool ShowConfirmButton => IsReportGenerated && !IsClassLocked;

        [ObservableProperty] private string _totalStudents;
        [ObservableProperty] private string _passedStudents;
        [ObservableProperty] private string _passRate;

        public Visibility ActionVisibility => PermissionService.HasFeature(PermissionService.Feature.ManageHomeroom)
                                              ? Visibility.Visible : Visibility.Collapsed;

        // --- CÁC BIẾN CHỈ DÙNG ĐỂ HIGHLIGHT DÒNG ĐƯỢC CHỌN TRÊN LƯỚI ---
        [ObservableProperty]
        private HomeroomStudentGradeItem _selectedProfileStudent;

        [ObservableProperty]
        private HomeroomStudentGradeItem _selectedGradeStudent;

        // =========================================================================
        // SỰ KIỆN KHI DOUBLE-CLICK VÀO TAB DANH SÁCH (Sửa thông tin)
        // =========================================================================
        [RelayCommand]
        private async Task OpenStudentDetail(HomeroomStudentGradeItem item)
        {
            if (item == null)
                return;
            try
            {
                string query = "SELECT * FROM Student WHERE StudentID = @ID";
                DataTable dt = DatabaseHelper.ExecuteQuery(query, new[] { new SqlParameter("@ID", item.StudentId) });

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    Student fullStudent = new Student
                    {
                        StudentId = row["StudentID"].ToString(),
                        FullName = row["FullName"].ToString(),
                        Gender = row["Gender"].ToString(),
                        DateOfBirth = row["DateOfBirth"] != DBNull.Value ? Convert.ToDateTime(row["DateOfBirth"]) : null,
                        PhoneNumber = row["PhoneNumber"].ToString(),
                        Email = row["Email"].ToString(),
                        Address = row["Address"].ToString(),
                        FamilyBackground = row["FamilyBackground"].ToString(),
                        GuardianName = row["GuardianName"].ToString(),
                        GuardianPhoneNumber = row["GuardianPhoneNumber"].ToString(),
                        AccountId = row["AccountID"] != DBNull.Value ? Convert.ToInt32(row["AccountID"]) : 0
                    };

                    var detailVM = new StudentProfileDetailViewModel(fullStudent);
                    var detailUC = new WPF_Student_Management.Components.StudentProfileDetailUC { DataContext = detailVM };
                    await MaterialDesignThemes.Wpf.DialogHost.Show(detailUC, "RootDialog");
                    LoadHomeroomData();
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi khi mở hồ sơ: " + ex.Message);
            }
        }

        // =========================================================================
        // SỰ KIỆN KHI DOUBLE-CLICK VÀO TAB BẢNG ĐIỂM (Xem điểm chi tiết bôi đỏ)
        // =========================================================================
        [RelayCommand] // <--- QUAN TRỌNG: Phải có dòng này UI mới móc nối được
        private async Task OpenGradeDetail(HomeroomStudentGradeItem item)
        {
            if (item == null)
                return;
            try
            {
                var gradeVM = new StudentGradeDetailViewModel(item.StudentId, item.FullName, CurrentSemester, CurrentAcademicYear);
                var gradeUC = new WPF_Student_Management.Components.StudentGradeDetailUC { DataContext = gradeVM };
                await MaterialDesignThemes.Wpf.DialogHost.Show(gradeUC, "RootDialog");
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi khi mở bảng điểm chi tiết: " + ex.Message);
            }
        }

        // Sự kiện: Khi click chọn 1 Báo cáo
        [ObservableProperty]
        private ReportItem _selectedReportItem;
        partial void OnSelectedReportItemChanged(ReportItem value)
        {
            if (value != null)
            {
                ViewDetail(value);
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => SelectedReportItem = null);
            }
        }

        // --- CONSTRUCTOR ---
        public HomeroomDashboardViewModel()
        {
            GenderList = new ObservableCollection<string> { "Tất cả", "Nam", "Nữ" };

            bool isDesignMode = DesignerProperties.GetIsInDesignMode(new DependencyObject());
            if (!isDesignMode)
            {
                SelectedGender = "Tất cả";

                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    LoadHomeroomData();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        // --- METHODS VÀ COMMANDS ĐÃ ĐƯỢC ÉP XUNG ---

        private bool CanExecuteReportActions() => _currentClassId > 0;

        private void LoadHomeroomData()
        {
            _allStudents = new ObservableCollection<HomeroomStudentGradeItem>();
            ReportList = null;
            IsReportGenerated = false;

            try
            {
                if (CurrentUser.Instance == null || CurrentUser.Instance.UserId == 0)
                {
                    ClassTitle = "Vui lòng đăng nhập vào hệ thống.";
                    FilterData();
                    return;
                }

                int currentUserId = CurrentUser.Instance.UserId;

                string roleQuery = @"
                    SELECT r.RoleName 
                    FROM Account a 
                    JOIN Role r ON a.RoleID = r.RoleID 
                    WHERE a.AccountID = @AccountID";

                DataTable dtRole = DatabaseHelper.ExecuteQuery(roleQuery, new[] { new SqlParameter("@AccountID", currentUserId) });

                if (dtRole.Rows.Count == 0 || dtRole.Rows[0]["RoleName"].ToString() != "GVCN")
                {
                    ClassTitle = "Bạn không phải là Giáo viên chủ nhiệm.";
                    FilterData();
                    return;
                }

                string query = @"
                SELECT 
                    c.ClassID, e.EmployeeID, ISNULL(cr.IsLocked, 0) AS IsLocked,
                    s.StudentID, s.FullName, s.Gender, s.DateOfBirth, s.PhoneNumber, c.ClassName,
                    sa.OverallAverage,
                    (SELECT COUNT(SubjectID) FROM Score WHERE StudentID = s.StudentID AND Semester = @Semester AND AcademicYear = @AcademicYear) as GradedCount,
                    (SELECT COUNT(DISTINCT SubjectID) FROM TeachingAssignment 
                     WHERE ClassID = c.ClassID AND Semester = @Semester AND AcademicYear = @AcademicYear) as TotalSubjects
                FROM Student s
                JOIN ClassPlacement cp ON s.StudentID = cp.StudentID AND cp.EffectiveTo IS NULL AND cp.AcademicYear = @AcademicYear
                JOIN Class c ON cp.ClassID = c.ClassID
                JOIN Employee e ON c.HomeroomTeacherID = e.EmployeeID
                JOIN Account a ON e.AccountID = a.AccountID
                LEFT JOIN ClassReport cr ON c.ClassID = cr.ClassID AND cr.Semester = @Semester AND cr.AcademicYear = @AcademicYear
                LEFT JOIN StudentAverage sa ON s.StudentID = sa.StudentID AND sa.Semester = @Semester AND sa.AcademicYear = @AcademicYear
                WHERE a.AccountID = @AccountID AND c.AcademicYear = @AcademicYear";

                SqlParameter[] parameters = {
                    new SqlParameter("@AccountID", currentUserId),
                    new SqlParameter("@Semester", CurrentSemester),
                    new SqlParameter("@AcademicYear", CurrentAcademicYear)
                };
                DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);

                if (dt.Rows.Count > 0)
                {
                    _currentClassId = Convert.ToInt32(dt.Rows[0]["ClassID"]);
                    _currentTeacherId = Convert.ToInt32(dt.Rows[0]["EmployeeID"]);

                    // Cập nhật trạng thái các Command sau khi có ClassID mới
                    GenerateReportCommand.NotifyCanExecuteChanged();
                    ConfirmReportCommand.NotifyCanExecuteChanged();
                    CancelReportCommand.NotifyCanExecuteChanged();
                    ViewDetailCommand.NotifyCanExecuteChanged();

                    IsClassLocked = Convert.ToBoolean(dt.Rows[0]["IsLocked"]);
                    ClassTitle = $"Danh sách học tập lớp {dt.Rows[0]["ClassName"]} - {CurrentSemester}";

                    int stt = 1;
                    foreach (DataRow row in dt.Rows)
                    {
                        int gradedCount = Convert.ToInt32(row["GradedCount"]);
                        int totalSubjects = Convert.ToInt32(row["TotalSubjects"]);

                        string scoreStr;
                        if (totalSubjects == 0) scoreStr = "Chưa phân công môn";
                        else if (gradedCount == 0) scoreStr = "Chưa có điểm";
                        else if (gradedCount < totalSubjects) scoreStr = "Thiếu điểm môn";
                        else scoreStr = row["OverallAverage"] != DBNull.Value ? Convert.ToDecimal(row["OverallAverage"]).ToString("0.0") : "Chưa có điểm";

                        _allStudents.Add(new HomeroomStudentGradeItem
                        {
                            STT = stt++,
                            StudentId = row["StudentID"].ToString(),
                            FullName = row["FullName"].ToString(),
                            Gender = row["Gender"].ToString(),
                            ClassName = row["ClassName"].ToString(),
                            AverageScore = scoreStr,
                            DateOfBirth = row["DateOfBirth"] != DBNull.Value ? Convert.ToDateTime(row["DateOfBirth"]) : null,
                            PhoneNumber = row["PhoneNumber"].ToString()
                        });
                    }
                }
                else
                {
                    ClassTitle = "Tài khoản này hiện chưa được phân công chủ nhiệm lớp nào trong năm học này.";
                    _currentClassId = 0;
                    GenerateReportCommand.NotifyCanExecuteChanged();
                    ConfirmReportCommand.NotifyCanExecuteChanged();
                    CancelReportCommand.NotifyCanExecuteChanged();
                    ViewDetailCommand.NotifyCanExecuteChanged();
                }

                FilterData();
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi hệ thống khi tải dữ liệu lớp chủ nhiệm: " + ex.Message);
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteReportActions))]
        private void GenerateReport()
        {
            try
            {
                string checkLockQuery = @"
                    DECLARE @TotalAssigned INT = (SELECT COUNT(DISTINCT SubjectID) FROM TeachingAssignment WHERE ClassID = @ClassID AND Semester = @Semester AND AcademicYear = @AcademicYear);
                    DECLARE @TotalLocked INT = (SELECT COUNT(*) FROM SubjectReport WHERE ClassID = @ClassID AND Semester = @Semester AND AcademicYear = @AcademicYear AND IsLocked = 1);
                    SELECT @TotalAssigned AS TotalAssigned, @TotalLocked AS TotalLocked;";

                SqlParameter[] lockParams = {
                    new SqlParameter("@ClassID", _currentClassId),
                    new SqlParameter("@Semester", CurrentSemester),
                    new SqlParameter("@AcademicYear", CurrentAcademicYear)
                };

                DataTable dtCheck = DatabaseHelper.ExecuteQuery(checkLockQuery, lockParams);
                if (dtCheck.Rows.Count > 0)
                {
                    int totalAssigned = Convert.ToInt32(dtCheck.Rows[0]["TotalAssigned"]);
                    int totalLocked = Convert.ToInt32(dtCheck.Rows[0]["TotalLocked"]);

                    if (totalAssigned == 0)
                    {
                        NotificationHelper.ShowError("Lớp này chưa được phân công môn học nào! Không thể lập báo cáo.");
                        IsReportGenerated = false;
                        return;
                    }

                    if (totalLocked < totalAssigned)
                    {
                        NotificationHelper.ShowError($"Chưa thể lập báo cáo! Tình trạng: {totalLocked}/{totalAssigned} môn đã được GVBM lập báo cáo.");
                        IsReportGenerated = false;
                        return;
                    }
                }

                string getPassingGradeQuery = "SELECT ISNULL((SELECT Value FROM Parameter WHERE ParameterName = 'NumPassingGrade'), 5.0) as PassingGrade";
                DataTable dtParam = DatabaseHelper.ExecuteQuery(getPassingGradeQuery);
                decimal passingGrade = Convert.ToDecimal(dtParam.Rows[0]["PassingGrade"]);

                string query = @"
                    SELECT 
                        s.StudentID, s.FullName,
                        sa.OverallAverage,
                        (SELECT MIN(AverageScore) FROM Score WHERE StudentID = s.StudentID AND Semester = @Semester AND AcademicYear = @AcademicYear) AS MinScore
                    FROM Student s
                    JOIN ClassPlacement cp ON s.StudentID = cp.StudentID AND cp.EffectiveTo IS NULL AND cp.AcademicYear = @AcademicYear
                    LEFT JOIN StudentAverage sa ON s.StudentID = sa.StudentID AND sa.Semester = @Semester AND sa.AcademicYear = @AcademicYear
                    WHERE cp.ClassID = @ClassID";

                SqlParameter[] parameters = {
                    new SqlParameter("@ClassID", _currentClassId),
                    new SqlParameter("@Semester", CurrentSemester),
                    new SqlParameter("@AcademicYear", CurrentAcademicYear)
                };
                DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);

                var tempList = new ObservableCollection<ReportItem>();
                int passCount = 0;
                int stt = 1;

                foreach (DataRow row in dt.Rows)
                {
                    decimal minScore = row["MinScore"] != DBNull.Value ? Convert.ToDecimal(row["MinScore"]) : 0;
                    decimal overallAverage = row["OverallAverage"] != DBNull.Value ? Convert.ToDecimal(row["OverallAverage"]) : 0;

                    bool isPassed = (overallAverage >= passingGrade) && (minScore >= passingGrade);
                    if (isPassed) passCount++;

                    tempList.Add(new ReportItem
                    {
                        STT = stt++,
                        StudentId = row["StudentID"].ToString(),
                        FullName = row["FullName"].ToString(),
                        Status = isPassed ? "Đạt" : "Không đạt"
                    });
                }

                ReportList = tempList;
                TotalStudents = dt.Rows.Count.ToString();
                PassedStudents = passCount.ToString();
                PassRate = dt.Rows.Count > 0 ? ((double)passCount / dt.Rows.Count * 100).ToString("0.0") + "%" : "0%";

                IsReportGenerated = true;
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi tạo báo cáo: " + ex.Message);
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteReportActions))]
        private void ConfirmReport()
        {
            try
            {
                string pendingCheckQuery = @"
                SELECT COUNT(*) 
                FROM Application a
                JOIN ClassPlacement cp ON a.StudentID = cp.StudentID 
                                       AND cp.EffectiveTo IS NULL
                WHERE cp.ClassID = @ClassID 
                  AND a.StatusID IN (1, 2)";

                DataTable dtPending = DatabaseHelper.ExecuteQuery(pendingCheckQuery,
                    new[] { new SqlParameter("@ClassID", _currentClassId) });

                int pendingCount = Convert.ToInt32(dtPending.Rows[0][0]);
                if (pendingCount > 0)
                {
                    NotificationHelper.ShowError(
                        $"Không thể chốt sổ!\n" +
                        $"Lớp đang có {pendingCount} học sinh với đơn chuyển lớp/thôi học chưa xử lý xong.\n" +
                        $"Vui lòng chờ Hiệu trưởng và Giáo vụ xử lý hết đơn trước khi chốt sổ.");
                    return;
                }

                string query = @"
                IF EXISTS (SELECT 1 FROM ClassReport WHERE ClassID = @ClassID AND Semester = @Semester AND AcademicYear = @AcademicYear)
                BEGIN
                    UPDATE ClassReport 
                    SET IsLocked = 1,
                        TotalStudents = @Total
                    WHERE ClassID = @ClassID AND Semester = @Semester AND AcademicYear = @AcademicYear
                END
                ELSE
                BEGIN
                    INSERT INTO ClassReport (ClassID, Semester, AcademicYear, TotalStudents, IsLocked, CreatedByTeacherID, CreatedAt)
                    VALUES (@ClassID, @Semester, @AcademicYear, @Total, 1, @TeacherID, GETDATE())
                END";

                SqlParameter[] parameters = {
                    new SqlParameter("@ClassID", _currentClassId),
                    new SqlParameter("@Semester", CurrentSemester),
                    new SqlParameter("@AcademicYear", CurrentAcademicYear),
                    new SqlParameter("@TeacherID", _currentTeacherId),
                    new SqlParameter("@Total", int.Parse(TotalStudents)),
                };

                int rows = DatabaseHelper.ExecuteNonQuery(query, parameters);

                if (rows > 0)
                {
                    IsClassLocked = true;
                    NotificationHelper.ShowSuccess("Đã xác nhận báo cáo và KHÓA SỔ thành công! Giáo viên bộ môn sẽ không thể sửa điểm của kỳ này nữa.");
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi khóa sổ: " + ex.Message);
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteReportActions))]
        private void CancelReport()
        {
            try
            {
                string query = "UPDATE ClassReport SET IsLocked = 0 WHERE ClassID = @ClassID AND Semester = @Semester AND AcademicYear = @AcademicYear";

                SqlParameter[] parameters = {
                    new SqlParameter("@ClassID", _currentClassId),
                    new SqlParameter("@Semester", CurrentSemester),
                    new SqlParameter("@AcademicYear", CurrentAcademicYear)
                };

                int rows = DatabaseHelper.ExecuteNonQuery(query, parameters);

                if (rows > 0)
                {
                    IsClassLocked = false;
                    NotificationHelper.ShowSuccess("Đã HỦY báo cáo và MỞ KHÓA sổ thành công!");
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi mở khóa sổ: " + ex.Message);
            }
        }

       
        [RelayCommand(CanExecute = nameof(CanExecuteReportActions))]
        private async Task ViewDetail(ReportItem selectedStudent)
        {
            if (selectedStudent == null) return;
            if (selectedStudent.Status.Trim().Equals("Đạt", StringComparison.OrdinalIgnoreCase)) return;

            if (selectedStudent.Status.Trim().Equals("Không đạt", StringComparison.OrdinalIgnoreCase))
            {
                var failedList = GetFailedSubjectsFromDB(selectedStudent.StudentId);

                var detailVM = new FailedSubjectViewModel
                {
                    StudentName = selectedStudent.FullName,
                    FailedSubjectsList = new ObservableCollection<FailedSubjectItem>(failedList)
                };

                var detailUC = new WPF_Student_Management.Components.FailedSubjectDetailUC
                {
                    DataContext = detailVM
                };

                await MaterialDesignThemes.Wpf.DialogHost.Show(detailUC, "RootDialog");
            }
        }

        private List<FailedSubjectItem> GetFailedSubjectsFromDB(string studentId)
        {
            var list = new List<FailedSubjectItem>();
            try
            {
                string paramQuery = "SELECT ISNULL((SELECT Value FROM Parameter WHERE ParameterName = 'NumPassingGrade'), 5.0) as PassingGrade";
                DataTable dtParam = DatabaseHelper.ExecuteQuery(paramQuery);
                decimal passingGrade = Convert.ToDecimal(dtParam.Rows[0]["PassingGrade"]);

                string query = @"
                    SELECT sub.SubjectName, sc.RegularTestScore, sc.MidTermScore, sc.FinalTermScore, sc.AverageScore 
                    FROM Score sc
                    JOIN Subject sub ON sc.SubjectID = sub.SubjectID
                    WHERE sc.StudentID = @StudentID 
                      AND sc.AverageScore < @PassingGrade
                      AND sc.Semester = @Semester 
                      AND sc.AcademicYear = @AcademicYear";

                SqlParameter[] parameters = {
                    new SqlParameter("@StudentID", studentId),
                    new SqlParameter("@PassingGrade", passingGrade),
                    new SqlParameter("@Semester", CurrentSemester),
                    new SqlParameter("@AcademicYear", CurrentAcademicYear)
                };

                DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new FailedSubjectItem
                    {
                        SubjectName = row["SubjectName"].ToString(),
                        RegularTestScore = row["RegularTestScore"] != DBNull.Value ? Convert.ToDecimal(row["RegularTestScore"]).ToString("0.##") : "",
                        MidTermScore = row["MidTermScore"] != DBNull.Value ? Convert.ToDecimal(row["MidTermScore"]).ToString("0.##") : "",
                        FinalTermScore = row["FinalTermScore"] != DBNull.Value ? Convert.ToDecimal(row["FinalTermScore"]).ToString("0.##") : "",
                        AverageScore = row["AverageScore"] != DBNull.Value ? Convert.ToDecimal(row["AverageScore"]).ToString("0.##") : ""
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi tải danh sách môn chưa đạt: " + ex.Message);
            }
            return list;
        }

        private void FilterData()
        {
            if (_allStudents == null) return;

            var filtered = _allStudents.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
                filtered = filtered.Where(s => s.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(SelectedGender) && SelectedGender != "Tất cả")
                filtered = filtered.Where(s => s.Gender.Equals(SelectedGender, StringComparison.OrdinalIgnoreCase));

            var resultList = filtered.ToList();
            for (int i = 0; i < resultList.Count; i++) resultList[i].STT = i + 1;

            DisplayStudents = new ObservableCollection<HomeroomStudentGradeItem>(resultList);
        }
    }
}