using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.ViewModels
{
    // Class trung gian để hiển thị lên DataGrid
    public class HomeroomStudentGradeItem
    {
        public int STT { get; set; }
        public string StudentId { get; set; }
        public string FullName { get; set; }
        public string Gender { get; set; }
        public string ClassName { get; set; }
        public string AverageScore { get; set; } // Dùng string để dễ format "Chưa có điểm"
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

        // --- CÁC BIẾN TÌM KIẾM & LỌC ---
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterData(); // Tự động lọc khi gõ chữ
            }
        }

        public ObservableCollection<string> GenderList { get; set; }

        private string _selectedGender;
        public string SelectedGender
        {
            get => _selectedGender;
            set
            {
                _selectedGender = value;
                OnPropertyChanged();
                FilterData(); // Tự động lọc khi chọn giới tính
            }
        }

        // Tiêu đề hiển thị (Ví dụ: Danh sách lớp 10A1)
        private string _classTitle;
        public string ClassTitle
        {
            get => _classTitle;
            set { _classTitle = value; OnPropertyChanged(); }
        }

        public HomeroomDashboardViewModel()
        {
            GenderList = new ObservableCollection<string> { "Tất cả", "Nam", "Nữ" };
            SelectedGender = "Tất cả";
            LoadHomeroomData();
        }

        private void LoadHomeroomData()
        {
            _allStudents = new ObservableCollection<HomeroomStudentGradeItem>();

            try
            {
                // Kiểm tra trạng thái đăng nhập
                if (CurrentUser.Instance == null || CurrentUser.Instance.UserId == 0)
                {
                    ClassTitle = "Vui lòng đăng nhập vào hệ thống.";
                    FilterData();
                    return;
                }

                int currentUserId = CurrentUser.Instance.UserId;

                // Kiểm tra Role của User hiện tại có phải GVCN hay không
                string roleQuery = @"
                    SELECT r.RoleName 
                    FROM Account a 
                    JOIN Role r ON a.RoleID = r.RoleID 
                    WHERE a.AccountID = @AccountID";

                DataTable dtRole = DatabaseHelper.ExecuteQuery(roleQuery, new[] { new SqlParameter("@AccountID", currentUserId) });

                if (dtRole.Rows.Count == 0 || dtRole.Rows[0]["RoleName"].ToString() != "GVCN")
                {
                    // Nếu là Học sinh, GVBM, Giáo vụ... thì báo lỗi và dừng hàm luôn
                    ClassTitle = "Bạn không phải là Giáo viên chủ nhiệm.";
                    FilterData();
                    return;
                }

                // Đã xác nhận đúng là GVCN -> Query lấy danh sách học sinh và điểm
                string query = @"
            SELECT 
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
            GROUP BY s.StudentID, s.FullName, s.Gender, c.ClassName";

                SqlParameter[] parameters = { new SqlParameter("@AccountID", currentUserId) };
                DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);

                if (dt.Rows.Count > 0)
                {
                    ClassTitle = $"Danh sách học tập lớp {dt.Rows[0]["ClassName"]}";

                    int stt = 1;
                    foreach (DataRow row in dt.Rows)
                    {
                        int gradedCount = Convert.ToInt32(row["GradedCount"]);
                        int totalSubjects = Convert.ToInt32(row["TotalSubjects"]);

                        // Kiểm tra học sinh đã nhập đủ điểm các môn hay chưa
                        string scoreStr;
                        if (gradedCount == 0)
                        {
                            scoreStr = "Chưa có điểm";
                        }
                        else if (gradedCount < totalSubjects)
                        {
                            scoreStr = "Thiếu điểm môn";
                        }
                        else
                        {
                            scoreStr = row["OverallAverage"] != DBNull.Value
                                ? Convert.ToDecimal(row["OverallAverage"]).ToString("0.0")
                                : "Chưa có điểm";
                        }

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

        // ---XỬ LÝ CLICK VÀO DÒNG MỞ CHI TIẾT ---
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

        private async void ExecuteOpenDetail(HomeroomStudentGradeItem student)
        {
            var detailVM = new StudentGradeDetailViewModel(student.StudentId, student.FullName);

            var detailView = new WPF_Student_Management.Components.StudentGradeDetailUC
            {
                DataContext = detailVM
            };

            await MaterialDesignThemes.Wpf.DialogHost.Show(detailView, "RootDialog");
        }

        // Hàm xử lý Lọc (Giới tính) và Tìm kiếm (Họ tên) đáp ứng DoD
        private void FilterData()
        {
            if (_allStudents == null) return;

            var filtered = _allStudents.AsEnumerable();

            // Lọc theo tên (Tìm kiếm tương đối / Contains)
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(s => s.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            // Lọc theo giới tính
            if (!string.IsNullOrWhiteSpace(SelectedGender) && SelectedGender != "Tất cả")
            {
                filtered = filtered.Where(s => s.Gender.Equals(SelectedGender, StringComparison.OrdinalIgnoreCase));
            }

            // Cập nhật lại STT sau khi lọc
            var resultList = filtered.ToList();
            for (int i = 0; i < resultList.Count; i++)
            {
                resultList[i].STT = i + 1;
            }

            DisplayStudents = new ObservableCollection<HomeroomStudentGradeItem>(resultList);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}