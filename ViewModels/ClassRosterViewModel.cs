using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;

namespace WPF_Student_Management.ViewModels
{
    public partial class SelectableStudentItem : ObservableObject
    {
        [ObservableProperty] private bool _isSelected = false;
        [ObservableProperty] private string _studentId = string.Empty;
        [ObservableProperty] private string _fullName = string.Empty;
        [ObservableProperty] private string _gender = string.Empty;
        [ObservableProperty] private string _dateOfBirth = string.Empty;
        [ObservableProperty] private string _phoneNumber = string.Empty;
    }

    public partial class ClassRosterViewModel : ObservableObject
    {
        // --- BỔ SUNG: QUẢN LÝ NĂM HỌC HIỆN TẠI ---
        [ObservableProperty]
        private string _currentAcademicYear = "2025-2026";
        // -----------------------------------------

        [ObservableProperty]
        private ObservableCollection<SelectableStudentItem> _currentClassStudents;

        [ObservableProperty]
        private ObservableCollection<SelectableStudentItem> _availableStudents;

        [ObservableProperty]
        private string _selectedGrade;

        [ObservableProperty]
        private string _selectedClass;

        // Kho giấu kín chứa TOÀN BỘ lớp từ DB
        private List<Class> _allClassesFromDb = new List<Class>();

        // Kho đổ lên ComboBox Khối
        [ObservableProperty]
        private ObservableCollection<string> _availableGrades;

        // Kho đổ lên ComboBox Lớp
        [ObservableProperty]
        private ObservableCollection<Class> _availableClasses;

        // BIẾN LƯU SĨ SỐ TỐI ĐA (Kéo từ DB lên, giả định mặc định là 40)
        private int _maxClassSize = 40;

        public string ClassSizeText
        {
            get
            {
                // Nếu chưa chọn lớp thì cho tàng hình luôn
                if (SelectedClass == null || SelectedClass.ToString() == "")
                {
                    return "";
                }

                // Nếu đã chọn lớp thì hiện bình thường
                return $"Sĩ số: {CurrentClassStudents?.Count ?? 0} / {_maxClassSize}";
            }
        }

        public ClassRosterViewModel()
        {
            AvailableStudents = new ObservableCollection<SelectableStudentItem>();
            CurrentClassStudents = new ObservableCollection<SelectableStudentItem>();

            AvailableGrades = new ObservableCollection<string>();
            AvailableClasses = new ObservableCollection<Class>();
        }

        private void LoadRegulations()
        {
            try
            {
                // Lấy toàn bộ quy định từ CSDL
                var allRegulations = Regulation.GetAllRegulations();
                if (allRegulations != null && allRegulations.Any())
                {
                    // Tìm quy định có tên "MaxClassSize" (hoặc SiSoToiDa tùy bro đặt trong DB)
                    var maxSizeParam = allRegulations.FirstOrDefault(r => r.RegulationName == "MaxClassSize");
                    if (maxSizeParam != null)
                    {
                        // Gán vào biến của VM
                        _maxClassSize = (int)maxSizeParam.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi tải quy định sĩ số: " + ex.Message);
                // Giữ nguyên _maxClassSize = 40 (hoặc số an toàn nào đó) nếu DB lỗi
            }
        }

        private void LoadStudentsForSelectedClass()
        {
            // Xóa lưới cũ cho sạch sẽ
            CurrentClassStudents.Clear();

            // Rào chắn: Phải có mã lớp thì mới tìm
            if (string.IsNullOrEmpty(SelectedClass)) return;

            // Ép mã lớp từ chuỗi sang số nguyên
            if (int.TryParse(SelectedClass, out int classId))
            {
                try
                {
                    // Tận dụng hàm SearchStudents của Model, truyền đúng cái classId vào
                    var dbStudents = Student.SearchStudents(classId: classId);

                    // Trút data từ Model sang giao diện
                    foreach (var hs in dbStudents)
                    {
                        CurrentClassStudents.Add(new SelectableStudentItem
                        {
                            StudentId = hs.StudentId, // Mã giờ là "hs25..." tự sinh nên ném thẳng lên
                            FullName = hs.FullName,
                            Gender = hs.Gender ?? "Không rõ",
                            DateOfBirth = hs.DateOfBirth?.ToString("dd/MM/yyyy") ?? "Không rõ",
                            PhoneNumber = hs.PhoneNumber ?? ""
                        });
                    }

                    // Cập nhật dòng text "Sĩ số: X / Y" 
                    OnPropertyChanged(nameof(ClassSizeText));

                    // Check lại nút "+ Thêm học sinh" xem lớp có bị full chưa
                    OpenAddStudentDialogCommand.NotifyCanExecuteChanged();
                }
                catch (System.Exception ex)
                {
                    NotificationHelper.ShowError("Lỗi tải danh sách lớp: " + ex.Message);
                }
            }
        }

        //Check điều kiện để Enable/Disable nút "+ Thêm học sinh"
        private bool CanOpenAddStudent()
        {
            return CurrentClassStudents != null && CurrentClassStudents.Count < _maxClassSize;
        }

        [RelayCommand(CanExecute = nameof(CanOpenAddStudent))]
        private async Task OpenAddStudentDialog()
        {
            //Dọn sạch rác cũ trước khi load
            AvailableStudents.Clear();

            try
            {
                // SỬA LỖI COMPILER: Kéo danh sách học sinh chưa có lớp của NĂM HỌC HIỆN TẠI
                var bovoStudents = Student.GetUnassignedStudents(CurrentAcademicYear);

                //Đổ data vào kho chứa của Popup
                foreach (var hs in bovoStudents)
                {
                    AvailableStudents.Add(new SelectableStudentItem
                    {
                        StudentId = hs.StudentId,
                        FullName = hs.FullName,
                        Gender = hs.Gender ?? "Không rõ",
                        DateOfBirth = hs.DateOfBirth?.ToString("dd/MM/yyyy") ?? "Không rõ",
                        PhoneNumber = hs.PhoneNumber ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi kéo dữ liệu học sinh vãng lai: " + ex.Message);
                return; // Lỗi DB thì khỏi mở popup luôn
            }

            //gọi Popup
            var dialogContent = new WPF_Student_Management.Components.AddStudentToClassDialog
            {
                DataContext = this
            };
            await MaterialDesignThemes.Wpf.DialogHost.Show(dialogContent, "RootDialog");
        }

        [RelayCommand]
        private void SaveSelection()
        {
            //Lọc ra tụi nhỏ đang được tick chọn
            var selectedStudents = AvailableStudents.Where(s => s.IsSelected).ToList();

            if (selectedStudents.Count == 0)
            {
                return;
            }

            //Ép kiểu cái SelectedClass (đang là string) sang classId (int) để ném xuống DB
            if (string.IsNullOrEmpty(SelectedClass) || !int.TryParse(SelectedClass, out int classId))
            {
                NotificationHelper.ShowError("Lỗi: Không xác định được Lớp để xếp vào!");
                return;
            }

            //Rào chắn sĩ số
            int projectedSize = CurrentClassStudents.Count + selectedStudents.Count;
            if (projectedSize > _maxClassSize)
            {
                NotificationHelper.ShowError($"Vượt quá sĩ số quy định!\nHiện tại lớp đã có {CurrentClassStudents.Count}/{_maxClassSize} HS.\nChỉ được thêm tối đa {_maxClassSize - CurrentClassStudents.Count} HS nữa.");
                return;
            }

            //Đưa dữ liệu xuống database
            int successCount = 0;
            foreach (var hs in selectedStudents)
            {
                // Gọi Model để Insert
                bool isSavedToDb = Student.AssignStudentToClass(hs.StudentId, classId);

                if (isSavedToDb)
                {
                    // Nếu DB ok thì mới update UI (chuyển từ bảng ngoài vào bảng trong)
                    hs.IsSelected = false;
                    CurrentClassStudents.Add(hs);
                    AvailableStudents.Remove(hs);
                    successCount++;
                }
            }

            // 5. Báo cáo kết quả
            if (successCount > 0)
            {
                NotificationHelper.ShowSuccess($"Đã xếp lớp thành công cho {successCount} học sinh!");

                // Refresh lại UI
                OnPropertyChanged(nameof(ClassSizeText));
                OpenAddStudentDialogCommand.NotifyCanExecuteChanged();

                // Đóng Popup
                MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
            }
            else
            {
                NotificationHelper.ShowError("Lỗi hệ thống: Không thể lưu dữ liệu xuống Database!");
            }
        }

        // Bẫy sự kiện: Tự động chạy mỗi khi giá trị của SelectedClass thay đổi
        partial void OnSelectedClassChanged(string value)
        {
            // 1. BÁO UI CẬP NHẬT TRƯỚC! Dù có chọn lớp hay bị reset về null, cứ báo UI tính lại cái Text sĩ số
            OnPropertyChanged(nameof(ClassSizeText));
            OpenAddStudentDialogCommand.NotifyCanExecuteChanged();

            // 2. Nếu là Null (do chuyển tab reset) thì dọn sạch bảng học sinh rồi mới thoát
            if (string.IsNullOrEmpty(value))
            {
                CurrentClassStudents?.Clear(); // Xóa sạch grid học sinh ngoài màn hình
                return;
            }

            //Load danh sách học sinh của cái Lớp vừa chọn
            LoadStudentsForSelectedClass();
        }

        partial void OnSelectedGradeChanged(string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            // Xóa sạch danh sách lớp cũ
            AvailableClasses.Clear();

            // Ép kiểu chữ (string) sang số (int)
            if (int.TryParse(value, out int selectedGradeInt))
            {
                // Lọc trong kho giấu kín: Lấy những lớp có Grade khớp với số vừa chọn
                var filteredClasses = _allClassesFromDb.Where(c => c.Grade == selectedGradeInt).ToList();

                foreach (var cls in filteredClasses)
                {
                    AvailableClasses.Add(cls);
                }
            }

            // Reset lại ô chọn Lớp (để chống bug chọn Khối 10 nhưng ô Lớp vẫn đang ngậm "11A1")
            SelectedClass = null;
        }

        public void RefreshData()
        {
            // 1. Reset trắng ComboBox và Grid
            SelectedGrade = null;
            SelectedClass = null; // Đoạn này sẽ kích hoạt hàm OnSelectedClassChanged để xóa Grid
            AvailableClasses.Clear();
            AvailableGrades.Clear();

            // 2. Kéo lại Quy định sĩ số (đề phòng vừa sửa bên tab Cài đặt)
            LoadRegulations();

            // 3. SỬA LỖI LOGIC: Kéo mẻ lưới mới từ DB, nhưng CHỈ LẤY LỚP CỦA NĂM HỌC HIỆN TẠI
            _allClassesFromDb = Class.GetAllClasses().Where(c => c.AcademicYear == CurrentAcademicYear).ToList();

            // 4. Lọc lại danh sách Khối đổ lên ComboBox
            var distinctGrades = _allClassesFromDb.Select(c => c.Grade.ToString()).Distinct().OrderBy(g => g).ToList();
            foreach (var grade in distinctGrades)
            {
                AvailableGrades.Add(grade);
            }
        }
    }
}