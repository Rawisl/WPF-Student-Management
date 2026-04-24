using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using WPF_Student_Management.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.ViewModels
{
    public class EmployeeManagementViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Staff> _staffList;

        public ObservableCollection<string> GenderList { get; } = new ObservableCollection<string> { "Nam", "Nữ" };
        public ObservableCollection<string> StatusList { get; } = new ObservableCollection<string> { "Active", "Inactive", "Nghỉ thai sản" };

        public ObservableCollection<Staff> StaffList
        {
            get => _staffList;
            set { _staffList = value; OnPropertyChanged(); }
        }

        private Staff _currentStaff;
        public Staff CurrentStaff
        {
            get => _currentStaff;
            set
            {
                _currentStaff = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearFormCommand { get; }

        public EmployeeManagementViewModel()
        {
            CurrentStaff = new Staff { StaffId = 0, AccountId = 0, FullName = "", Gender = "Nam", Status = "Active", HireDate = DateTime.Now };

            LoadCommand = new RelayCommand(ExecuteLoad);
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            DeleteCommand = new RelayCommand(ExecuteDelete, CanExecuteDelete);
            ClearFormCommand = new RelayCommand(ExecuteClearForm);

            bool isDesignMode = DesignerProperties.GetIsInDesignMode(new DependencyObject());
            if (!isDesignMode)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ExecuteLoad(null);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
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
                MessageBox.Show("Lỗi tải dữ liệu:\n" + ex.Message, "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteClearForm(object obj)
        {
            CurrentStaff = new Staff
            {
                StaffId = 0,
                AccountId = 0,
                FullName = "",
                Gender = "Nam",
                Status = "Active",
                HireDate = DateTime.Now
            };
        }

        private int GetNextAccountId()
        {
            string query = "SELECT ISNULL(MAX(AccountID), 0) + 1 FROM Account";
            var data = DatabaseHelper.ExecuteQuery(query);
            if (data != null && data.Rows.Count > 0)
            {
                return Convert.ToInt32(data.Rows[0][0]);
            }
            return 1;
        }

        private int GetRoleId(string roleName)
        {
            string query = $"SELECT RoleID FROM Role WHERE RoleName = N'{roleName}'";
            var data = DatabaseHelper.ExecuteQuery(query);
            if (data != null && data.Rows.Count > 0)
            {
                return Convert.ToInt32(data.Rows[0][0]);
            }
            return 4;
        }

        private void ExecuteSave(object obj)
        {
            List<string> missingFields = new List<string>();

            if (CurrentStaff.StaffId <= 0) missingFields.Add("- Mã Nhân Viên (EmployeeID)");
            if (string.IsNullOrWhiteSpace(CurrentStaff.FullName)) missingFields.Add("- Họ và Tên");
            if (string.IsNullOrWhiteSpace(CurrentStaff.PhoneNumber)) missingFields.Add("- Số điện thoại");
            if (string.IsNullOrWhiteSpace(CurrentStaff.NationalId)) missingFields.Add("- CCCD/CMND");

            string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (string.IsNullOrWhiteSpace(CurrentStaff.Email) || !Regex.IsMatch(CurrentStaff.Email, emailPattern))
            {
                missingFields.Add("- Email (Bị trống hoặc sai định dạng)");
            }

            if (missingFields.Count > 0)
            {
                string errorMessage = $"Vui lòng kiểm tra và nhập đầy đủ {missingFields.Count} thông tin sau:\n\n" + string.Join("\n", missingFields);
                MessageBox.Show(errorMessage, "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                bool isSuccess = false;
                bool isNewStaff = true;

                if (StaffList != null)
                {
                    foreach (var staff in StaffList)
                    {
                        if (staff.StaffId == CurrentStaff.StaffId)
                        {
                            isNewStaff = false;
                            break;
                        }
                    }
                }

                if (isNewStaff)
                {
                    int newAccountId = GetNextAccountId();
                    int defaultRoleId = GetRoleId("GVBM");

                    Account newAcc = new Account
                    {
                        AccountId = newAccountId,
                        RoleId = defaultRoleId,
                        Username = CurrentStaff.NationalId,
                        PasswordHash = "123456",
                        IsRequiredChangePassword = true,
                        IsActive = true
                    };

                    if (newAcc.AddAccount())
                    {
                        CurrentStaff.AccountId = newAccountId;

                        isSuccess = CurrentStaff.AddStaff();

                        if (!isSuccess)
                        {
                            Account.DeleteAccount(newAccountId);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Không thể tạo tài khoản tự động cho nhân viên!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    isSuccess = CurrentStaff.UpdateStaff();
                }

                if (isSuccess)
                {
                    MessageBox.Show("Lưu dữ liệu nhân viên thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    ExecuteLoad(null);
                    ExecuteClearForm(null);
                }
                else
                {
                    MessageBox.Show("Lưu dữ liệu thất bại, có thể Mã NV, Email hoặc CCCD đã bị trùng!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi Database:\n" + ex.Message, "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteSave(object obj) => CurrentStaff != null;

        private void ExecuteDelete(object obj)
        {
            if (CurrentStaff == null || CurrentStaff.StaffId <= 0) return;

            if (CurrentStaff.AccountId == CurrentUser.Instance.UserId)
            {
                MessageBox.Show("Không thể xóa nhân viên đang đăng nhập vào hệ thống!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa HOÀN TOÀN nhân viên '{CurrentStaff.FullName}' khỏi hệ thống không?\n\nHành động này không thể hoàn tác!",
                "Xác nhận Xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    int accountIdToDelete = CurrentStaff.AccountId;

                    bool isDeleted = Staff.DeleteStaff(CurrentStaff.StaffId);

                    if (isDeleted)
                    {
                        Account.DeleteAccount(accountIdToDelete);

                        MessageBox.Show("Đã xóa nhân viên thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        ExecuteLoad(null);
                        ExecuteClearForm(null);
                    }
                }
                catch (Microsoft.Data.SqlClient.SqlException sqlEx)
                {
                    if (sqlEx.Number == 547)
                    {
                        MessageBox.Show("Xóa dữ liệu thất bại!\n\nNhân viên này đang có dữ liệu liên kết ở bảng khác.\nVui lòng gỡ bỏ các liên kết này trước khi xóa.",
                                        "Lỗi ràng buộc dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        MessageBox.Show("Lỗi cơ sở dữ liệu:\n" + sqlEx.Message, "Lỗi SQL", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể xóa nhân viên.\nLỗi chi tiết: " + ex.Message, "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanExecuteDelete(object obj) => CurrentStaff != null && CurrentStaff.StaffId > 0;

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