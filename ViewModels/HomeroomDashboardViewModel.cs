using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    public class HomeroomDashboardViewModel : INotifyPropertyChanged
    {
        // --- BỔ SUNG: BIẾN KIỂM SOÁT THỜI GIAN ---
        private string _currentSemester = "Học kỳ 1";
        public string CurrentSemester
        {
            get => _currentSemester;
            set { _currentSemester = value; OnPropertyChanged(); LoadHomeroomData(); }
        }

        private string _currentAcademicYear = "2025-2026";
        public string CurrentAcademicYear
        {
            get => _currentAcademicYear;
            set { _currentAcademicYear = value; OnPropertyChanged(); LoadHomeroomData(); }
        }
        // ----------------------------------------

        private ObservableCollection<HomeroomStudentGradeItem> _allStudents;

        private ObservableCollection<HomeroomStudentGradeItem> _displayStudents;
        public ObservableCollection<HomeroomStudentGradeItem> DisplayStudents
        {
            get => _displayStudents;
            set { _displayStudents = value; OnPropertyChanged(); }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); FilterData(); }
        }

        public ObservableCollection<string> GenderList { get; set; }

        private string _selectedGender;
        public string SelectedGender
        {
            get => _selectedGender;
            set { _selectedGender = value; OnPropertyChanged(); FilterData(); }
        }

        private string _classTitle;
        public string ClassTitle
        {
            get => _classTitle;
            set { _classTitle = value; OnPropertyChanged(); }
        }

        private int _currentClassId = 0;
        private int _currentTeacherId = 0; // Thêm biến lưu ID giáo viên để cắm vào Report

        private ObservableCollection<ReportItem> _reportList;
        public ObservableCollection<ReportItem> ReportList
        {
            get => _reportList;
            set { _reportList = value; OnPropertyChanged(); }
        }

        private bool _isReportGenerated = false;
        public bool IsReportGenerated
        {
            get => _isReportGenerated;
            set { _isReportGenerated = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowConfirmButton)); }
        }

        private bool _isClassLocked = false;
        public bool IsClassLocked
        {
            get => _isClassLocked;
            set { _isClassLocked = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowConfirmButton)); }
        }

        public bool ShowConfirmButton => IsReportGenerated && !IsClassLocked;

        private string _totalStudents;
        public string TotalStudents { get => _totalStudents; set { _totalStudents = value; OnPropertyChanged(); } }

        private string _passedStudents;
        public string PassedStudents { get => _passedStudents; set { _passedStudents = value; OnPropertyChanged(); } }

        private string _passRate;
        public string PassRate { get => _passRate; set { _passRate = value; OnPropertyChanged(); } }

        // --- COMMANDS ---
        public ICommand GenerateReportCommand { get; }
        public ICommand ConfirmReportCommand { get; }
        public ICommand CancelReportCommand { get; }
        public ICommand ViewDetailCommand { get; }
        public ICommand OpenStudentDetailCommand { get; }

        public HomeroomDashboardViewModel()
        {
            GenderList = new ObservableCollection<string> { "Tất cả", "Nam", "Nữ" };

            GenerateReportCommand = new RelayCommand(ExecuteGenerateReport, CanExecuteReportActions);
            ConfirmReportCommand = new RelayCommand(ExecuteConfirmReport, CanExecuteReportActions);
            CancelReportCommand = new RelayCommand(ExecuteCancelReport, CanExecuteReportActions);
            ViewDetailCommand = new RelayCommand<object>(ExecuteViewDetail, CanExecuteReportActions);
            OpenStudentDetailCommand = new RelayCommand<HomeroomStudentGradeItem>(ExecuteOpenStudentDetail);

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

        private bool CanExecuteReportActions(object obj) => _currentClassId > 0;

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

                // SỬA: Đếm TotalSubjects dựa trên TeachingAssignment của Lớp đó trong Học kỳ/Năm học hiện tại
                string query = @"
            SELECT 
                c.ClassID, e.EmployeeID, ISNULL(cr.IsLocked, 0) AS IsLocked,
                s.StudentID, s.FullName, s.Gender, s.DateOfBirth, s.PhoneNumber, c.ClassName,
                AVG(CASE WHEN sub.SubjectName <> N'Giáo dục thể chất' THEN sc.AverageScore ELSE NULL END) as OverallAverage,
                COUNT(sc.SubjectID) as GradedCount,
                (SELECT COUNT(DISTINCT SubjectID) FROM TeachingAssignment 
                 WHERE ClassID = c.ClassID AND Semester = @Semester AND AcademicYear = @AcademicYear) as TotalSubjects
            FROM Student s
            JOIN ClassPlacement cp ON s.StudentID = cp.StudentID
            JOIN Class c ON cp.ClassID = c.ClassID
            JOIN Employee e ON c.HomeroomTeacherID = e.EmployeeID
            JOIN Account a ON e.AccountID = a.AccountID
            LEFT JOIN ClassReport cr ON c.ClassID = cr.ClassID AND cr.Semester = @Semester AND cr.AcademicYear = @AcademicYear
            LEFT JOIN Score sc ON s.StudentID = sc.StudentID AND sc.Semester = @Semester AND sc.AcademicYear = @AcademicYear
            LEFT JOIN Subject sub ON sc.SubjectID = sub.SubjectID
            WHERE a.AccountID = @AccountID AND c.AcademicYear = @AcademicYear
            GROUP BY c.ClassID, e.EmployeeID, cr.IsLocked, s.StudentID, s.FullName, s.Gender, s.DateOfBirth, s.PhoneNumber, c.ClassName";

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
                    IsClassLocked = Convert.ToBoolean(dt.Rows[0]["IsLocked"]);
                    ClassTitle = $"Danh sách học tập lớp {dt.Rows[0]["ClassName"]} - {CurrentSemester}";

                    int stt = 1;
                    foreach (DataRow row in dt.Rows)
                    {
                        int gradedCount = Convert.ToInt32(row["GradedCount"]);
                        int totalSubjects = Convert.ToInt32(row["TotalSubjects"]);

                        string scoreStr;
                        if (totalSubjects == 0) scoreStr = "Chưa phân công môn"; // Handle trường hợp chưa phân công
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
                }

                FilterData();
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi hệ thống khi tải dữ liệu lớp chủ nhiệm: " + ex.Message);
            }
        }

        private void ExecuteGenerateReport(object obj)
        {
            try
            {
                // BƯỚC 1: KIỂM TRA ĐIỀU KIỆN TIÊN QUYẾT - TẤT CẢ MÔN PHẢI ĐƯỢC GVBM CHỐT SỔ!
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

                // BƯỚC 2: TIẾN HÀNH TÍNH TOÁN BÁO CÁO KHI ĐÃ ĐỦ ĐIỀU KIỆN
                string getPassingGradeQuery = "SELECT ISNULL((SELECT Value FROM Parameter WHERE ParameterName = 'NumPassingGrade'), 5.0) as PassingGrade";
                DataTable dtParam = DatabaseHelper.ExecuteQuery(getPassingGradeQuery);
                decimal passingGrade = Convert.ToDecimal(dtParam.Rows[0]["PassingGrade"]);

                string query = @"
                    SELECT 
                        s.StudentID, s.FullName,
                        MIN(sc.AverageScore) AS MinScore,
                        AVG(CASE WHEN sub.SubjectName <> N'Giáo dục thể chất' THEN sc.AverageScore ELSE NULL END) AS OverallAverage
                    FROM Student s
                    JOIN ClassPlacement cp ON s.StudentID = cp.StudentID
                    LEFT JOIN Score sc ON s.StudentID = sc.StudentID AND sc.Semester = @Semester AND sc.AcademicYear = @AcademicYear
                    LEFT JOIN Subject sub ON sc.SubjectID = sub.SubjectID
                    WHERE cp.ClassID = @ClassID
                    GROUP BY s.StudentID, s.FullName";

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

                    // KIỂM TRA KÉP: Đạt = Điểm TB >= Điểm chuẩn VÀ Không có môn nào bị liệt (< Điểm chuẩn)
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

        private void ExecuteConfirmReport(object obj)
        {
            try
            {
                // SỬA LOGIC: Insert hoặc Update vào bảng ClassReport (Không chọc vào bảng Class nữa)
                string query = @"
                IF EXISTS (SELECT 1 FROM ClassReport WHERE ClassID = @ClassID AND Semester = @Semester AND AcademicYear = @AcademicYear)
                BEGIN
                    UPDATE ClassReport 
                    SET IsLocked = 1, TotalStudents = @TotalStudents 
                    WHERE ClassID = @ClassID AND Semester = @Semester AND AcademicYear = @AcademicYear
                END
                ELSE
                BEGIN
                    INSERT INTO ClassReport (ClassID, Semester, AcademicYear, TotalStudents, IsLocked, CreatedByTeacherID, CreatedAt)
                    VALUES (@ClassID, @Semester, @AcademicYear, @TotalStudents, 1, @TeacherID, GETDATE())
                END";

                SqlParameter[] parameters = {
                    new SqlParameter("@ClassID", _currentClassId),
                    new SqlParameter("@Semester", CurrentSemester),
                    new SqlParameter("@AcademicYear", CurrentAcademicYear),
                    new SqlParameter("@TotalStudents", int.Parse(TotalStudents)),
                    new SqlParameter("@TeacherID", _currentTeacherId)
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

        private void ExecuteCancelReport(object obj)
        {
            try
            {
                // SỬA LOGIC: Mở khóa trên bảng ClassReport
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

        private HomeroomStudentGradeItem _selectedStudent;
        public HomeroomStudentGradeItem SelectedStudent
        {
            get => _selectedStudent;
            set
            {
                _selectedStudent = value;
                OnPropertyChanged();

                if (value != null)
                {
                    ExecuteOpenDetail(value);

                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _selectedStudent = null;
                        OnPropertyChanged();
                    });
                }
            }
        }

        private ReportItem _selectedReportItem;
        public ReportItem SelectedReportItem
        {
            get => _selectedReportItem;
            set
            {
                _selectedReportItem = value;
                OnPropertyChanged();

                if (value != null)
                {
                    ExecuteViewDetail(value);

                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _selectedReportItem = null;
                        OnPropertyChanged(nameof(SelectedReportItem));
                    });
                }
            }
        }

        //code này là code bẩn, khởi tạo giao diện trong viewmodel thì quá rác nhưng t đéo biết làm j khác
        private async void ExecuteOpenDetail(HomeroomStudentGradeItem student)
        {
            var detailVM = new StudentGradeDetailViewModel(student.StudentId, student.FullName, CurrentSemester, CurrentAcademicYear);

            var detailView = new WPF_Student_Management.Components.StudentGradeDetailUC
            {
                DataContext = detailVM
            };

            var popupContainer = new System.Windows.Controls.Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10),
                Effect = new DropShadowEffect
                {
                    BlurRadius = 15,
                    ShadowDepth = 3,
                    Color = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#DDDDDD"),
                    Opacity = 0.5
                },
                Width = 850,                 // Ép size cho Pop-up
                Height = 600,
                Child = detailView           // Nhét cái ruột vào trong vỏ
            };

            await MaterialDesignThemes.Wpf.DialogHost.Show(popupContainer, "RootDialog");
        }

        private async void ExecuteViewDetail(object obj)
        {
            if (obj is ReportItem selectedStudent)
            {
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
        }

        private List<FailedSubjectItem> GetFailedSubjectsFromDB(string studentId)
        {
            var list = new List<FailedSubjectItem>();
            try
            {
                string paramQuery = "SELECT ISNULL((SELECT Value FROM Parameter WHERE ParameterName = 'NumPassingGrade'), 5.0) as PassingGrade";
                DataTable dtParam = DatabaseHelper.ExecuteQuery(paramQuery);
                decimal passingGrade = Convert.ToDecimal(dtParam.Rows[0]["PassingGrade"]);

                // SỬA CÂU QUERY: Thêm bộ lọc Semester và AcademicYear
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private async void ExecuteOpenStudentDetail(HomeroomStudentGradeItem item)
        {
            if (item == null) return;

            try
            {
                // Truy vấn ngược DB để lấy Full Object Student dựa vào StudentId
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

                    // Gọi popup StudentProfileDetailUC lên
                    var detailVM = new StudentProfileDetailViewModel(fullStudent);
                    var detailUC = new WPF_Student_Management.Components.StudentProfileDetailUC { DataContext = detailVM };

                    await MaterialDesignThemes.Wpf.DialogHost.Show(detailUC, "RootDialog");

                    // Tùy chọn: Sau khi đóng popup có thể gọi FilterData() để reload lại lưới nếu data thay đổi
                    LoadHomeroomData();
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi khi mở hồ sơ: " + ex.Message);
            }
        }
    }
}