using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        [ObservableProperty] private string _studentID = string.Empty;
        [ObservableProperty] private string _fullName = string.Empty;
        [ObservableProperty] private string _gender = string.Empty;
        [ObservableProperty] private string _dateOfBirth = string.Empty;
    }

    public partial class ClassRosterViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<SelectableStudentItem> _currentClassStudents;

        [ObservableProperty]
        private ObservableCollection<SelectableStudentItem> _availableStudents;

        [ObservableProperty]
        private string _selectedGrade;

        [ObservableProperty]
        private string _selectedClass;

        // BIẾN LƯU SĨ SỐ TỐI ĐA (Kéo từ DB lên, giả định mặc định là 40)
        private int _maxClassSize = 40;

        public string ClassSizeText => $"Sĩ số: {CurrentClassStudents?.Count ?? 0} / {_maxClassSize}";

        public ClassRosterViewModel()
        {
            AvailableStudents = new ObservableCollection<SelectableStudentItem>();
            CurrentClassStudents = new ObservableCollection<SelectableStudentItem>();

            LoadRegulations(); // Kéo quy định lên trước
            LoadMockData();
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

        private void LoadMockData()
        {
            CurrentClassStudents.Add(new SelectableStudentItem { FullName = "Phạm Thị D", StudentID = "HS004", Gender = "Nữ", DateOfBirth = "15/08/2008" });
            CurrentClassStudents.Add(new SelectableStudentItem { FullName = "Hoàng Văn E", StudentID = "HS005", Gender = "Nam", DateOfBirth = "22/11/2008" });
            CurrentClassStudents.Add(new SelectableStudentItem { FullName = "Vũ Thị F", StudentID = "HS006", Gender = "Nữ", DateOfBirth = "05/01/2009" });

            AvailableStudents.Add(new SelectableStudentItem { FullName = "Trần Văn G", StudentID = "HS007", Gender = "Nam", DateOfBirth = "12/03/2008" });
            AvailableStudents.Add(new SelectableStudentItem { FullName = "Lê Thị H", StudentID = "HS008", Gender = "Nữ", DateOfBirth = "29/12/2008" });

            OnPropertyChanged(nameof(ClassSizeText));
            OpenAddStudentDialogCommand.NotifyCanExecuteChanged(); // Ép check lại nút Thêm HS
        }

        // LỖ HỔNG 2: Check điều kiện để Enable/Disable nút "+ Thêm học sinh"
        private bool CanOpenAddStudent()
        {
            // Tương lai: Thêm điều kiện && !string.IsNullOrEmpty(_selectedClass)
            return CurrentClassStudents != null && CurrentClassStudents.Count < _maxClassSize;
        }

        [RelayCommand(CanExecute = nameof(CanOpenAddStudent))]
        private async Task OpenAddStudentDialog()
        {
            var dialogContent = new WPF_Student_Management.Components.AddStudentToClassDialog
            {
                DataContext = this
            };
            await MaterialDesignThemes.Wpf.DialogHost.Show(dialogContent, "RootDialog");
        }

        [RelayCommand]
        private void SaveSelection()
        {
            var selectedStudents = AvailableStudents.Where(s => s.IsSelected).ToList();

            if (selectedStudents.Count == 0)
            {
                NotificationHelper.ShowWarning("Bro chưa chọn học sinh nào cả!");
                return;
            }

            // LỖ HỔNG 3: Chặn nếu nhét quá nhiều
            int projectedSize = CurrentClassStudents.Count + selectedStudents.Count;
            if (projectedSize > _maxClassSize)
            {
                NotificationHelper.ShowError($"Vượt quá sĩ số quy định!\nHiện tại lớp đã có {CurrentClassStudents.Count}/{_maxClassSize} HS.\nChỉ được thêm tối đa {_maxClassSize - CurrentClassStudents.Count} HS nữa.");
                // Return luôn, KHÔNG ĐÓNG Popup
                return;
            }

            foreach (var hs in selectedStudents)
            {
                hs.IsSelected = false;
                CurrentClassStudents.Add(hs);
                AvailableStudents.Remove(hs);
            }

            NotificationHelper.ShowSuccess($"Đã xếp lớp thành công cho {selectedStudents.Count} học sinh!");

            OnPropertyChanged(nameof(ClassSizeText));
            OpenAddStudentDialogCommand.NotifyCanExecuteChanged(); // Ép check lại nút bật Pop-up

            MaterialDesignThemes.Wpf.DialogHost.Close("RootDialog");
        }
    }
}