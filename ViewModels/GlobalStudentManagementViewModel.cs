using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
                var allRegulations = Regulation.GetAllRegulations();

                if (allRegulations != null && allRegulations.Any())
                {
                    var minAgeParam = allRegulations.FirstOrDefault(r => r.RegulationName == "MinAge");
                    if (minAgeParam != null)
                    {
                        _minAge = (int)minAgeParam.Value;
                    }

                    var maxAgeParam = allRegulations.FirstOrDefault(r => r.RegulationName == "MaxAge");
                    if (maxAgeParam != null)
                    {
                        _maxAge = (int)maxAgeParam.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Không tải được quy định tuổi: " + ex.Message);
            }
        }

        private void LoadDataFromDatabase()
        {
            try
            {
                var studentList = Student.GetAllStudents();
                _originalStudentList = studentList;
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
                DataContext = this
            };
            await MaterialDesignThemes.Wpf.DialogHost.Show(dialogContent, "RootDialog");
        }

        [RelayCommand]
        private async Task EditStudent(Student student)
        {
            if (student == null) return;

            var detailVM = new StudentProfileDetailViewModel(student);
            var view = new Components.StudentProfileDetailUC
            {
                DataContext = detailVM
            };

            await MaterialDesignThemes.Wpf.DialogHost.Show(view, "RootDialog");
            LoadDataFromDatabase();
        }

        [RelayCommand]
        private void DeleteStudent(Student student)
        {
            if (student == null) return;

            bool isChonOK = NotificationHelper.ShowConfirm($"Bạn có chắc chắn muốn xóa học sinh '{student.FullName}' khỏi hệ thống không?\nHành động này không thể hoàn tác!");

            if (isChonOK)
            {
                try
                {
                    // Lưu lại ID tài khoản để xóa kèm
                    int accountIdToDelete = student.AccountId;

                    // Gọi Model để xóa Student trước (Tránh lỗi khóa ngoại chiếu ngược)
                    if (Student.DeleteStudent(student.StudentId))
                    {
                        // Xóa luôn Account của học sinh đó
                        Account.DeleteAccount(accountIdToDelete);

                        // Cập nhật lại UI
                        AllStudent.Remove(student);
                        _originalStudentList.Remove(student);
                        NotificationHelper.ShowSuccess("Xóa học sinh thành công!");
                    }
                    else
                    {
                        NotificationHelper.ShowError("Xóa thất bại! Không tìm thấy học sinh trong CSDL.");
                    }
                }
                catch (SqlException sqlEx)
                {
                    if (sqlEx.Number == 547)
                    {
                        NotificationHelper.ShowWarning("Không thể xóa học sinh này!\n\nHọc sinh đã có dữ liệu Điểm số hoặc Xếp lớp.\nVui lòng chuyển trạng thái thành 'Inactive' hoặc xóa các dữ liệu liên quan trước.");
                    }
                    else
                    {
                        NotificationHelper.ShowError("Lỗi CSDL: " + sqlEx.Message);
                    }
                }
                catch (Exception ex)
                {
                    NotificationHelper.ShowError("Lỗi hệ thống: " + ex.Message);
                }
            }
        }

        partial void OnDateOfBirthChanged(DateTime value)
        {
            int age = DateTime.Now.Year - value.Year;
            if (DateTime.Now.DayOfYear < value.DayOfYear) age--;

            if (age < _minAge || age > _maxAge)
                AgeErrorMessage = $"Tuổi {age} không hợp lệ (Quy định: {_minAge} - {_maxAge})";
            else
                AgeErrorMessage = string.Empty;
        }

        private bool CanSave()
        {
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
                NotificationHelper.ShowSuccess($"Tiếp nhận thành công!\nMã HS / Tài khoản: {newStudentId} / Mật khẩu : <Ngày/tháng/năm sinh + 4 số cuối trong số điện thoại liên lạc học sinh");
                LoadDataFromDatabase();
                Cancel();
            }
            else
            {
                NotificationHelper.ShowError("Lưu thông tin học sinh thất bại!");
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            FullName = Address = EmailPrefix = PhoneNumber = GuardianName = GuardianPhoneNumber = string.Empty;
            IsMale = true;
            IsFamilyNormal = true;
            DateOfBirth = DateTime.Now;
            AgeErrorMessage = string.Empty;
            MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterData();
        }

        partial void OnSelectedGenderChanged(string value)
        {
            FilterData();
        }

        private void FilterData()
        {
            if (_originalStudentList == null || !_originalStudentList.Any()) return;

            var filtered = _originalStudentList.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(s =>
                    (!string.IsNullOrEmpty(s.FullName) && s.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(s.StudentId) && s.StudentId.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(s.PhoneNumber) && s.PhoneNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (!string.IsNullOrWhiteSpace(SelectedGender) && SelectedGender != "Tất cả")
            {
                filtered = filtered.Where(s => !string.IsNullOrEmpty(s.Gender) && s.Gender.Equals(SelectedGender, StringComparison.OrdinalIgnoreCase));
            }

            AllStudent = new ObservableCollection<Student>(filtered);
        }
    }
}