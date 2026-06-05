using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;

namespace WPF_Student_Management.ViewModels
{
    public partial class GlobalStudentManagementViewModel : ObservableObject
    {
        //KIỂM TRA ROLE ĐỂ KHÓA GIAO DIỆN HIỆU TRƯỞNG ---
        public Visibility ActionVisibility => PermissionService.HasFeature(PermissionService.Feature.ManageGlobalStudents) ? Visibility.Visible : Visibility.Collapsed;
        private bool CanModify() => PermissionService.HasFeature(PermissionService.Feature.ManageGlobalStudents);
        private bool CanModifyStudent(Student student) => PermissionService.HasFeature(PermissionService.Feature.ManageGlobalStudents);
        private bool CanViewOrModifyStudent(Student student) =>
            PermissionService.HasFeature(PermissionService.Feature.ManageGlobalStudents) ||
            PermissionService.HasFeature(PermissionService.Feature.ViewGlobalStudents);

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
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _phoneNumber = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _address = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
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

        [ObservableProperty]
        private string _selectedClass = "Tất cả";

        // Danh sách đổ vào ComboBox Giới tính
        public ObservableCollection<string> GenderList { get; } = new ObservableCollection<string> { "Tất cả", "Nam", "Nữ" };

        // Danh sách đổ vào ComboBox Lớp
        public ObservableCollection<string> ClassList { get; } = new ObservableCollection<string>();

        // Map StudentId → ClassName (runtime lookup)
        private Dictionary<string, string> _studentClassMap = new();

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
            LoadClassFilterData();
            LoadDataFromDatabase();
            OnDateOfBirthChanged(DateTime.Now);
            LoadAgeRegulations();
            OnDateOfBirthChanged(DateOfBirth);
        }

        private void LoadClassFilterData()
        {
            try
            {
                var classes = Class.GetAllClasses()
                    .Select(c => c.ClassName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name)
                    .ToList();

                ClassList.Clear();
                ClassList.Add("Tất cả");
                foreach (var cls in classes)
                    ClassList.Add(cls);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Không tải được danh sách lớp: " + ex.Message);
                ClassList.Clear();
                ClassList.Add("Tất cả");
            }
        }

        private void LoadStudentClassMap()
        {
            try
            {
                _studentClassMap = Class.GetCurrentStudentClassMap();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Không tải được map lớp học sinh: " + ex.Message);
                _studentClassMap = new Dictionary<string, string>();
            }
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
                LoadStudentClassMap();

                //lọc ra đứa nào có status Active mới lấy
                var studentList = Student.GetAllStudents()
                                         .Where(s => s.Status == "Active")
                                         .ToList();

                _originalStudentList = studentList;
                FilterData();
            }
            catch (System.Exception ex)
            {
                NotificationHelper.ShowError($"Lỗi kết nối CSDL:\n{ex.Message}");
            }
        }

        // ĐÃ KHÓA NẾU LÀ HIỆU TRƯỞNG
        [RelayCommand(CanExecute = nameof(CanModify))]
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

        // ĐÃ KHÓA NẾU LÀ HIỆU TRƯỞNG
        [RelayCommand(CanExecute = nameof(CanViewOrModifyStudent))]
        private async Task EditStudent(Student student)
        {
            if (student == null)
                return;

            bool isReadOnlyForThisUser = !PermissionService.HasFeature(PermissionService.Feature.ManageGlobalStudents);

            var detailVM = new StudentProfileDetailViewModel(student, isReadOnlyForThisUser);

            var view = new Components.StudentProfileDetailUC
            {
                DataContext = detailVM
            };

            await MaterialDesignThemes.Wpf.DialogHost.Show(view, "RootDialog");
            LoadDataFromDatabase();
        }

        // ĐÃ KHÓA NẾU LÀ HIỆU TRƯỞNG
        [RelayCommand(CanExecute = nameof(CanModifyStudent))]
        private void DeleteStudent(Student student)
        {
            if (student == null)
                return;

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
                        NotificationHelper.ShowWarning("Không thể xóa học sinh này!\n\nHọc sinh đã có dữ liệu Điểm số hoặc Xếp lớp.\nVui lòng chuyển trạng thái thành 'Inactive' hoặc xóa các dữ liệu liên quan trước.");
                    else
                        NotificationHelper.ShowError("Lỗi CSDL: " + sqlEx.Message);
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
            if (DateTime.Now.DayOfYear < value.DayOfYear)
                age--;

            if (age < _minAge || age > _maxAge)
                AgeErrorMessage = $"Tuổi {age} không hợp lệ (Quy định: {_minAge} - {_maxAge})";
            else
                AgeErrorMessage = string.Empty;
        }

        private bool CanSave()
        {
            if (!CanModify())
                return false;

            string phoneRegexPattern = @"^0\d{9}$";
            return string.IsNullOrEmpty(AgeErrorMessage) &&
                   !string.IsNullOrWhiteSpace(FullName) &&
                   !string.IsNullOrWhiteSpace(Address) &&
                   !string.IsNullOrWhiteSpace(PhoneNumber) &&
                   !string.IsNullOrWhiteSpace(EmailPrefix) &&
                   !string.IsNullOrEmpty(GuardianName) &&
                   !string.IsNullOrEmpty(GuardianPhoneNumber) &&
                   Regex.IsMatch(PhoneNumber?.Trim() ?? "", phoneRegexPattern) &&
                   Regex.IsMatch(GuardianPhoneNumber?.Trim() ?? "", phoneRegexPattern);
        }

        // Kiểm tra sớm theo hướng an toàn cho concurrency: báo trùng email sớm cho người dùng, nhưng ràng buộc UNIQUE trong DB vẫn là lớp chặn cuối cùng.
        private bool IsStudentEmailAvailable(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return true;

            string query = "SELECT COUNT(*) FROM Student WHERE Email = @Email";
            DataTable dt = DatabaseHelper.ExecuteQuery(query, new[]
            {
                new SqlParameter("@Email", email)
            });

            return dt.Rows.Count == 0 || Convert.ToInt32(dt.Rows[0][0]) == 0;
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void Save()
        {
            try
            {
                // Chuẩn hóa email trước khi kiểm tra trùng để các người dùng đồng thời so sánh trên cùng một giá trị chuẩn.
                string? normalizedEmail = string.IsNullOrWhiteSpace(this.EmailPrefix) ? null : $"{this.EmailPrefix.Trim().ToLowerInvariant()}@gmail.com";
                if (!IsStudentEmailAvailable(normalizedEmail))
                {
                    NotificationHelper.ShowError("Lỗi: Email này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại!");
                    return;
                }

                var newDbStudent = new Student
                {
                    StudentId = "",
                    FullName = this.FullName,
                    Gender = IsMale ? "Nam" : "Nữ",
                    DateOfBirth = this.DateOfBirth,
                    PhoneNumber = this.PhoneNumber,
                    Email = normalizedEmail,
                    Address = this.Address,
                    FamilyBackground = IsFamilyNormal ? "Bình thường" : "Khó khăn",
                    GuardianName = this.GuardianName,
                    GuardianPhoneNumber = this.GuardianPhoneNumber,
                    Status = "Active"
                };

                string? newStudentId = newDbStudent.ReceiveNewStudent();

                if (!string.IsNullOrEmpty(newStudentId))
                {
                    NotificationHelper.ShowSuccess($"Tiếp nhận thành công!\nMã HS / Tài khoản: {newStudentId}\nMật khẩu : <Ngày/tháng/năm sinh> + 4 số cuối trong số điện thoại liên lạc học sinh");
                    LoadDataFromDatabase();
                    Cancel();
                }
                else
                {
                    NotificationHelper.ShowError("Tiếp nhận học sinh thất bại!");
                }
            }
            catch (SqlException sqlEx)
            {
                // Bắt lỗi trùng Email hoặc trùng dữ liệu (Unique Constraint)
                // 2601: Lỗi do vi phạm "Unique Index" (Tạo ra một dòng trùng lặp ở cột đã đánh dấu là Unique).
                // 2627: Lỗi do vi phạm "Primary Key"(Khóa chính) hoặc "Unique Constraint"(Ràng buộc duy nhất).
                if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
                    NotificationHelper.ShowError("Lỗi: Email này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại!");
                else
                    NotificationHelper.ShowError("Lỗi cơ sở dữ liệu: " + sqlEx.Message);
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi hệ thống khi lưu: " + ex.Message);
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

        partial void OnSearchTextChanged(string value) => FilterData();
        partial void OnSelectedGenderChanged(string value) => FilterData();
        partial void OnSelectedClassChanged(string value) => FilterData();

        private string GetStudentClassName(Student s)
        {
            return _studentClassMap.TryGetValue(s.StudentId, out var cn) ? cn : "";
        }

        private void FilterData()
        {
            if (_originalStudentList == null || !_originalStudentList.Any())
                return;

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

            if (!string.IsNullOrWhiteSpace(SelectedClass) && SelectedClass != "Tất cả")
            {
                filtered = filtered.Where(s =>
                {
                    var cn = GetStudentClassName(s);
                    return !string.IsNullOrEmpty(cn) && cn.Equals(SelectedClass, StringComparison.OrdinalIgnoreCase);
                });
            }

            var resultList = filtered.ToList();
            for (int i = 0; i < resultList.Count; i++)
                resultList[i].STT = i + 1;

            AllStudent = new ObservableCollection<Student>(resultList);
        }
    }
}