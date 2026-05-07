using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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

        public ICommand LogoutCommand { get; }

        public MainViewModel()
        {
            CurrentView = ClassRosterVM; // Trang mặc định
            LogoutCommand = new RelayCommand(ExecuteLogout);
        }

        [RelayCommand]
        private void Navigate(object destinationViewModel)
        {
            // 1. KIỂM TRA CHỐT CHẶN TỪ MÀN HÌNH QUY ĐỊNH
            if (CurrentView is RegulationSettingsViewModel regVM)
            {
                if (regVM.HasUnsavedChanges)
                {
                    bool isConfirmSwitchTab = NotificationHelper.ShowConfirm("Bạn có thay đổi chưa lưu! Xác nhận rời đi và mất dữ liệu?");
                    if (!isConfirmSwitchTab) return; // Chọn Hủy thì ở lại
                }

                // LUÔN LUÔN REFRESH: Cho dù có thay đổi hay không, cứ rời đi là nạp lại DB để dọn sạch
                regVM.LoadDataFromDatabase();
            }

            // 2. KIỂM TRA CHỐT CHẶN TỪ MÀN HÌNH NHẬP ĐIỂM
            if (CurrentView is SubjectGradebookViewModel gradebookVM)
            {
                if (gradebookVM.HasUnsavedChanges)
                {
                    bool isConfirmSwitchTab = NotificationHelper.ShowConfirm("Màn hình Nhập điểm đang có dữ liệu chưa lưu!\nNếu chuyển sang màn hình khác, điểm sẽ bị mất. Bạn có chắc chắn muốn thoát không?");
                    if (!isConfirmSwitchTab) return; // Chọn Hủy thì ở lại nhập tiếp
                }

                // LUÔN LUÔN REFRESH: Cứ rời đi là dọn sạch ComboBox và DataGrid
                gradebookVM.RefreshData();
            }

            // 3. NẾU AN TOÀN -> THỰC HIỆN CHUYỂN TRANG
            CurrentView = destinationViewModel;
        }

        private void ExecuteLogout(object obj)
        {
            // --- BẢO VỆ DỮ LIỆU TRƯỚC KHI ĐĂNG XUẤT ---
            if (CurrentView is RegulationSettingsViewModel regVM && regVM.HasUnsavedChanges)
            {
                bool confirm = NotificationHelper.ShowConfirm("Bạn có quy định chưa lưu! Vẫn muốn đăng xuất?");
                if (!confirm) return;
            }

            if (CurrentView is SubjectGradebookViewModel gradebookVM && gradebookVM.HasUnsavedChanges)
            {
                bool confirm = NotificationHelper.ShowConfirm("Bạn có điểm chưa lưu! Vẫn muốn đăng xuất?");
                if (!confirm) return;
            }
            // ------------------------------------------

            // Xóa thông tin đăng nhập trong Singleton
            CurrentUser.Instance.Logout();

            // Mở lại cửa sổ đăng nhập
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();

            // Đóng cửa sổ hiện tại (MainWindow)
            if (obj is Window mainWindow)
            {
                mainWindow.Close();
            }
            else
            {
                // Cách dự phòng đóng tất cả các cửa sổ cũ
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is MainWindow)
                    {
                        window.Close();
                        break;
                    }
                }
            }
        }
    }
}
