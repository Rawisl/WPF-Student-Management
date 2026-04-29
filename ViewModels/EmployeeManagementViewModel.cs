using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;

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
                NotificationHelper.ShowError("Lỗi tải dữ liệu:\n" + ex.Message);
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

        // ĐÃ XÓA HÀM GetNextAccountId() VÌ ĐÃ CÓ AUTO-ID

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

            // Bỏ qua check StaffId vì thêm mới ID tự động sinh
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
                string errorMessage = $"Vui lòng kiểm tra và nhập đầy đủ thông tin sau:\n\n" + string.Join("\n", missingFields);
                NotificationHelper.ShowWarning(errorMessage);
                return;
            }

            try
            {
                bool isSuccess = false;
                bool isNewStaff = (CurrentStaff.StaffId == 0);

                if (isNewStaff)
                {
                    int defaultRoleId = GetRoleId("GVBM");

                    Account newAcc = new Account
                    {
                        RoleId = defaultRoleId,
                        Username = CurrentStaff.NationalId,
                        PasswordHash = "123456",
                        IsRequiredChangePassword = true,
                        IsActive = true
                    };

                    // ÁP DỤNG AUTO-ID: Lấy ID tài khoản vừa được SQL sinh ra
                    int generatedAccountId = newAcc.AddAccountAndGetId();

                    if (generatedAccountId > 0)
                    {
                        CurrentStaff.AccountId = generatedAccountId;
                        isSuccess = CurrentStaff.AddStaff();

                        // Rollback nếu thêm nhân viên thất bại
                        if (!isSuccess)
                        {
                            Account.DeleteAccount(generatedAccountId);
                        }
                    }
                    else
                    {
                        NotificationHelper.ShowError("Không thể tạo tài khoản tự động cho nhân viên!");
                        return;
                    }
                }
                else
                {
                    isSuccess = CurrentStaff.UpdateStaff();
                }

                if (isSuccess)
                {
                    NotificationHelper.ShowSuccess("Lưu dữ liệu nhân viên thành công!");
                    ExecuteLoad(null);
                    ExecuteClearForm(null);
                }
                else
                {
                    NotificationHelper.ShowError("Lưu dữ liệu thất bại, có thể Mã NV, Email hoặc CCCD đã bị trùng!");
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi Database:\n" + ex.Message);
            }
        }

        private bool CanExecuteSave(object obj) => CurrentStaff != null;

        private void ExecuteDelete(object obj)
        {
            if (CurrentStaff == null || CurrentStaff.StaffId <= 0) return;

            if (CurrentStaff.AccountId == CurrentUser.Instance.UserId)
            {
                NotificationHelper.ShowError("Không thể xóa nhân viên đang đăng nhập vào hệ thống!");
                return;
            }

            bool result = NotificationHelper.ShowConfirm($"Bạn có chắc chắn muốn xóa HOÀN TOÀN nhân viên '{CurrentStaff.FullName}' khỏi hệ thống không?\n\nHành động này không thể hoàn tác!");

            if (result)
            {
                try
                {
                    int accountIdToDelete = CurrentStaff.AccountId;

                    bool isDeleted = Staff.DeleteStaff(CurrentStaff.StaffId);

                    if (isDeleted)
                    {
                        Account.DeleteAccount(accountIdToDelete);

                        NotificationHelper.ShowSuccess("Đã xóa nhân viên thành công!");
                        ExecuteLoad(null);
                        ExecuteClearForm(null);
                    }
                }
                catch (Microsoft.Data.SqlClient.SqlException sqlEx)
                {
                    if (sqlEx.Number == 547)
                    {
                        NotificationHelper.ShowWarning("Xóa dữ liệu thất bại!\n\nNhân viên này đang có dữ liệu liên kết ở bảng khác.\nVui lòng gỡ bỏ các liên kết này trước khi xóa.");
                    }
                    else
                    {
                        NotificationHelper.ShowError("Lỗi cơ sở dữ liệu:\n" + sqlEx.Message);
                    }
                }
                catch (Exception ex)
                {
                    NotificationHelper.ShowError("Không thể xóa nhân viên.\nLỗi chi tiết: " + ex.Message);
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