using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
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
                GradebookTitle = "Vui lòng chọn Lớp học";
                IsSaveVisible = Visibility.Hidden;

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
                IsSaveVisible = Visibility.Hidden;
                GradebookTitle = value != null ? "Vui lòng bấm 'Lấy danh sách'" : "Vui lòng chọn Lớp học";
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

            // SỬA: Lọc môn học theo Học kỳ và Năm học hiện tại
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

            // SỬA: Lọc lớp học theo Học kỳ và Năm học hiện tại
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

                // SỬA: Kiểm tra khóa sổ từ bảng ClassReport (Khóa theo Học Kỳ/Năm học)
                string lockQuery = "SELECT IsLocked FROM ClassReport WHERE ClassID = @ClassID AND Semester = @Semester AND AcademicYear = @AcademicYear";
                SqlParameter[] lockParams = {
                    new SqlParameter("@ClassID", SelectedClass.Id),
                    new SqlParameter("@Semester", SelectedSemester),
                    new SqlParameter("@AcademicYear", SelectedAcademicYear)
                };
                DataTable dtLock = DatabaseHelper.ExecuteQuery(lockQuery, lockParams);

                bool isLocked = dtLock.Rows.Count > 0 && dtLock.Rows[0]["IsLocked"] != DBNull.Value && Convert.ToBoolean(dtLock.Rows[0]["IsLocked"]);

                if (isLocked)
                {
                    IsSaveVisible = Visibility.Collapsed;
                    NotificationHelper.ShowWarning("Lớp này đã được GVCN lập báo cáo tổng kết!\nBạn chỉ có quyền xem, không thể sửa điểm.");
                }
                else
                {
                    IsSaveVisible = Visibility.Visible;
                }

                GradebookTitle = $"Nhập điểm môn {SelectedSubject.Name} - Lớp {SelectedClass.Name} ({SelectedSemester} - {SelectedAcademicYear})";

                // SỬA: Join bảng Score bám theo Semester và AcademicYear
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

            // Check khóa sổ một lần nữa đề phòng rủi ro Concurrency
            string lockQuery = "SELECT IsLocked FROM ClassReport WHERE ClassID = @ClassID AND Semester = @Semester AND AcademicYear = @AcademicYear";
            SqlParameter[] lockParams = {
                new SqlParameter("@ClassID", SelectedClass.Id),
                new SqlParameter("@Semester", SelectedSemester),
                new SqlParameter("@AcademicYear", SelectedAcademicYear)
            };
            DataTable dtLock = DatabaseHelper.ExecuteQuery(lockQuery, lockParams);

            if (dtLock.Rows.Count > 0 && Convert.ToBoolean(dtLock.Rows[0]["IsLocked"]))
            {
                IsSaveVisible = Visibility.Collapsed;
                NotificationHelper.ShowError("Hành động bị từ chối! Lớp này đã được GVCN lập báo cáo chốt sổ.");
                return;
            }

            int successCount = 0;

            foreach (var hs in StudentGrades)
            {
                if (!hs.RegularScore.HasValue && !hs.MidSemScore.HasValue && !hs.FinalScore.HasValue) continue;

                // SỬA: Lệnh MERGE check đầy đủ 4 key (StudentID, SubjectID, Semester, AcademicYear)
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

        public class SubjectReportRow
        {
            public int OrderNumber { get; set; }
            public string ClassName { get; set; }
            public int TotalStudents { get; set; }
            public int PassedCount { get; set; }
            public double PassRate { get; set; }
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
            if (SelectedSubject == null || SelectedClass == null)
            {
                NotificationHelper.ShowWarning("Vui lòng chọn Môn học và Lớp để xem báo cáo!");
                return;
            }

            try
            {
                ReportData.Clear();
                DetailedStudentList.Clear();

                // 1. Lấy quy định "Điểm Đạt" từ hệ thống (Mặc định 5.0 nếu không có)
                double passingGrade = 5.0;
                DataTable dtParam = DatabaseHelper.ExecuteQuery("SELECT Value FROM Parameter WHERE ParameterName = 'NumPassingGrade'");
                if (dtParam.Rows.Count > 0) passingGrade = Convert.ToDouble(dtParam.Rows[0]["Value"]);

                // Đảm bảo lấy hệ số mới nhất
                LoadCoefficients();
                double totalCoef = _regCoef + _midCoef + _finCoef;

                // 2. Lấy dữ liệu điểm của lớp bám theo Học kỳ và Năm học
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
                    new SqlParameter("@ClassID", SelectedClass.Id),
                    new SqlParameter("@Semester", SelectedSemester),
                    new SqlParameter("@AcademicYear", SelectedAcademicYear)
                };

                DataTable dt = DatabaseHelper.ExecuteQuery(query, paras);

                if (dt.Rows.Count == 0)
                {
                    NotificationHelper.ShowWarning("Không có dữ liệu học sinh trong lớp này!");
                    return;
                }

                int passCount = 0;
                int totalStudents = dt.Rows.Count;
                int orderNumber = 1;

                foreach (DataRow row in dt.Rows)
                {
                    double r = row["RegularTestScore"] != DBNull.Value ? Convert.ToDouble(row["RegularTestScore"]) : 0;
                    double m = row["MidTermScore"] != DBNull.Value ? Convert.ToDouble(row["MidTermScore"]) : 0;
                    double f = row["FinalTermScore"] != DBNull.Value ? Convert.ToDouble(row["FinalTermScore"]) : 0;

                    double avg = 0;
                    if (row["RegularTestScore"] != DBNull.Value || row["MidTermScore"] != DBNull.Value || row["FinalTermScore"] != DBNull.Value)
                    {
                        if (totalCoef > 0)
                            avg = Math.Round((r * _regCoef + m * _midCoef + f * _finCoef) / totalCoef, 1);
                    }

                    bool isPass = avg >= passingGrade;
                    if (isPass) passCount++;

                    DetailedStudentList.Add(new SubjectReportDetailRow
                    {
                        OrderNumber = orderNumber++,
                        FullName = row["FullName"].ToString(),
                        AverageScore = avg,
                        Result = isPass ? "Đạt" : "Không đạt"
                    });
                }

                // 3. Đổ dữ liệu tổng kết vào Bảng 1
                var summaryRow = new SubjectReportRow
                {
                    OrderNumber = 1,
                    ClassName = SelectedClass.Name,
                    TotalStudents = totalStudents,
                    PassedCount = passCount,
                    PassRate = totalStudents > 0 ? Math.Round((double)passCount / totalStudents * 100, 2) : 0
                };

                ReportData.Add(summaryRow);
                SelectedReportRow = summaryRow; // Tự động select để hiện tiêu đề bảng 2
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi hệ thống khi lập báo cáo:\n" + ex.Message);
            }
        }
    }
}