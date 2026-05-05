using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using WPF_Student_Management.Helpers;

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
    }

    // Class hiển thị cho Tab Báo cáo tổng kết
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

        // --- CÁC BIẾN & PROPERTY PHỤC VỤ BÁO CÁO (DOD 1 & 2) ---
        private int _currentClassId = 0;

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

        public HomeroomDashboardViewModel()
        {
            GenderList = new ObservableCollection<string> { "Tất cả", "Nam", "Nữ" };    

            GenerateReportCommand = new RelayCommand(ExecuteGenerateReport, CanExecuteReportActions);
            ConfirmReportCommand = new RelayCommand(ExecuteConfirmReport, CanExecuteReportActions);
            CancelReportCommand = new RelayCommand(ExecuteCancelReport, CanExecuteReportActions);
            ViewDetailCommand = new RelayCommand<object>(ExecuteViewDetail, CanExecuteReportActions);

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

                // Cập nhật lấy thêm ClassID và IsLocked
                string query = @"
            SELECT 
                c.ClassID, c.IsLocked,
                s.StudentID, s.FullName, s.Gender, c.ClassName,
                AVG(sc.AverageScore) as OverallAverage,
                COUNT(sc.SubjectID) as GradedCount,
                (SELECT COUNT(*) FROM Subject WHERE IsDeleted = 0) as TotalSubjects
            FROM Student s
            JOIN ClassPlacement cp ON s.StudentID = cp.StudentID
            JOIN Class c ON cp.ClassID = c.ClassID
            JOIN Employee e ON c.HomeroomTeacherID = e.EmployeeID
            JOIN Account a ON e.AccountID = a.AccountID
            LEFT JOIN Score sc ON s.StudentID = sc.StudentID
            WHERE a.AccountID = @AccountID
            GROUP BY c.ClassID, c.IsLocked, s.StudentID, s.FullName, s.Gender, c.ClassName";

                SqlParameter[] parameters = { new SqlParameter("@AccountID", currentUserId) };
                DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);

                if (dt.Rows.Count > 0)
                {
                    _currentClassId = Convert.ToInt32(dt.Rows[0]["ClassID"]);
                    IsClassLocked = dt.Rows[0]["IsLocked"] != DBNull.Value && Convert.ToBoolean(dt.Rows[0]["IsLocked"]);
                    ClassTitle = $"Danh sách học tập lớp {dt.Rows[0]["ClassName"]}";

                    int stt = 1;
                    foreach (DataRow row in dt.Rows)
                    {
                        int gradedCount = Convert.ToInt32(row["GradedCount"]);
                        int totalSubjects = Convert.ToInt32(row["TotalSubjects"]);

                        string scoreStr;
                        if (gradedCount == 0) scoreStr = "Chưa có điểm";
                        else if (gradedCount < totalSubjects) scoreStr = "Thiếu điểm môn";
                        else scoreStr = row["OverallAverage"] != DBNull.Value ? Convert.ToDecimal(row["OverallAverage"]).ToString("0.0") : "Chưa có điểm";

                        _allStudents.Add(new HomeroomStudentGradeItem
                        {
                            STT = stt++,
                            StudentId = row["StudentID"].ToString(),
                            FullName = row["FullName"].ToString(),
                            Gender = row["Gender"].ToString(),
                            ClassName = row["ClassName"].ToString(),
                            AverageScore = scoreStr
                        });
                    }
                }
                else
                {
                    ClassTitle = "Tài khoản này hiện chưa được phân công chủ nhiệm lớp nào.";
                }

                FilterData();
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi hệ thống khi tải dữ liệu lớp chủ nhiệm: " + ex.Message);
            }
        }

        // --- XÉT DUYỆT ĐẠT/KHÔNG ĐẠT ---
        private void ExecuteGenerateReport(object obj)
        {
            try
            {
                // Lấy điểm chuẩn quy định từ bảng Parameter (Mặc định 5.0)
                string getPassingGradeQuery = "SELECT ISNULL((SELECT Value FROM Parameter WHERE ParameterName = 'NumPassingGrade'), 5.0) as PassingGrade";
                DataTable dtParam = DatabaseHelper.ExecuteQuery(getPassingGradeQuery);
                decimal passingGrade = Convert.ToDecimal(dtParam.Rows[0]["PassingGrade"]);

                // Lọc Min Score của từng học sinh
                string query = @"
                    DECLARE @TotalSubjects INT = (SELECT COUNT(*) FROM Subject WHERE IsDeleted = 0);

                    SELECT 
                        s.StudentID, s.FullName,
                        COUNT(sc.SubjectID) AS GradedCount,
                        MIN(sc.AverageScore) AS MinScore,
                        @TotalSubjects AS TotalSubjects
                    FROM Student s
                    JOIN ClassPlacement cp ON s.StudentID = cp.StudentID
                    LEFT JOIN Score sc ON s.StudentID = sc.StudentID
                    WHERE cp.ClassID = @ClassID
                    GROUP BY s.StudentID, s.FullName";

                DataTable dt = DatabaseHelper.ExecuteQuery(query, new[] { new SqlParameter("@ClassID", _currentClassId) });

                var tempList = new ObservableCollection<ReportItem>();
                int passCount = 0;
                int stt = 1;

                foreach (DataRow row in dt.Rows)
                {
                    int gradedCount = Convert.ToInt32(row["GradedCount"]);
                    int totalSubjects = Convert.ToInt32(row["TotalSubjects"]);

                    // BẮT LỖI TÍNH TOÀN VẸN DỮ LIỆU ĐIỂM
                    if (gradedCount < totalSubjects)
                    {
                        NotificationHelper.ShowError("Không thể lập báo cáo. Dữ liệu điểm của lớp chưa hoàn tất. Vui lòng đợi GVBM hoàn thiện điểm.");
                        IsReportGenerated = false;
                        return;
                    }

                    decimal minScore = row["MinScore"] != DBNull.Value ? Convert.ToDecimal(row["MinScore"]) : 0;

                    // Có 1 môn dưới điểm chuẩn (MinScore < Điểm chuẩn) -> RỚT 
                    bool isPassed = minScore >= passingGrade;

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

        // --- KHÓA SỔ ---
        private void ExecuteConfirmReport(object obj)
        {
            try
            {
                string query = "UPDATE Class SET IsLocked = 1 WHERE ClassID = @ClassID";
                int rows = DatabaseHelper.ExecuteNonQuery(query, new[] { new SqlParameter("@ClassID", _currentClassId) });

                if (rows > 0)
                {
                    IsClassLocked = true;
                    NotificationHelper.ShowSuccess("Đã xác nhận báo cáo và KHÓA SỔ thành công! Giáo viên bộ môn sẽ không thể sửa điểm nữa.");
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi khóa sổ: " + ex.Message);
            }
        }

        // --- MỞ KHÓA SỔ ---
        private void ExecuteCancelReport(object obj)
        {
            try
            {
                string query = "UPDATE Class SET IsLocked = 0 WHERE ClassID = @ClassID";
                int rows = DatabaseHelper.ExecuteNonQuery(query, new[] { new SqlParameter("@ClassID", _currentClassId) });

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

        private async void ExecuteOpenDetail(HomeroomStudentGradeItem student)
        {
            var detailVM = new StudentGradeDetailViewModel(student.StudentId, student.FullName);

            var detailView = new WPF_Student_Management.Components.StudentGradeDetailUC
            {
                DataContext = detailVM
            };

            await MaterialDesignThemes.Wpf.DialogHost.Show(detailView, "RootDialog");
        }

        // --- XỬ LÝ CLICK ĐÚP VÀO HỌC SINH TRONG BÁO CÁO ---
        private async void ExecuteViewDetail(object obj)
        {
            if (obj is ReportItem selectedStudent)
            {
                if (selectedStudent.Status.Trim().Equals("Đạt", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

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

                string query = @"
                    SELECT sub.SubjectName, sc.RegularTestScore, sc.MidTermScore, sc.FinalTermScore, sc.AverageScore 
                    FROM Score sc
                    JOIN Subject sub ON sc.SubjectID = sub.SubjectID
                    WHERE sc.StudentID = @StudentID 
                      AND sc.AverageScore < @PassingGrade";

                SqlParameter[] parameters = {
                    new SqlParameter("@StudentID", studentId),
                    new SqlParameter("@PassingGrade", passingGrade)
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
    }
}