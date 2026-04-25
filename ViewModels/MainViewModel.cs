using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.ViewModels

{
    internal partial class MainViewModel : ObservableObject
    {
        // Tạo sẵn một đối tượng để dùng chung (singleton trong nội bộ MainVM)
        public ClassRosterViewModel ClassRosterVM { get; } = new ClassRosterViewModel();
        public SubjectGradebookViewModel SubjectGradebookVM { get; } = new SubjectGradebookViewModel();
        public HomeroomDashboardViewModel HomeroomDashboardVM { get; } = new HomeroomDashboardViewModel();
        public ClassManagementViewModel ClassManagementVM { get; } = new ClassManagementViewModel();
        public SubjectManagementViewModel SubjectManagementVM { get; } = new SubjectManagementViewModel();
        public RegulationSettingsViewModel RegulationSettingsVM { get; } = new RegulationSettingsViewModel();
        public SettingsViewModel SettingsVM { get; } = new SettingsViewModel();
        public GlobalStudentManagementViewModel GlobalStudentManagementVM { get; } = new GlobalStudentManagementViewModel();

        public EmployeeManagementViewModel EmployeeManagementVM { get; } = new EmployeeManagementViewModel();
        public EnrollmentChangeExecutionViewModel EnrollmentChangeExecutionVM { get; } = new EnrollmentChangeExecutionViewModel();
        public GlobalSummaryReportViewModel GlobalSummaryReportVM { get; } = new GlobalSummaryReportViewModel();
        public GradeLookupViewModel GradeLookupVM { get; } = new GradeLookupViewModel();
        public TeachingAssignmentViewModel TeachingAssignmentVM { get; } = new TeachingAssignmentViewModel();

        // Biến lưu trang hiện tại đang hiển thị trên ContentControl
        [ObservableProperty]
        private object _currentView;

        public MainViewModel()
        {
            CurrentView = ClassRosterVM; // Trang mặc định
        }

        [RelayCommand]
        private void Navigate(object destinationViewModel)
        {
            // 1. KIỂM TRA CHỐT CHẶN
            // Nếu trang hiện tại ĐANG LÀ trang Cài Đặt (BM6) VÀ Cờ Dirty đang bật
            if (CurrentView is RegulationSettingsViewModel RegulationSettingsVM && RegulationSettingsVM.HasUnsavedChanges)
            {
                //Bật Cảnh báo
                bool isConfirmSwitchTab = NotificationHelper.ShowConfirm("Bạn có thay đổi chưa lưu! Xác nhận rời đi và mất dữ liệu?");

                if (!isConfirmSwitchTab) return; // Nếu chọn Hủy thì ở lại
                RegulationSettingsVM.LoadDataFromDatabase();
            }

            // 2. NẾU AN TOÀN -> THỰC HIỆN CHUYỂN TRANG
            CurrentView = destinationViewModel;
        }
    }
}
