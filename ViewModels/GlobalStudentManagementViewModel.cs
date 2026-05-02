using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Effects;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;

namespace WPF_Student_Management.ViewModels
{
    public partial class GlobalStudentManagementViewModel : ObservableObject
    {
        //TẠO 2 BIẾN LƯU TRỮ QUY ĐỊNH LÚC VỪA MỞ FORM(Cho số mặc định lỡ DB lỗi)
        private int _minAge = 15;
        private int _maxAge = 20;

        [ObservableProperty]
        private ObservableCollection<Student> _allStudent;
        private List<Student> _originalStudentList = new List<Student>();

        //thông tin cá nhân của học sinh
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _fullName = string.Empty;
        [ObservableProperty]
        private bool _isMale = true;
        [ObservableProperty]
        private bool _isFemale;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private DateTime _dateOfBirth = DateTime.Now;
        [ObservableProperty]
        private string _phoneNumber = string.Empty;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _address = string.Empty;
        [ObservableProperty]
        private string _emailPrefix = string.Empty;

        //người bảo hộ của học sinh
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _guardianName = string.Empty;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _guardianPhoneNumber = string.Empty;

        //hoàn cảnh gia đình
        [ObservableProperty]
        private bool _isFamilyNormal = true;
        [ObservableProperty]
        private bool _isFamilyHard;

        //Các biến tìm kiếm và lọc
        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedGender = "Tất cả";

        // Danh sách đổ vào ComboBox Giới tính
        public ObservableCollection<string> GenderList { get; } = new ObservableCollection<string> { "Tất cả", "Nam", "Nữ" };

        //Logic đồng bộ giới tính
        partial void OnIsMaleChanged(bool value) => IsFemale = !value;
        partial void OnIsFemaleChanged(bool value) => IsMale = !value;

        //Logic đồng bộ hoàn cảnh gia đình
        partial void OnIsFamilyNormalChanged(bool value) => IsFamilyHard = !value;
        partial void OnIsFamilyHardChanged(bool value) => IsFamilyNormal = !value;

        // Thông báo lỗi nếu sai tuổi quy định
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _ageErrorMessage = string.Empty;

        public GlobalStudentManagementViewModel()
        {
            AllStudent = new ObservableCollection<Student>();
            LoadDataFromDatabase();
            OnDateOfBirthChanged(DateTime.Now);

            //KÉO QUY ĐỊNH TỪ DB LÊN NGAY LÚC KHỞI TẠO
            LoadAgeRegulations();

            // Ép nó check lại tuổi ngay khi vừa load lên (lỡ tuổi lúc trước hợp lệ, giờ đổi quy định thành không hợp lệ)
            OnDateOfBirthChanged(DateOfBirth);
        }

        private void LoadAgeRegulations()
        {
            try
            {
                //Kéo toàn bộ list tham số từ DB lên
                var allRegulations = Regulation.GetAllRegulations();

                if (allRegulations != null && allRegulations.Any())
                {
                    //Tìm dòng có tên là "MinAge"
                    var minAgeParam = allRegulations.FirstOrDefault(r => r.RegulationName == "MinAge");
                    if (minAgeParam != null)
                    {
                        // Ép kiểu từ Decimal (trong Model của Long) sang int
                        _minAge = (int)minAgeParam.Value;
                    }

                    //Tìm dòng có tên là "MaxAge"
                    var maxAgeParam = allRegulations.FirstOrDefault(r => r.RegulationName == "MaxAge");
                    if (maxAgeParam != null)
                    {
                        _maxAge = (int)maxAgeParam.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                // Lỗi DB thì nuốt lỗi, UI vẫn xài số 15-20 mặc định, app không sập
                Console.WriteLine("Không tải được quy định tuổi: " + ex.Message);
            }
        }

        private void LoadDataFromDatabase()
        {
            try
            {
                // 1. Gọi Database lấy toàn bộ học sinh
                var studentList = Student.GetAllStudents();

                // 2. LƯU VÀO DANH SÁCH GỐC
                _originalStudentList = studentList;

                // 3. Chạy FilterData để nó tự động xử lý và nạp vào AllStudent (đề phòng lúc reload đang có sẵn chữ ở ô Tìm kiếm)
                FilterData();
            }
            catch (System.Exception ex)
            {
                NotificationHelper.ShowError($"Lỗi kết nối CSDL:\n{ex.Message}");
            }
        }

        [RelayCommand]
        private async Task AddStudent()
        {
            LoadAgeRegulations();
            OnDateOfBirthChanged(DateOfBirth);

            var dialogContent = new WPF_Student_Management.Components.AddStudentDialog
            {
                DataContext = this // Quan trọng: Dùng chung bộ não này
            };
            await MaterialDesignThemes.Wpf.DialogHost.Show(dialogContent, "RootDialog");
        }

        [RelayCommand]
        private async Task EditStudent(Student student)
        {
            if (student == null) return;

            // Khởi tạo "Bộ não" cho popup
            var detailVM = new StudentProfileDetailViewModel(student);

            // Khởi tạo "Cái xác" popup
            var view = new Components.StudentProfileDetailUC
            {
                DataContext = detailVM
            };

            // Bật lên!
            await MaterialDesignThemes.Wpf.DialogHost.Show(view, "RootDialog");
            // Ngay khi Popup vừa đóng lại (dù là bấm Lưu hay Hủy), ép DataGrid load lại data mới nhất từ CSDL!
            LoadDataFromDatabase();
        }

        [RelayCommand]
        private void DeleteStudent(Student student)
        {
            if (student != null)
            {
                bool isChonOK = NotificationHelper.ShowConfirm($"Bạn có chắc chắn muốn xóa học sinh '{student.FullName}' khỏi hệ thống không?\nHành động này không thể hoàn tác!");

                if (isChonOK)
                {
                    // Xóa thẳng trên UI để test cảm giác bấm nút
                    AllStudent.Remove(student);
                    NotificationHelper.ShowSuccess("Xóa học sinh thành công!");
                }
            }
        }

        // Logic kiểm tra tuổi mỗi khi thay đổi ngày sinh
        partial void OnDateOfBirthChanged(DateTime value)
        {
            int age = DateTime.Now.Year - value.Year;
            if (DateTime.Now.DayOfYear < value.DayOfYear) age--;

            if (age < _minAge || age > _maxAge)
                AgeErrorMessage = $"Tuổi {age} không hợp lệ (Quy định: {_minAge} - {_maxAge})";
            else
                AgeErrorMessage = string.Empty;
        }

        // Kiểm tra điều kiện để kích hoạt nút Lưu
        private bool CanSave()
        {
            // Bắt buộc nhập Họ Tên học sinh, Địa Chỉ,Sdt liên lạc, Họ tên + sdt ng bảo hộ, và không có lỗi tuổi thì mới được lưu
            return string.IsNullOrEmpty(AgeErrorMessage) &&
                   !string.IsNullOrWhiteSpace(FullName) &&
                   !string.IsNullOrWhiteSpace(Address) &&
                   !string.IsNullOrWhiteSpace(PhoneNumber) &&
                   !string.IsNullOrEmpty(GuardianName) &&
                   !string.IsNullOrEmpty(GuardianPhoneNumber);
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void Save()
        {

            //Bắt buộc bắt đầu bằng '0' và theo sau là đúng '9' chữ số (Tổng = 10)
            string phoneRegexPattern = @"^0\d{9}$";

            if (!System.Text.RegularExpressions.Regex.IsMatch(PhoneNumber?.Trim() ?? "", phoneRegexPattern))
            {
                NotificationHelper.ShowWarning("Số điện thoại Học sinh chưa hợp lệ!\nVui lòng nhập ĐỦ 10 chữ số và bắt đầu bằng số 0.");
                return;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(GuardianPhoneNumber?.Trim() ?? "", phoneRegexPattern))
            {
                NotificationHelper.ShowWarning("Số điện thoại Người bảo hộ chưa hợp lệ!\nVui lòng nhập ĐỦ 10 chữ số và bắt đầu bằng số 0.");
                return;
            }

            var newDbStudent = new Student
            {
                StudentId = "",
                FullName = this.FullName,
                Gender = IsMale ? "Nam" : "Nữ",
                DateOfBirth = this.DateOfBirth,
                PhoneNumber = this.PhoneNumber,
                Email = string.IsNullOrWhiteSpace(this.EmailPrefix)
                        ? ""
                        : $"{this.EmailPrefix.Trim()}@gmail.com",
                Address = this.Address,
                FamilyBackground = IsFamilyNormal ? "Bình thường" : "Khó khăn",
                GuardianName = this.GuardianName,
                GuardianPhoneNumber = this.GuardianPhoneNumber,
                Status = "Active"
            };

            string? newStudentId = newDbStudent.ReceiveNewStudent();

            if (!string.IsNullOrEmpty(newStudentId))
            {
                NotificationHelper.ShowSuccess($"Tiếp nhận thành công!\nMã HS / Tài khoản: {newStudentId}");

                // Refresh lại Grid để thấy ngay học sinh vừa thêm
                LoadDataFromDatabase();

                Cancel(); // Đóng form, dọn rác
            }
            else
            {
                NotificationHelper.ShowError("Lưu thông tin học sinh thất bại!");
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            // Reset form sạch sẽ
            FullName = Address = EmailPrefix = PhoneNumber = GuardianName = GuardianPhoneNumber = string.Empty;

            IsMale = true;
            IsFamilyNormal = true;

            DateOfBirth = DateTime.Now;
            AgeErrorMessage = string.Empty;
            // Đóng Dialog
            MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
        }
        partial void OnSearchTextChanged(string value)
        {
            FilterData();
        }

        // Tự động gọi hàm FilterData() mỗi khi chọn Giới tính khác
        partial void OnSelectedGenderChanged(string value)
        {
            FilterData();
        }
        private void FilterData()
        {
            if (_originalStudentList == null || !_originalStudentList.Any()) return;

            // Lấy toàn bộ danh sách gốc ra để chuẩn bị cắt gọt
            var filtered = _originalStudentList.AsEnumerable();

            // 1. LỌC THEO TỪ KHÓA TÌM KIẾM (SearchText)
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(s =>
                    (!string.IsNullOrEmpty(s.FullName) && s.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(s.StudentId) && s.StudentId.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(s.PhoneNumber) && s.PhoneNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                );
            }

            // 2. LỌC THEO GIỚI TÍNH (SelectedGender)
            if (!string.IsNullOrWhiteSpace(SelectedGender) && SelectedGender != "Tất cả")
            {
                filtered = filtered.Where(s => !string.IsNullOrEmpty(s.Gender) && s.Gender.Equals(SelectedGender, StringComparison.OrdinalIgnoreCase));
            }

            // 3. ĐỔ KẾT QUẢ VÀO DANH SÁCH HIỂN THỊ CỦA DATAGRID
            // Gán biến trực tiếp như vầy thì [ObservableProperty] sẽ tự động báo UI cập nhật
            AllStudent = new ObservableCollection<Student>(filtered);
        }
    }
}
