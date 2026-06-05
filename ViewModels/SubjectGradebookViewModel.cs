using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;

namespace WPF_Student_Management.ViewModels
{
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

        [ObservableProperty] private bool _hasPendingApplication = false;

        private double? _tx1;
        public double? TX1 { get => _tx1; set { if (value.HasValue) { if (value.Value < 0) value = 0; if (value.Value > 10) value = 10; } if (SetProperty(ref _tx1, value)) { IsDirty = true; OnPropertyChanged(nameof(AverageScore)); } } }

        private double? _tx2;
        public double? TX2 { get => _tx2; set { if (value.HasValue) { if (value.Value < 0) value = 0; if (value.Value > 10) value = 10; } if (SetProperty(ref _tx2, value)) { IsDirty = true; OnPropertyChanged(nameof(AverageScore)); } } }

        private double? _tx3;
        public double? TX3 { get => _tx3; set { if (value.HasValue) { if (value.Value < 0) value = 0; if (value.Value > 10) value = 10; } if (SetProperty(ref _tx3, value)) { IsDirty = true; OnPropertyChanged(nameof(AverageScore)); } } }

        private double? _tx4;
        public double? TX4 { get => _tx4; set { if (value.HasValue) { if (value.Value < 0) value = 0; if (value.Value > 10) value = 10; } if (SetProperty(ref _tx4, value)) { IsDirty = true; OnPropertyChanged(nameof(AverageScore)); } } }

        private double? _midSemScore;
        public double? MidSemScore
        {
            get => _midSemScore;
            set
            {
                if (value.HasValue)
                { if (value.Value < 0) value = 0; if (value.Value > 10) value = 10; }
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
                if (value.HasValue)
                { if (value.Value < 0) value = 0; if (value.Value > 10) value = 10; }
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
                double sumTX = 0;
                int countTX = 0;
                if (TX1.HasValue)
                { sumTX += TX1.Value; countTX++; }
                if (TX2.HasValue)
                { sumTX += TX2.Value; countTX++; }
                if (TX3.HasValue)
                { sumTX += TX3.Value; countTX++; }
                if (TX4.HasValue)
                { sumTX += TX4.Value; countTX++; }

                double sumGK = MidSemScore ?? 0;
                int countGK = MidSemScore.HasValue ? 1 : 0;

                double sumCK = FinalScore ?? 0;
                int countCK = FinalScore.HasValue ? 1 : 0;

                if (countTX == 0 && countGK == 0 && countCK == 0)
                    return null;

                double totalDenominator = (countTX * RegCoef) + (countGK * MidCoef) + (countCK * FinCoef);
                if (totalDenominator == 0)
                    return 0;

                return Math.Round((sumTX * RegCoef + sumGK * MidCoef + sumCK * FinCoef) / totalDenominator, 1);
            }
        }
    }

    public class ComboBoxItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() => Name;
    }

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
                if (_selectedSubject == value)
                    return;

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
                if (_selectedClass == value)
                    return;

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
            if (CurrentUser.Instance == null)
                return;

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
            if (subject == null || CurrentUser.Instance == null)
                return;

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

                    if (name == "RegularScoreCoefficient")
                        _regCoef = val;
                    else if (name == "MidtermScoreCoefficient")
                        _midCoef = val;
                    else if (name == "FinalScoreCoefficient")
                        _finCoef = val;
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError($"Lỗi truy xuất quy định:\n{ex.Message}");
            }
        }

        private void CheckLockStatus()
        {
            if (SelectedClass == null || SelectedSubject == null)
                return;

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
            ExecuteLoadGradeData(false);
        }

        private void ExecuteLoadGradeData(bool silent)
        {
            if (HasUnsavedChanges)
            {
                bool confirm = NotificationHelper.ShowConfirm("Bạn đang có điểm chưa lưu trên màn hình!\nNếu lấy danh sách mới, các điểm vừa nhập sẽ bị mất. Bạn có chắc chắn tiếp tục không?");
                if (!confirm)
                    return;
            }

            try
            {
                LoadCoefficients();
                StudentGrades.Clear();

                CheckLockStatus(); // Gọi hàm kiểm tra khóa sổ

                // Chỉ hiện cảnh báo nếu silent = false (người dùng tự bấm nút Lấy danh sách)
                if (!silent)
                {
                    if (IsClassLockedByGVCN)
                        NotificationHelper.ShowWarning("Lớp này đã được GVCN lập báo cáo tổng kết!\nBạn chỉ có quyền xem, không thể sửa điểm.");
                    else if (IsSubjectLocked)
                        NotificationHelper.ShowWarning("Bạn đã chốt sổ môn này rồi!\nHãy mở khóa môn nếu muốn tiếp tục sửa điểm.");
                }

                GradebookTitle = $"Nhập điểm môn {SelectedSubject.Name} - Lớp {SelectedClass.Name} ({SelectedSemester} - {SelectedAcademicYear})";

                string sqlQuery = @"
                    SELECT 
                        s.StudentID, s.FullName, sc.ScoreID,
                        sc.RegularScore1, sc.RegularScore2, sc.RegularScore3, sc.RegularScore4, 
                        sc.MidTermScore, sc.FinalTermScore
                    FROM Student s
                    JOIN ClassPlacement cp ON s.StudentID = cp.StudentID
                    LEFT JOIN Score sc ON s.StudentID = sc.StudentID 
                                      AND sc.SubjectID = @SubjectID 
                                      AND sc.Semester = @Semester 
                                      AND sc.AcademicYear = @AcademicYear
                    WHERE cp.ClassID = @ClassID AND s.Status = 'Active'
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

                    if (row["RegularScore1"] != DBNull.Value)
                        hs.TX1 = Convert.ToDouble(row["RegularScore1"]);
                    if (row["RegularScore2"] != DBNull.Value)
                        hs.TX2 = Convert.ToDouble(row["RegularScore2"]);
                    if (row["RegularScore3"] != DBNull.Value)
                        hs.TX3 = Convert.ToDouble(row["RegularScore3"]);
                    if (row["RegularScore4"] != DBNull.Value)
                        hs.TX4 = Convert.ToDouble(row["RegularScore4"]);

                    if (row["MidTermScore"] != DBNull.Value)
                        hs.MidSemScore = Convert.ToDouble(row["MidTermScore"]);
                    if (row["FinalTermScore"] != DBNull.Value)
                        hs.FinalScore = Convert.ToDouble(row["FinalTermScore"]);

                    StudentGrades.Add(hs);
                }

                foreach (var hs in StudentGrades)
                {
                    hs.IsDirty = false;

                    string pendingQuery = @"
                        SELECT COUNT(*) 
                        FROM Application 
                        WHERE StudentID = @StudentID AND StatusID IN (1, 2)";
                    DataTable dtPending = DatabaseHelper.ExecuteQuery(pendingQuery,
                        new[] { new SqlParameter("@StudentID", hs.StudentID) });
                    hs.HasPendingApplication = Convert.ToInt32(dtPending.Rows[0][0]) > 0;
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
            if (StudentGrades.Count == 0)
                return;

            CheckLockStatus();
            if (IsClassLockedByGVCN || IsSubjectLocked)
            {
                NotificationHelper.ShowError("Hành động bị từ chối! Bảng điểm này đang trong trạng thái bị khóa.");
                return;
            }

            // Chặn xung đột: nếu giáo viên mở sổ điểm từ trước nhưng môn đã bị xóa sau đó thì không cho lưu nữa.
            string subjectActiveQuery = "SELECT COUNT(*) FROM Subject WHERE SubjectID = @SubjectID AND IsDeleted = 0";
            DataTable dtSubjectActive = DatabaseHelper.ExecuteQuery(subjectActiveQuery, new[] { new SqlParameter("@SubjectID", SelectedSubject.Id) });
            if (dtSubjectActive.Rows.Count == 0 || Convert.ToInt32(dtSubjectActive.Rows[0][0]) == 0)
            {
                NotificationHelper.ShowError("Môn học này đã bị xóa hoặc ngưng sử dụng. Không thể tiếp tục chỉnh sửa điểm.");
                RefreshData();
                return;
            }

            int successCount = 0;
            List<string> failedRows = new();

            foreach (var hs in StudentGrades)
            {
                if (!hs.IsDirty)
                    continue; //Chỉ chạy DB cho những em có điểm bị sửa
                if (!hs.TX1.HasValue && !hs.TX2.HasValue && !hs.TX3.HasValue && !hs.TX4.HasValue && !hs.MidSemScore.HasValue && !hs.FinalScore.HasValue)
                    continue;
                string checkStudentQuery = @"
                    SELECT s.Status, 
                           (SELECT COUNT(*) FROM Application a WHERE a.StudentID = s.StudentID AND a.StatusID IN (1, 2)) AS PendingCount
                    FROM Student s 
                    WHERE s.StudentID = @StudentID";

                DataTable dtStudent = DatabaseHelper.ExecuteQuery(checkStudentQuery, new[] { new SqlParameter("@StudentID", hs.StudentID) });

                if (dtStudent.Rows.Count > 0)
                {
                    //Chặn nếu đã nghỉ học hoặc chuyển đi
                    if (dtStudent.Rows[0]["Status"].ToString() == "Inactive")
                    {
                        NotificationHelper.ShowError($"Thao tác thất bại!\nHọc sinh {hs.FullName} đã thôi học hoặc chuyển lớp. Dữ liệu điểm của học sinh này không thể cập nhật.");
                        foreach (var student in StudentGrades)
                            student.IsDirty = false;
                        ExecuteLoadGradeData(true);
                        return;
                    }

                    //Chặn nếu đang có đơn xin Chuyển lớp/Thôi học chờ xử lý
                    int pendingCount = Convert.ToInt32(dtStudent.Rows[0]["PendingCount"]);
                    if (pendingCount > 0)
                    {
                        NotificationHelper.ShowError($"Thao tác thất bại!\nHọc sinh {hs.FullName} đang có đơn xin chuyển lớp/thôi học chưa xử lý xong.\nHệ thống tạm thời KHÓA BĂNG điểm của học sinh này cho đến khi đơn được duyệt hoặc từ chối.");
                        foreach (var student in StudentGrades)
                            student.IsDirty = false;
                        ExecuteLoadGradeData(true);
                        return;
                    }
                }

                // nếu môn bị xóa sau bước pre-check thì vẫn chặn được lúc lưu.
                string mergeQuery = @"
                    IF EXISTS (SELECT 1 FROM Subject WHERE SubjectID = @SubjectID AND IsDeleted = 0)
                    BEGIN
                        MERGE Score AS target
                        USING (SELECT @StudentID AS StudentID, @SubjectID AS SubjectID, @Semester AS Semester, @AcademicYear AS AcademicYear) AS source
                        ON (target.StudentID = source.StudentID AND target.SubjectID = source.SubjectID AND target.Semester = source.Semester AND target.AcademicYear = source.AcademicYear)
                        WHEN MATCHED THEN
                            UPDATE SET RegularScore1 = @TX1, RegularScore2 = @TX2, RegularScore3 = @TX3, RegularScore4 = @TX4, 
                                       MidTermScore = @Mid, FinalTermScore = @Fin, AverageScore = @Avg
                        WHEN NOT MATCHED THEN
                            INSERT (StudentID, SubjectID, Semester, AcademicYear, RegularScore1, RegularScore2, RegularScore3, RegularScore4, MidTermScore, FinalTermScore, AverageScore)
                            VALUES (@StudentID, @SubjectID, @Semester, @AcademicYear, @TX1, @TX2, @TX3, @TX4, @Mid, @Fin, @Avg);
                    END
                    ELSE
                    BEGIN
                        THROW 51001, N'Môn học đã bị xóa, không thể cập nhật điểm.', 1;
                    END";

                SqlParameter[] parameters = {
                    new SqlParameter("@StudentID", hs.StudentID),
                    new SqlParameter("@SubjectID", SelectedSubject.Id),
                    new SqlParameter("@Semester", SelectedSemester),
                    new SqlParameter("@AcademicYear", SelectedAcademicYear),
                    new SqlParameter("@TX1", hs.TX1 ?? (object)DBNull.Value),
                    new SqlParameter("@TX2", hs.TX2 ?? (object)DBNull.Value),
                    new SqlParameter("@TX3", hs.TX3 ?? (object)DBNull.Value),
                    new SqlParameter("@TX4", hs.TX4 ?? (object)DBNull.Value),
                    new SqlParameter("@Mid", hs.MidSemScore ?? (object)DBNull.Value),
                    new SqlParameter("@Fin", hs.FinalScore ?? (object)DBNull.Value),
                    new SqlParameter("@Avg", hs.AverageScore ?? (object)DBNull.Value) // Bắt buộc gửi Avg xuống DB
                };

                try
                {
                    DatabaseHelper.ExecuteNonQuery(mergeQuery, parameters);
                    successCount++;
                }
                catch (Exception ex)
                {
                    // Phản hồi xung đột: vẫn tiếp tục lưu các dòng khác nhưng ghi nhận rõ học sinh nào bị lỗi và lý do.
                    failedRows.Add($"{hs.FullName}: {ex.Message}");
                }
            }

            if (successCount > 0)
            {
                foreach (var hs in StudentGrades)
                {
                    hs.IsDirty = false;
                }

                NotificationHelper.ShowSuccess($"Đã lưu thành công điểm của {successCount} học sinh!");
                if (failedRows.Count > 0)
                {
                    NotificationHelper.ShowWarning($"Có {failedRows.Count} học sinh chưa lưu được:\n" + string.Join("\n", failedRows.Take(5)));
                }
                ExecuteLoadGradeData(true);
            }
            else
            {
                if (failedRows.Count > 0)
                {
                    NotificationHelper.ShowWarning($"Không có điểm nào được lưu. Lỗi đầu tiên:\n{failedRows.First()}");
                }
                else
                {
                    NotificationHelper.ShowWarning("Không có thay đổi điểm số nào được lưu.");
                }
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
                if (dtParam.Rows.Count > 0)
                    passingGrade = Convert.ToDouble(dtParam.Rows[0]["Value"]);

                LoadCoefficients();
                double totalCoef = _regCoef + _midCoef + _finCoef;

                int orderNumber = 1;

                // Vòng lặp quét các lớp mà giáo viên dạy môn này
                foreach (var cls in Classes)
                {
                    bool isClassLocked = false;
                    bool isSubjectLocked = false;
                    bool isMissing = false;

                    //Kiểm tra trạng thái khóa sổ của Lớp này
                    string classLockQuery = "SELECT IsLocked FROM ClassReport WHERE ClassID = @ClassID AND Semester = @Semester AND AcademicYear = @AcademicYear";
                    DataTable dtClassLock = DatabaseHelper.ExecuteQuery(classLockQuery, new[] {
                        new SqlParameter("@ClassID", cls.Id), new SqlParameter("@Semester", SelectedSemester), new SqlParameter("@AcademicYear", SelectedAcademicYear)
                    });
                    if (dtClassLock.Rows.Count > 0 && dtClassLock.Rows[0]["IsLocked"] != DBNull.Value)
                        isClassLocked = Convert.ToBoolean(dtClassLock.Rows[0]["IsLocked"]);

                    string subjectLockQuery = "SELECT IsLocked FROM SubjectReport WHERE ClassID = @ClassID AND SubjectID = @SubjectID AND Semester = @Semester AND AcademicYear = @AcademicYear";
                    DataTable dtSubjectLock = DatabaseHelper.ExecuteQuery(subjectLockQuery, new[] {
                        new SqlParameter("@ClassID", cls.Id), new SqlParameter("@SubjectID", SelectedSubject.Id), new SqlParameter("@Semester", SelectedSemester), new SqlParameter("@AcademicYear", SelectedAcademicYear)
                    });
                    if (dtSubjectLock.Rows.Count > 0 && dtSubjectLock.Rows[0]["IsLocked"] != DBNull.Value)
                        isSubjectLocked = Convert.ToBoolean(dtSubjectLock.Rows[0]["IsLocked"]);

                    string query = @"
                        SELECT s.StudentID, s.FullName, 
                               sc.RegularScore1, sc.RegularScore2, sc.RegularScore3, sc.RegularScore4, 
                               sc.MidTermScore, sc.FinalTermScore
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

                    if (totalStudents == 0)
                        continue; // Lớp chưa có học sinh thì bỏ qua

                    foreach (DataRow row in dt.Rows)
                    {
                        double sumTX = 0;
                        int countTX = 0;
                        if (row["RegularScore1"] != DBNull.Value)
                        { sumTX += Convert.ToDouble(row["RegularScore1"]); countTX++; }
                        if (row["RegularScore2"] != DBNull.Value)
                        { sumTX += Convert.ToDouble(row["RegularScore2"]); countTX++; }
                        if (row["RegularScore3"] != DBNull.Value)
                        { sumTX += Convert.ToDouble(row["RegularScore3"]); countTX++; }
                        if (row["RegularScore4"] != DBNull.Value)
                        { sumTX += Convert.ToDouble(row["RegularScore4"]); countTX++; }

                        double m = row["MidTermScore"] != DBNull.Value ? Convert.ToDouble(row["MidTermScore"]) : 0;
                        int countGK = row["MidTermScore"] != DBNull.Value ? 1 : 0;

                        double f = row["FinalTermScore"] != DBNull.Value ? Convert.ToDouble(row["FinalTermScore"]) : 0;
                        int countCK = row["FinalTermScore"] != DBNull.Value ? 1 : 0;

                        // Nếu thiếu 1 trong 3 loại điểm (TX, GK, CK) thì coi như chưa hoàn tất
                        if (countTX == 0 || countGK == 0 || countCK == 0)
                        {
                            isMissing = true;
                        }

                        double totalDenominator = (countTX * _regCoef) + (countGK * _midCoef) + (countCK * _finCoef);
                        double avg = 0;
                        if (totalDenominator > 0)
                            avg = Math.Round((sumTX * _regCoef + m * _midCoef + f * _finCoef) / totalDenominator, 1);

                        if (avg >= passingGrade && !isMissing)
                            passCount++;
                    }

                    //Đưa vào bảng tổng kết
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
            if (SelectedReportRow == null)
                return;

            //Thiếu điểm thì cấm lập báo cáo/xem chi tiết
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
                if (dtParam.Rows.Count > 0)
                    passingGrade = Convert.ToDouble(dtParam.Rows[0]["Value"]);

                LoadCoefficients();
                double totalCoef = _regCoef + _midCoef + _finCoef;

                // Query lại chi tiết cho cái ClassID đang được double-click
                string query = @"
                    SELECT s.FullName, 
                           sc.RegularScore1, sc.RegularScore2, sc.RegularScore3, sc.RegularScore4, 
                           sc.MidTermScore, sc.FinalTermScore
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
                    double sumTX = 0;
                    int countTX = 0;
                    if (row["RegularScore1"] != DBNull.Value)
                    { sumTX += Convert.ToDouble(row["RegularScore1"]); countTX++; }
                    if (row["RegularScore2"] != DBNull.Value)
                    { sumTX += Convert.ToDouble(row["RegularScore2"]); countTX++; }
                    if (row["RegularScore3"] != DBNull.Value)
                    { sumTX += Convert.ToDouble(row["RegularScore3"]); countTX++; }
                    if (row["RegularScore4"] != DBNull.Value)
                    { sumTX += Convert.ToDouble(row["RegularScore4"]); countTX++; }

                    double m = row["MidTermScore"] != DBNull.Value ? Convert.ToDouble(row["MidTermScore"]) : 0;
                    int countGK = row["MidTermScore"] != DBNull.Value ? 1 : 0;

                    double f = row["FinalTermScore"] != DBNull.Value ? Convert.ToDouble(row["FinalTermScore"]) : 0;
                    int countCK = row["FinalTermScore"] != DBNull.Value ? 1 : 0;

                    double totalDenominator = (countTX * _regCoef) + (countGK * _midCoef) + (countCK * _finCoef);
                    double avg = 0;
                    if (totalDenominator > 0)
                        avg = Math.Round((sumTX * _regCoef + m * _midCoef + f * _finCoef) / totalDenominator, 1);

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

        [RelayCommand]
        private void ConfirmSubjectReport(SubjectReportRow row)
        {
            if (SelectedSubject == null || row == null || row.IsMissingScores)
                return;
            if (CurrentUser.Instance == null)
                return;

            try
            {
                int currentAccountId = CurrentUser.Instance.UserId;
                string query = @"
                DECLARE @EmpID INT = (SELECT TOP 1 EmployeeID FROM Employee WHERE AccountID = @AccountID);

                IF EXISTS (SELECT 1 FROM SubjectReport WHERE ClassID = @ClassID AND SubjectID = @SubjectID AND Semester = @Semester AND AcademicYear = @AcademicYear)
                BEGIN
                    UPDATE SubjectReport 
                    SET IsLocked = 1,
                        TotalStudents = @Total,
                        PassedStudents = @Pass
                    WHERE ClassID = @ClassID AND SubjectID = @SubjectID AND Semester = @Semester AND AcademicYear = @AcademicYear
                END
                ELSE
                BEGIN
                    INSERT INTO SubjectReport (ClassID, SubjectID, Semester, AcademicYear, TotalStudents, PassedStudents, IsLocked, CreatedByTeacherID, CreatedAt)
                    VALUES (@ClassID, @SubjectID, @Semester, @AcademicYear, @Total, @Pass, 1, @EmpID, GETDATE())
                END";

                SqlParameter[] paras = {
                    new SqlParameter("@ClassID", row.ClassId),
                    new SqlParameter("@SubjectID", SelectedSubject.Id),
                    new SqlParameter("@Semester", SelectedSemester),
                    new SqlParameter("@AcademicYear", SelectedAcademicYear),
                    new SqlParameter("@AccountID", currentAccountId),
                    new SqlParameter("@Total", row.TotalStudents),
                    new SqlParameter("@Pass", row.PassedCount)
                };

                DatabaseHelper.ExecuteNonQuery(query, paras);

                row.IsSubjectLocked = true;
                if (SelectedClass != null && SelectedClass.Id == row.ClassId)
                { CheckLockStatus(); }

                NotificationHelper.ShowSuccess($"Đã lập báo cáo môn học cho lớp {row.ClassName} thành công!");
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi lập báo cáo: " + ex.Message);
            }
        }

        [RelayCommand]
        private void CancelSubjectReport(SubjectReportRow row)
        {
            if (SelectedSubject == null || row == null)
                return;

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

                if (SelectedClass != null && SelectedClass.Id == row.ClassId)
                { CheckLockStatus(); }

                NotificationHelper.ShowSuccess($"Đã mở khóa sổ môn học cho lớp {row.ClassName}!");
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi mở khóa: " + ex.Message);
            }
        }
    }
}