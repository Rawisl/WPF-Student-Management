using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;

namespace WPF_Student_Management.ViewModels
{
    // === MODEL HIỂN THỊ ĐIỂM TRÊN UI ===
    public partial class StudentGradeDisplay : ObservableObject
    {
        public int OrdinalNumber { get; set; }
        public string StudentID { get; set; }
        public string FullName { get; set; }
        public int ScoreID { get; set; }

        public double RegCoef { get; set; } = 1.0;
        public double MidCoef { get; set; } = 2.0;
        public double FinCoef { get; set; } = 3.0;

        // Cờ theo dõi trạng thái chỉnh sửa
        public bool IsDirty { get; set; } = false;

        private double? _regularScore;
        public double? RegularScore
        {
            get => _regularScore;
            set
            {
                if (value.HasValue) { if (value.Value < 0) value = 0; if (value.Value > 10) value = 10; }
                if (SetProperty(ref _regularScore, value))
                {
                    IsDirty = true;
                    OnPropertyChanged(nameof(AverageScore));
                }
            }
        }

        private double? _midSemScore;
        public double? MidSemScore
        {
            get => _midSemScore;
            set
            {
                if (value.HasValue) { if (value.Value < 0) value = 0; if (value.Value > 10) value = 10; }
                if (SetProperty(ref _midSemScore, value))
                {
                    IsDirty = true;
                    OnPropertyChanged(nameof(AverageScore));
                }
            }
        }

        private double? _finalScore;
        public double? FinalScore
        {
            get => _finalScore;
            set
            {
                if (value.HasValue) { if (value.Value < 0) value = 0; if (value.Value > 10) value = 10; }
                if (SetProperty(ref _finalScore, value))
                {
                    IsDirty = true;
                    OnPropertyChanged(nameof(AverageScore));
                }
            }
        }

        public double? AverageScore
        {
            get
            {
                if (RegularScore.HasValue || MidSemScore.HasValue || FinalScore.HasValue)
                {
                    double r = RegularScore ?? 0;
                    double m = MidSemScore ?? 0;
                    double f = FinalScore ?? 0;
                    double totalCoef = RegCoef + MidCoef + FinCoef;

                    if (totalCoef == 0) return 0;
                    return Math.Round((r * RegCoef + m * MidCoef + f * FinCoef) / totalCoef, 1);
                }
                return null;
            }
        }
    }

    // === LỚP HELPER ĐỂ ĐỔ DỮ LIỆU VÀO COMBOBOX ===
    public class ComboBoxItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() => Name;
    }

    // ====================================================================
    // === VIEWMODEL CHÍNH ===
    // ====================================================================
    public partial class SubjectGradebookViewModel : ObservableObject
    {
        public bool HasUnsavedChanges => StudentGrades.Any(hs => hs.IsDirty);

        [ObservableProperty] private ObservableCollection<ComboBoxItem> _subjects = new();
        [ObservableProperty] private ObservableCollection<ComboBoxItem> _classes = new();
        public ObservableCollection<StudentGradeDisplay> StudentGrades { get; set; } = new();

        [ObservableProperty] private ObservableCollection<string> _academicYears = new() { "2025-2026" };
        [ObservableProperty] private string _selectedAcademicYear = "2025-2026";

        [ObservableProperty] private ObservableCollection<string> _semesters = new() { "Học kỳ 1", "Học kỳ 2" };
        [ObservableProperty] private string _selectedSemester = "Học kỳ 1";

        [ObservableProperty] private string _gradebookTitle = "Vui lòng chọn Lớp và Môn học";
        [ObservableProperty] private Visibility _isSaveVisible = Visibility.Hidden;

        private double _regCoef = 1.0;
        private double _midCoef = 2.0;
        private double _finCoef = 3.0;

        // --- CÁC BIẾN KIỂM SOÁT TRẠNG THÁI KHÓA SỔ ---

        //Biến tổng hợp trạng thái Read-Only cho bảng điểm
        public bool IsGradebookReadOnly => IsSubjectLocked || IsClassLockedByGVCN;

        private bool _isSubjectLocked;
        public bool IsSubjectLocked
        {
            get => _isSubjectLocked;
            set
            {
                SetProperty(ref _isSubjectLocked, value);
                OnPropertyChanged(nameof(ShowSubjectLockedWarning));
                OnPropertyChanged(nameof(ShowSubjectConfirmButton));
                OnPropertyChanged(nameof(ShowSubjectCancelButton));
                OnPropertyChanged(nameof(IsGradebookReadOnly));
            }
        }

        private bool _isClassLockedByGVCN;
        public bool IsClassLockedByGVCN
        {
            get => _isClassLockedByGVCN;
            set
            {
                SetProperty(ref _isClassLockedByGVCN, value);
                OnPropertyChanged(nameof(ShowSubjectLockedWarning));
                OnPropertyChanged(nameof(ShowSubjectConfirmButton));
                OnPropertyChanged(nameof(ShowSubjectCancelButton));
                OnPropertyChanged(nameof(IsGradebookReadOnly));
            }
        }

        // Logic ẩn hiện các nút trên UI
        public bool ShowSubjectLockedWarning => IsSubjectLocked && !IsClassLockedByGVCN;
        public bool ShowSubjectConfirmButton => ReportData.Count > 0 && !IsSubjectLocked && !IsClassLockedByGVCN;
        public bool ShowSubjectCancelButton => IsSubjectLocked && !IsClassLockedByGVCN;


        // Bẫy sự kiện khi đổi Năm học / Học kỳ -> Reset lại bảng điểm
        partial void OnSelectedAcademicYearChanged(string value) => RefreshData();
        partial void OnSelectedSemesterChanged(string value) => RefreshData();

        private ComboBoxItem _selectedSubject;
        public ComboBoxItem SelectedSubject
        {
            get => _selectedSubject;
            set
            {
                if (_selectedSubject == value) return;

                if (HasUnsavedChanges)
                {
                    bool confirm = NotificationHelper.ShowConfirm("Bạn đang có điểm chưa lưu!\nNếu chọn môn khác, dữ liệu sẽ bị mất. Bạn có chắc chắn muốn chuyển không?");
                    if (!confirm)
                    {
                        var oldValue = _selectedSubject;
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            _selectedSubject = null;
                            OnPropertyChanged(nameof(SelectedSubject));
                            _selectedSubject = oldValue;
                            OnPropertyChanged(nameof(SelectedSubject));
                        }), System.Windows.Threading.DispatcherPriority.ContextIdle);
                        return;
                    }
                }

                SetProperty(ref _selectedSubject, value);
                LoadGradeDataCommand.NotifyCanExecuteChanged();

                Classes.Clear();
                StudentGrades.Clear();
                ReportData.Clear(); // Dọn bảng báo cáo khi đổi môn
                DetailedStudentList.Clear();

                GradebookTitle = "Vui lòng chọn Lớp học";
                IsSaveVisible = Visibility.Hidden;

                // Mặc định tắt khóa sổ cho đến khi load dữ liệu thực
                IsSubjectLocked = false;
                IsClassLockedByGVCN = false;

                LoadClassesForSubject(value);
            }
        }

        private ComboBoxItem _selectedClass;
        public ComboBoxItem SelectedClass
        {
            get => _selectedClass;
            set
            {
                if (_selectedClass == value) return;

                if (HasUnsavedChanges)
                {
                    bool confirm = NotificationHelper.ShowConfirm("Bạn đang có điểm chưa lưu!\nNếu chọn lớp khác, dữ liệu sẽ bị mất. Bạn có chắc chắn muốn chuyển không?");
                    if (!confirm)
                    {
                        var oldValue = _selectedClass;
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            _selectedClass = null;
                            OnPropertyChanged(nameof(SelectedClass));
                            _selectedClass = oldValue;
                            OnPropertyChanged(nameof(SelectedClass));
                        }), System.Windows.Threading.DispatcherPriority.ContextIdle);
                        return;
                    }
                }

                SetProperty(ref _selectedClass, value);
                LoadGradeDataCommand.NotifyCanExecuteChanged();

                StudentGrades.Clear();
                ReportData.Clear(); // Dọn bảng báo cáo khi đổi lớp
                DetailedStudentList.Clear();

                IsSaveVisible = Visibility.Hidden;
                GradebookTitle = value != null ? "Vui lòng bấm 'Lấy danh sách'" : "Vui lòng chọn Lớp học";

                // Mặc định tắt khóa sổ cho đến khi load dữ liệu thực
                IsSubjectLocked = false;
                IsClassLockedByGVCN = false;
            }
        }

        public SubjectGradebookViewModel()
        {
            LoadCoefficients();
            LoadSubjectsForCurrentTeacher();
        }

        private void LoadSubjectsForCurrentTeacher()
        {
            Subjects.Clear();
            if (CurrentUser.Instance == null) return;

            string query = @"
                SELECT DISTINCT s.SubjectID, s.SubjectName 
                FROM TeachingAssignment ta
                JOIN Subject s ON ta.SubjectID = s.SubjectID
                JOIN Employee e ON ta.EmployeeID = e.EmployeeID
                WHERE e.AccountID = @AccountID 
                  AND ta.Semester = @Semester 
                  AND ta.AcademicYear = @AcademicYear";

            SqlParameter[] paras = {
                new SqlParameter("@AccountID", CurrentUser.Instance.UserId),
                new SqlParameter("@Semester", SelectedSemester),
                new SqlParameter("@AcademicYear", SelectedAcademicYear)
            };

            DataTable dt = DatabaseHelper.ExecuteQuery(query, paras);

            foreach (DataRow row in dt.Rows)
            {
                Subjects.Add(new ComboBoxItem { Id = Convert.ToInt32(row["SubjectID"]), Name = row["SubjectName"].ToString() });
            }
        }

        private void LoadClassesForSubject(ComboBoxItem subject)
        {
            if (subject == null || CurrentUser.Instance == null) return;

            string query = @"
                SELECT DISTINCT c.ClassID, c.ClassName 
                FROM TeachingAssignment ta
                JOIN Class c ON ta.ClassID = c.ClassID
                JOIN Employee e ON ta.EmployeeID = e.EmployeeID
                WHERE e.AccountID = @AccountID 
                  AND ta.SubjectID = @SubjectID
                  AND ta.Semester = @Semester 
                  AND ta.AcademicYear = @AcademicYear";

            SqlParameter[] paras = {
                new SqlParameter("@AccountID", CurrentUser.Instance.UserId),
                new SqlParameter("@SubjectID", subject.Id),
                new SqlParameter("@Semester", SelectedSemester),
                new SqlParameter("@AcademicYear", SelectedAcademicYear)
            };

            DataTable dt = DatabaseHelper.ExecuteQuery(query, paras);

            foreach (DataRow row in dt.Rows)
            {
                Classes.Add(new ComboBoxItem { Id = Convert.ToInt32(row["ClassID"]), Name = row["ClassName"].ToString() });
            }
        }

        private void LoadCoefficients()
        {
            try
            {
                string query = "SELECT ParameterName, Value FROM Parameter WHERE ParameterName IN ('RegularScoreCoefficient', 'MidtermScoreCoefficient', 'FinalScoreCoefficient')";
                DataTable dt = DatabaseHelper.ExecuteQuery(query);
                foreach (DataRow row in dt.Rows)
                {
                    string name = row["ParameterName"].ToString();
                    double val = Convert.ToDouble(row["Value"]);

                    if (name == "RegularScoreCoefficient") _regCoef = val;
                    else if (name == "MidtermScoreCoefficient") _midCoef = val;
                    else if (name == "FinalScoreCoefficient") _finCoef = val;
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError($"Lỗi truy xuất quy định:\n{ex.Message}");
            }
        }

        // BỔ SUNG HÀM KIỂM TRA KHÓA SỔ TỔNG HỢP
        private void CheckLockStatus()
        {
            if (SelectedClass == null || SelectedSubject == null) return;

            try
            {
                // Check GVCN khóa
                string classLockQuery = "SELECT IsLocked FROM ClassReport WHERE ClassID = @ClassID AND Semester = @Semester AND AcademicYear = @AcademicYear";
                SqlParameter[] classParams = {
                    new SqlParameter("@ClassID", SelectedClass.Id),
                    new SqlParameter("@Semester", SelectedSemester),
                    new SqlParameter("@AcademicYear", SelectedAcademicYear)
                };
                DataTable dtClass = DatabaseHelper.ExecuteQuery(classLockQuery, classParams);
                IsClassLockedByGVCN = dtClass.Rows.Count > 0 && dtClass.Rows[0]["IsLocked"] != DBNull.Value && Convert.ToBoolean(dtClass.Rows[0]["IsLocked"]);

                // Check GVBM khóa môn
                string subjectLockQuery = "SELECT IsLocked FROM SubjectReport WHERE ClassID = @ClassID AND SubjectID = @SubjectID AND Semester = @Semester AND AcademicYear = @AcademicYear";
                SqlParameter[] subjectParams = {
                    new SqlParameter("@ClassID", SelectedClass.Id),
                    new SqlParameter("@SubjectID", SelectedSubject.Id),
                    new SqlParameter("@Semester", SelectedSemester),
                    new SqlParameter("@AcademicYear", SelectedAcademicYear)
                };
                DataTable dtSubject = DatabaseHelper.ExecuteQuery(subjectLockQuery, subjectParams);
                IsSubjectLocked = dtSubject.Rows.Count > 0 && dtSubject.Rows[0]["IsLocked"] != DBNull.Value && Convert.ToBoolean(dtSubject.Rows[0]["IsLocked"]);

                // Cập nhật hiển thị nút Lưu Điểm
                IsSaveVisible = (IsClassLockedByGVCN || IsSubjectLocked) ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi kiểm tra trạng thái khóa sổ: " + ex.Message);
            }
        }

        private bool CanLoadGradeData()
        {
            return SelectedClass != null && SelectedSubject != null;
        }

        [RelayCommand(CanExecute = nameof(CanLoadGradeData))]
        private void LoadGradeData()
        {
            if (HasUnsavedChanges)
            {
                bool confirm = NotificationHelper.ShowConfirm("Bạn đang có điểm chưa lưu trên màn hình!\nNếu lấy danh sách mới, các điểm vừa nhập sẽ bị mất. Bạn có chắc chắn tiếp tục không?");
                if (!confirm) return;
            }

            try
            {
                LoadCoefficients();
                StudentGrades.Clear();

                CheckLockStatus(); // Gọi hàm kiểm tra khóa sổ

                if (IsClassLockedByGVCN)
                {
                    NotificationHelper.ShowWarning("Lớp này đã được GVCN lập báo cáo tổng kết!\nBạn chỉ có quyền xem, không thể sửa điểm.");
                }
                else if (IsSubjectLocked)
                {
                    NotificationHelper.ShowWarning("Bạn đã chốt sổ môn này rồi!\nHãy mở khóa môn nếu muốn tiếp tục sửa điểm.");
                }

                GradebookTitle = $"Nhập điểm môn {SelectedSubject.Name} - Lớp {SelectedClass.Name} ({SelectedSemester} - {SelectedAcademicYear})";

                string sqlQuery = @"
                    SELECT 
                        s.StudentID, s.FullName, sc.ScoreID,
                        sc.RegularTestScore, sc.MidTermScore, sc.FinalTermScore
                    FROM Student s
                    JOIN ClassPlacement cp ON s.StudentID = cp.StudentID
                    LEFT JOIN Score sc ON s.StudentID = sc.StudentID 
                                      AND sc.SubjectID = @SubjectID 
                                      AND sc.Semester = @Semester 
                                      AND sc.AcademicYear = @AcademicYear
                    WHERE cp.ClassID = @ClassID
                    ORDER BY s.FullName";

                SqlParameter[] sqlParams = {
                    new SqlParameter("@SubjectID", SelectedSubject.Id),
                    new SqlParameter("@ClassID", SelectedClass.Id),
                    new SqlParameter("@Semester", SelectedSemester),
                    new SqlParameter("@AcademicYear", SelectedAcademicYear)
                };

                DataTable dt = DatabaseHelper.ExecuteQuery(sqlQuery, sqlParams);

                if (dt == null || dt.Rows.Count == 0)
                {
                    NotificationHelper.ShowWarning("Lớp này hiện chưa có học sinh nào!");
                    return;
                }

                int count = 1;
                foreach (DataRow row in dt.Rows)
                {
                    var hs = new StudentGradeDisplay
                    {
                        OrdinalNumber = count++,
                        StudentID = row["StudentID"].ToString(),
                        FullName = row["FullName"].ToString(),
                        ScoreID = row["ScoreID"] != DBNull.Value ? Convert.ToInt32(row["ScoreID"]) : 0,
                        RegCoef = _regCoef,
                        MidCoef = _midCoef,
                        FinCoef = _finCoef
                    };

                    if (row["RegularTestScore"] != DBNull.Value) hs.RegularScore = Convert.ToDouble(row["RegularTestScore"]);
                    if (row["MidTermScore"] != DBNull.Value) hs.MidSemScore = Convert.ToDouble(row["MidTermScore"]);
                    if (row["FinalTermScore"] != DBNull.Value) hs.FinalScore = Convert.ToDouble(row["FinalTermScore"]);

                    StudentGrades.Add(hs);
                }

                foreach (var hs in StudentGrades)
                {
                    hs.IsDirty = false;
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError($"Lỗi truy xuất hệ thống:\n{ex.Message}");
            }
        }

        [RelayCommand]
        private void SaveGradeData()
        {
            if (StudentGrades.Count == 0) return;

            // Kiểm tra kép trước khi lưu
            CheckLockStatus();
            if (IsClassLockedByGVCN || IsSubjectLocked)
            {
                NotificationHelper.ShowError("Hành động bị từ chối! Bảng điểm này đang trong trạng thái bị khóa.");
                return;
            }

            int successCount = 0;

            foreach (var hs in StudentGrades)
            {
                if (!hs.RegularScore.HasValue && !hs.MidSemScore.HasValue && !hs.FinalScore.HasValue) continue;

                string mergeQuery = @"
                    MERGE Score AS target
                    USING (SELECT @StudentID AS StudentID, @SubjectID AS SubjectID, @Semester AS Semester, @AcademicYear AS AcademicYear) AS source
                    ON (target.StudentID = source.StudentID AND target.SubjectID = source.SubjectID AND target.Semester = source.Semester AND target.AcademicYear = source.AcademicYear)
                    WHEN MATCHED THEN 
                        UPDATE SET RegularTestScore = @Reg, MidTermScore = @Mid, FinalTermScore = @Fin
                    WHEN NOT MATCHED THEN
                        INSERT (StudentID, SubjectID, Semester, AcademicYear, RegularTestScore, MidTermScore, FinalTermScore)
                        VALUES (@StudentID, @SubjectID, @Semester, @AcademicYear, @Reg, @Mid, @Fin);";

                SqlParameter[] parameters = {
                    new SqlParameter("@StudentID", hs.StudentID),
                    new SqlParameter("@SubjectID", SelectedSubject.Id),
                    new SqlParameter("@Semester", SelectedSemester),
                    new SqlParameter("@AcademicYear", SelectedAcademicYear),
                    new SqlParameter("@Reg", hs.RegularScore ?? (object)DBNull.Value),
                    new SqlParameter("@Mid", hs.MidSemScore ?? (object)DBNull.Value),
                    new SqlParameter("@Fin", hs.FinalScore ?? (object)DBNull.Value)
                };

                try
                {
                    DatabaseHelper.ExecuteNonQuery(mergeQuery, parameters);
                    successCount++;
                }
                catch { /* Nuốt lỗi cục bộ để lưu tiếp các row sau */ }
            }

            if (successCount > 0)
            {
                foreach (var hs in StudentGrades)
                {
                    hs.IsDirty = false;
                }

                NotificationHelper.ShowSuccess($"Đã lưu thành công điểm của {successCount} học sinh!");
                LoadGradeData(); // Nạp lại DataGrid để lấy ScoreID mới cập nhật
            }
            else
            {
                NotificationHelper.ShowWarning("Không có thay đổi điểm số nào được lưu.");
            }
        }

        public void RefreshData()
        {
            StudentGrades.Clear();
            ReportData.Clear();
            DetailedStudentList.Clear();
            IsSubjectLocked = false;
            IsClassLockedByGVCN = false;

            SelectedSubject = null;
            SelectedClass = null;

            Classes.Clear();

            LoadCoefficients();
            LoadSubjectsForCurrentTeacher();

            GradebookTitle = "Vui lòng chọn Lớp và Môn học";
            IsSaveVisible = Visibility.Hidden;
        }

        // ====================================================================
        // === KHU VỰC LOGIC BÁO CÁO MÔN HỌC (SUBJECT REPORT) ===
        // ====================================================================

        public partial class SubjectReportRow : ObservableObject
        {
            [ObservableProperty] private int _orderNumber;
            [ObservableProperty] private int _classId;
            [ObservableProperty] private string _className;
            [ObservableProperty] private int _totalStudents;
            [ObservableProperty] private int _passedCount;
            [ObservableProperty] private double _passRate;
            [ObservableProperty] private bool _isMissingScores;
            [ObservableProperty] private bool _isSubjectLocked;
            [ObservableProperty] private bool _isClassLockedByGVCN;
        }

        public class SubjectReportDetailRow
        {
            public int OrderNumber { get; set; }
            public string FullName { get; set; }
            public double AverageScore { get; set; }
            public string Result { get; set; }
        }

        [ObservableProperty] private ObservableCollection<SubjectReportRow> _reportData = new();
        [ObservableProperty] private ObservableCollection<SubjectReportDetailRow> _detailedStudentList = new();
        [ObservableProperty] private SubjectReportRow _selectedReportRow;

        [RelayCommand]
        private void GenerateReport()
        {
            if (SelectedSubject == null)
            {
                NotificationHelper.ShowWarning("Vui lòng chọn Môn học để xem báo cáo!");
                return;
            }

            try
            {
                ReportData.Clear();
                DetailedStudentList.Clear();

                double passingGrade = 5.0;
                DataTable dtParam = DatabaseHelper.ExecuteQuery("SELECT Value FROM Parameter WHERE ParameterName = 'NumPassingGrade'");
                if (dtParam.Rows.Count > 0) passingGrade = Convert.ToDouble(dtParam.Rows[0]["Value"]);

                LoadCoefficients();
                double totalCoef = _regCoef + _midCoef + _finCoef;

                int orderNumber = 1;

                // Vòng lặp quét TẤT CẢ CÁC LỚP mà giáo viên dạy môn này
                foreach (var cls in Classes)
                {
                    bool isClassLocked = false;
                    bool isSubjectLocked = false;
                    bool isMissing = false;

                    // 1. Kiểm tra trạng thái khóa sổ của Lớp này
                    string classLockQuery = "SELECT IsLocked FROM ClassReport WHERE ClassID = @ClassID AND Semester = @Semester AND AcademicYear = @AcademicYear";
                    DataTable dtClassLock = DatabaseHelper.ExecuteQuery(classLockQuery, new[] {
                        new SqlParameter("@ClassID", cls.Id), new SqlParameter("@Semester", SelectedSemester), new SqlParameter("@AcademicYear", SelectedAcademicYear)
                    });
                    if (dtClassLock.Rows.Count > 0 && dtClassLock.Rows[0]["IsLocked"] != DBNull.Value) isClassLocked = Convert.ToBoolean(dtClassLock.Rows[0]["IsLocked"]);

                    string subjectLockQuery = "SELECT IsLocked FROM SubjectReport WHERE ClassID = @ClassID AND SubjectID = @SubjectID AND Semester = @Semester AND AcademicYear = @AcademicYear";
                    DataTable dtSubjectLock = DatabaseHelper.ExecuteQuery(subjectLockQuery, new[] {
                        new SqlParameter("@ClassID", cls.Id), new SqlParameter("@SubjectID", SelectedSubject.Id), new SqlParameter("@Semester", SelectedSemester), new SqlParameter("@AcademicYear", SelectedAcademicYear)
                    });
                    if (dtSubjectLock.Rows.Count > 0 && dtSubjectLock.Rows[0]["IsLocked"] != DBNull.Value) isSubjectLocked = Convert.ToBoolean(dtSubjectLock.Rows[0]["IsLocked"]);

                    // 2. Query điểm số
                    string query = @"
                        SELECT s.StudentID, s.FullName, sc.RegularTestScore, sc.MidTermScore, sc.FinalTermScore
                        FROM Student s
                        JOIN ClassPlacement cp ON s.StudentID = cp.StudentID
                        LEFT JOIN Score sc ON s.StudentID = sc.StudentID 
                                          AND sc.SubjectID = @SubjectID 
                                          AND sc.Semester = @Semester 
                                          AND sc.AcademicYear = @AcademicYear
                        WHERE cp.ClassID = @ClassID";

                    SqlParameter[] paras = {
                        new SqlParameter("@SubjectID", SelectedSubject.Id),
                        new SqlParameter("@ClassID", cls.Id),
                        new SqlParameter("@Semester", SelectedSemester),
                        new SqlParameter("@AcademicYear", SelectedAcademicYear)
                    };

                    DataTable dt = DatabaseHelper.ExecuteQuery(query, paras);

                    int passCount = 0;
                    int totalStudents = dt.Rows.Count;

                    if (totalStudents == 0) continue; // Lớp chưa có học sinh thì bỏ qua

                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["RegularTestScore"] == DBNull.Value || row["MidTermScore"] == DBNull.Value || row["FinalTermScore"] == DBNull.Value)
                        {
                            isMissing = true;
                        }

                        double r = row["RegularTestScore"] != DBNull.Value ? Convert.ToDouble(row["RegularTestScore"]) : 0;
                        double m = row["MidTermScore"] != DBNull.Value ? Convert.ToDouble(row["MidTermScore"]) : 0;
                        double f = row["FinalTermScore"] != DBNull.Value ? Convert.ToDouble(row["FinalTermScore"]) : 0;

                        double avg = 0;
                        if (totalCoef > 0) avg = Math.Round((r * _regCoef + m * _midCoef + f * _finCoef) / totalCoef, 1);

                        if (avg >= passingGrade && !isMissing) passCount++;
                    }

                    // 3. Đưa vào bảng tổng kết
                    ReportData.Add(new SubjectReportRow
                    {
                        OrderNumber = orderNumber++,
                        ClassId = cls.Id,
                        ClassName = cls.Name,
                        TotalStudents = totalStudents,
                        PassedCount = isMissing ? 0 : passCount,
                        PassRate = (totalStudents > 0 && !isMissing) ? Math.Round((double)passCount / totalStudents * 100, 2) : 0,
                        IsMissingScores = isMissing,
                        IsSubjectLocked = isSubjectLocked,
                        IsClassLockedByGVCN = isClassLocked
                    });
                }

                if (ReportData.Count == 0)
                {
                    NotificationHelper.ShowWarning("Không có dữ liệu báo cáo cho môn học và học kỳ này.");
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi hệ thống khi lập báo cáo:\n" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task ViewDetail()
        {
            if (SelectedReportRow == null) return;

            // Theo đặc tả: Thiếu điểm thì cấm lập báo cáo/xem chi tiết
            if (SelectedReportRow.IsMissingScores)
            {
                NotificationHelper.ShowWarning("Lớp này chưa hoàn tất nhập điểm, không thể xem chi tiết!");
                return;
            }

            try
            {
                DetailedStudentList.Clear();
                double passingGrade = 5.0;
                DataTable dtParam = DatabaseHelper.ExecuteQuery("SELECT Value FROM Parameter WHERE ParameterName = 'NumPassingGrade'");
                if (dtParam.Rows.Count > 0) passingGrade = Convert.ToDouble(dtParam.Rows[0]["Value"]);

                LoadCoefficients();
                double totalCoef = _regCoef + _midCoef + _finCoef;

                // Query lại chi tiết cho cái ClassID đang được double-click
                string query = @"
                    SELECT s.FullName, sc.RegularTestScore, sc.MidTermScore, sc.FinalTermScore
                    FROM Student s
                    JOIN ClassPlacement cp ON s.StudentID = cp.StudentID
                    LEFT JOIN Score sc ON s.StudentID = sc.StudentID 
                                      AND sc.SubjectID = @SubjectID 
                                      AND sc.Semester = @Semester 
                                      AND sc.AcademicYear = @AcademicYear
                    WHERE cp.ClassID = @ClassID
                    ORDER BY s.FullName";

                SqlParameter[] paras = {
                    new SqlParameter("@SubjectID", SelectedSubject.Id),
                    new SqlParameter("@ClassID", SelectedReportRow.ClassId),
                    new SqlParameter("@Semester", SelectedSemester),
                    new SqlParameter("@AcademicYear", SelectedAcademicYear)
                };

                DataTable dt = DatabaseHelper.ExecuteQuery(query, paras);
                int orderNumber = 1;

                foreach (DataRow row in dt.Rows)
                {
                    double r = row["RegularTestScore"] != DBNull.Value ? Convert.ToDouble(row["RegularTestScore"]) : 0;
                    double m = row["MidTermScore"] != DBNull.Value ? Convert.ToDouble(row["MidTermScore"]) : 0;
                    double f = row["FinalTermScore"] != DBNull.Value ? Convert.ToDouble(row["FinalTermScore"]) : 0;

                    double avg = 0;
                    if (totalCoef > 0) avg = Math.Round((r * _regCoef + m * _midCoef + f * _finCoef) / totalCoef, 1);
                    bool isPass = avg >= passingGrade;

                    DetailedStudentList.Add(new SubjectReportDetailRow
                    {
                        OrderNumber = orderNumber++,
                        FullName = row["FullName"].ToString(),
                        AverageScore = avg,
                        Result = isPass ? "Đạt" : "Không đạt"
                    });
                }

                var detailDialog = new WPF_Student_Management.Components.SubjectReportDetailUC { DataContext = this };
                await MaterialDesignThemes.Wpf.DialogHost.Show(detailDialog, "RootDialog");
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi khi mở bảng chi tiết:\n" + ex.Message);
            }
        }

        // --- LỆNH CHỐT SỔ MÔN HỌC (Nhận Parameter là Dòng đang thao tác) ---
        [RelayCommand]
        private void ConfirmSubjectReport(SubjectReportRow row)
        {
            if (SelectedSubject == null || row == null || row.IsMissingScores) return;
            if (CurrentUser.Instance == null) return;

            try
            {
                int currentAccountId = CurrentUser.Instance.UserId;
                
                string query = @"
                DECLARE @EmpID INT = (SELECT TOP 1 EmployeeID FROM Employee WHERE AccountID = @AccountID);

                IF EXISTS (SELECT 1 FROM SubjectReport WHERE ClassID = @ClassID AND SubjectID = @SubjectID AND Semester = @Semester AND AcademicYear = @AcademicYear)
                BEGIN
                    UPDATE SubjectReport 
                    SET IsLocked = 1, TotalStudents = @Total, PassedStudents = @Pass, PassRate = @Rate 
                    WHERE ClassID = @ClassID AND SubjectID = @SubjectID AND Semester = @Semester AND AcademicYear = @AcademicYear
                END
                ELSE
                BEGIN
                    INSERT INTO SubjectReport (ClassID, SubjectID, Semester, AcademicYear, TotalStudents, PassedStudents, PassRate, IsLocked, CreatedByTeacherID, CreatedAt)
                    VALUES (@ClassID, @SubjectID, @Semester, @AcademicYear, @Total, @Pass, @Rate, 1, @EmpID, GETDATE())
                END";

                SqlParameter[] paras = {
                    new SqlParameter("@ClassID", row.ClassId),
                    new SqlParameter("@SubjectID", SelectedSubject.Id),
                    new SqlParameter("@Semester", SelectedSemester),
                    new SqlParameter("@AcademicYear", SelectedAcademicYear),
                    new SqlParameter("@Total", row.TotalStudents),
                    new SqlParameter("@Pass", row.PassedCount),
                    new SqlParameter("@Rate", row.PassRate),
                    new SqlParameter("@AccountID", currentAccountId)
                };

                DatabaseHelper.ExecuteNonQuery(query, paras);

                // Cập nhật State để UI nháy tự động sang trạng thái "Đã chốt"
                row.IsSubjectLocked = true;

                // Đồng bộ cập nhật nút Lưu ở tab Nhập Điểm (Nếu user đang xem lớp này)
                if (SelectedClass != null && SelectedClass.Id == row.ClassId) { CheckLockStatus(); }

                NotificationHelper.ShowSuccess($"Đã lập báo cáo môn học cho lớp {row.ClassName} thành công!");
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi lập báo cáo: " + ex.Message);
            }
        }

        // --- LỆNH MỞ KHÓA MÔN HỌC ---
        [RelayCommand]
        private void CancelSubjectReport(SubjectReportRow row)
        {
            if (SelectedSubject == null || row == null) return;

            try
            {
                string query = "UPDATE SubjectReport SET IsLocked = 0 WHERE ClassID = @ClassID AND SubjectID = @SubjectID AND Semester = @Semester AND AcademicYear = @AcademicYear";

                SqlParameter[] paras = {
                    new SqlParameter("@ClassID", row.ClassId),
                    new SqlParameter("@SubjectID", SelectedSubject.Id),
                    new SqlParameter("@Semester", SelectedSemester),
                    new SqlParameter("@AcademicYear", SelectedAcademicYear)
                };

                DatabaseHelper.ExecuteNonQuery(query, paras);

                // Cập nhật State để UI nháy tự động sang trạng thái "Chưa chốt"
                row.IsSubjectLocked = false;

                if (SelectedClass != null && SelectedClass.Id == row.ClassId) { CheckLockStatus(); }

                NotificationHelper.ShowSuccess($"Đã mở khóa sổ môn học cho lớp {row.ClassName}!");
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi mở khóa: " + ex.Message);
            }
        }
    }
}