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
        public int StudentId { get; set; }
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
                // Sử dụng UserId từ CurrentUser. Nếu chưa đăng nhập (CurrentUser rỗng), 
                // mặc định lấy ID là 4 (Thầy Phạm Văn Cán - GVCN 10A1) để test.
                int currentUserId = (CurrentUser.Instance != null && CurrentUser.Instance.UserId != 0)
                                    ? CurrentUser.Instance.UserId
                                    : 4;

                // Query lấy danh sách học sinh và điểm trung bình dựa trên AccountID của GVCN
                // Đếm thêm số môn đã nhập (GradedCount) và số môn yêu cầu (TotalSubjects)
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
                            scoreStr = "Thiếu điểm môn"; // Học sinh còn thiếu môn chưa nhập
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
                            StudentId = Convert.ToInt32(row["StudentID"]),
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
                // DÙNG NOTIFICATION HELPER THAY VÌ MESSAGEBOX HỆ THỐNG
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
                    // Mở Pop-up xem điểm chi tiết
                    ExecuteOpenDetail(value);

                    // Hoãn việc set null lại một nhịp để WPF kịp xử lý xong sự kiện MouseClick
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
            // Tạo ViewModel và truyền ID học sinh vào để nó kéo điểm
            var detailVM = new StudentGradeDetailViewModel(student.StudentId, student.FullName);

            // Gọi Component View và gán DataContext
            var detailView = new WPF_Student_Management.Components.StudentGradeDetailUC
            {
                DataContext = detailVM
            };

            // Hiển thị Pop-up thông qua RootDialog (được khai báo sẵn ở MainWindow)
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