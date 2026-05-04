using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;

namespace WPF_Student_Management.ViewModels
{
    public class EmployeeManagementViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Staff> _staffList;

        public ObservableCollection<string> GenderList { get; } = new ObservableCollection<string> { "Nam", "Nữ" };
        public ObservableCollection<string> StatusList { get; } = new ObservableCollection<string> { "Active", "Inactive" };

        public ObservableCollection<Role> RoleList { get; set; }

        public ObservableCollection<Staff> StaffList
        {
            get => _staffList;
            set { _staffList = value; OnPropertyChanged(); }
        }

        private Staff _currentStaff;
        public Staff CurrentStaff
        {
            get => _currentStaff;
            set { _currentStaff = value; OnPropertyChanged(); }
        }

        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand OpenAddDialogCommand { get; }
        public ICommand EditCommand { get; }

        public EmployeeManagementViewModel()
        {
            LoadCommand = new RelayCommand(ExecuteLoad);
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            DeleteCommand = new RelayCommand(ExecuteDelete);

            OpenAddDialogCommand = new RelayCommand(ExecuteOpenAddDialog);
            EditCommand = new RelayCommand(ExecuteEdit);

            bool isDesignMode = DesignerProperties.GetIsInDesignMode(new DependencyObject());
            if (!isDesignMode)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ExecuteLoad(null);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }

            RoleList = new ObservableCollection<Role>(Role.GetAllRoles());
        }

        private void ExecuteLoad(object obj)
        {
            try
            {
                var list = Staff.GetAllStaff();
                StaffList = new ObservableCollection<Staff>(list);
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi tải dữ liệu:\n" + ex.Message);
            }
        }

        // --- CÁC HÀM XỬ LÝ DIALOG ---
        private async void ExecuteOpenAddDialog(object obj)
        {
            // Reset form
            CurrentStaff = new Staff { StaffId = 0, AccountId = 0, FullName = "", Gender = "Nam", Status = "Active", HireDate = DateTime.Now };

            // Gọi UserControl Dialog mới tạo
            var dialog = new Components.EmployeeDetailDialog { DataContext = this };
            await DialogHost.Show(dialog, "RootDialog");
        }

        private async void ExecuteEdit(object obj)
        {
            if (obj is Staff staff)
            {
                CurrentStaff = staff;
                var dialog = new Components.EmployeeDetailDialog { DataContext = this };
                await DialogHost.Show(dialog, "RootDialog");
            }
        }

        private int GetRoleId(string roleName)
        {
            string query = $"SELECT RoleID FROM Role WHERE RoleName = N'{roleName}'";
            var data = DatabaseHelper.ExecuteQuery(query);
            if (data != null && data.Rows.Count > 0)
                return Convert.ToInt32(data.Rows[0][0]);
            return 4;
        }

        private void ExecuteSave(object obj)
        {
            try
            {
                bool isNewStaff = (CurrentStaff.StaffId == 0);
                bool isSuccess = false;

                if (isNewStaff)
                {
                    var accountInfo = CurrentStaff.ReceiveNewStaff();

                    if (accountInfo != null)
                    {
                        NotificationHelper.ShowSuccess(
                            $"Tiếp nhận giáo viên thành công!\n\n" +
                            $"Tài khoản: {accountInfo.Value.Username}\n" +
                            $"Mật khẩu: {accountInfo.Value.Password}\n\n" +
                            $"Lưu ý: Mật khẩu mặc định là tên + 4 số cuối SĐT.");

                        isSuccess = true;
                    }
                }
                else
                {
                    // CẬP NHẬT THÔNG TIN CHO NHÂN VIÊN ĐÃ CÓ
                    isSuccess = CurrentStaff.UpdateStaff();
                    if (isSuccess) NotificationHelper.ShowSuccess("Cập nhật thông tin thành công!");
                }

                // Nếu mọi thứ ok thì load lại danh sách và đóng cửa sổ
                if (isSuccess)
                {
                    ExecuteLoad(null);
                    DialogHost.Close("RootDialog");
                }
                else if (isNewStaff)
                {
                    NotificationHelper.ShowError("Tiếp nhận thất bại. Có thể do trùng số CCCD hoặc Số điện thoại trong hệ thống!");
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi hệ thống: " + ex.Message);
            }
        }

        private bool CanExecuteSave(object obj)
        {
            if (CurrentStaff == null) return false;

            // Kiểm tra Họ Tên: Phải có ít nhất 2 từ (Họ và Tên)
            if (string.IsNullOrWhiteSpace(CurrentStaff.FullName) ||
                CurrentStaff.FullName.Trim().Split(' ').Length < 2)
                return false;

            // Kiểm tra SĐT: Phải đúng 10 số và bắt đầu bằng số 0
            string phonePattern = @"^0\d{9}$";
            if (string.IsNullOrWhiteSpace(CurrentStaff.PhoneNumber) ||
                !System.Text.RegularExpressions.Regex.IsMatch(CurrentStaff.PhoneNumber, phonePattern))
                return false;

            // Kiểm tra CCCD: Ít nhất 9 hoặc 12 số
            if (string.IsNullOrWhiteSpace(CurrentStaff.NationalId) ||
                CurrentStaff.NationalId.Length < 9)
                return false;

            // Kiểm tra Email chuẩn
            string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (string.IsNullOrWhiteSpace(CurrentStaff.Email) ||
                !System.Text.RegularExpressions.Regex.IsMatch(CurrentStaff.Email, emailPattern))
                return false;

            return true;
        }

        private void ExecuteDelete(object obj)
        {
            var staffToDelete = obj as Staff ?? CurrentStaff;
            if (staffToDelete == null || staffToDelete.StaffId <= 0) return;

            if (staffToDelete.AccountId == CurrentUser.Instance.UserId)
            {
                NotificationHelper.ShowError("Không thể xóa nhân viên đang đăng nhập vào hệ thống!");
                return;
            }

            bool result = NotificationHelper.ShowConfirm($"Bạn có chắc chắn muốn xóa HOÀN TOÀN nhân viên '{staffToDelete.FullName}' khỏi hệ thống không?\n\nHành động này không thể hoàn tác!");

            if (result)
            {
                try
                {
                    int accountIdToDelete = staffToDelete.AccountId;
                    bool isDeleted = Staff.DeleteStaff(staffToDelete.StaffId);

                    if (isDeleted)
                    {
                        Account.DeleteAccount(accountIdToDelete);
                        NotificationHelper.ShowSuccess("Đã xóa nhân viên thành công!");
                        ExecuteLoad(null);
                    }
                }
                catch (Microsoft.Data.SqlClient.SqlException sqlEx)
                {
                    if (sqlEx.Number == 547)
                        NotificationHelper.ShowWarning("Xóa dữ liệu thất bại!\n\nNhân viên này đang có dữ liệu liên kết ở bảng khác.\nVui lòng gỡ bỏ các liên kết này trước khi xóa.");
                    else
                        NotificationHelper.ShowError("Lỗi cơ sở dữ liệu:\n" + sqlEx.Message);
                }
                catch (Exception ex)
                {
                    NotificationHelper.ShowError("Không thể xóa nhân viên.\nLỗi chi tiết: " + ex.Message);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);
    }
}