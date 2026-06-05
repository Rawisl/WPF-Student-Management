using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using WPF_Student_Management.Helpers;
using WPF_Student_Management.Models;
using WPF_Student_Management.Services;

namespace WPF_Student_Management.ViewModels
{
    public partial class PersonalInfoLookupViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableObject _currentProfileDataContext;

        public PersonalInfoLookupViewModel()
        {
            LoadProfile();
        }

        private void LoadProfile()
        {
            //Phải đăng nhập mới được xem
            if (CurrentUser.Instance == null || CurrentUser.Instance.UserId == 0)
                return;

            int currentAccountId = CurrentUser.Instance.UserId;
            int roleId = (int)CurrentUser.Instance.Role;

            try
            {
                if (roleId == 0)
                {
                    string query = "SELECT * FROM Student WHERE AccountID = @AccountID";
                    DataTable dt = DatabaseHelper.ExecuteQuery(query, new[] { new SqlParameter("@AccountID", currentAccountId) });

                    if (dt.Rows.Count > 0)
                    {
                        DataRow row = dt.Rows[0];
                        Student studentModel = new Student
                        {
                            StudentId = row["StudentID"].ToString(),
                            FullName = row["FullName"].ToString(),
                            Gender = row["Gender"].ToString(),
                            DateOfBirth = row["DateOfBirth"] != DBNull.Value ? Convert.ToDateTime(row["DateOfBirth"]) : null,
                            PhoneNumber = row["PhoneNumber"].ToString(),
                            Email = row["Email"].ToString(),
                            Address = row["Address"].ToString(),
                            FamilyBackground = row["FamilyBackground"].ToString(),
                            GuardianName = row["GuardianName"].ToString(),
                            GuardianPhoneNumber = row["GuardianPhoneNumber"].ToString(),
                            AccountId = currentAccountId
                        };

                        var studentVM = new StudentProfileDetailViewModel(studentModel, isReadOnly: true);

                        // Bắn VM vào ContentControl -> Tự động render form Học sinh
                        CurrentProfileDataContext = studentVM;
                    }
                    else
                    {
                        NotificationHelper.ShowError("Không tìm thấy hồ sơ học sinh liên kết với tài khoản này.");
                    }
                }
                else
                {
                    string query = @"
                        SELECT e.*, a.RoleID 
                        FROM Employee e
                        JOIN Account a ON e.AccountID = a.AccountID
                        WHERE e.AccountID = @AccountID";

                    DataTable dt = DatabaseHelper.ExecuteQuery(query, new[] { new SqlParameter("@AccountID", currentAccountId) });

                    if (dt.Rows.Count > 0)
                    {
                        DataRow row = dt.Rows[0];
                        Staff staffModel = new Staff
                        {
                            StaffId = Convert.ToInt32(row["EmployeeID"]),
                            FullName = row["FullName"].ToString(),
                            Gender = row["Gender"].ToString(),
                            NationalId = row["NationalID"].ToString(),
                            PhoneNumber = row["PhoneNumber"].ToString(),
                            Email = row["Email"].ToString(),
                            HometownAddress = row["HometownAddress"].ToString(),
                            HireDate = row["HireDate"] != DBNull.Value ? Convert.ToDateTime(row["HireDate"]) : DateTime.Now,
                            Status = row["Status"].ToString(),
                            Specialization = row["Specialization"] != DBNull.Value ? Convert.ToInt32(row["Specialization"]) : (int?)null,
                            AccountId = currentAccountId,
                            RoleId = Convert.ToInt32(row["RoleID"])
                        };

                        var employeeVM = new EmployeeProfileDetailViewModel(staffModel, isReadOnly: true);
                        CurrentProfileDataContext = employeeVM;
                    }
                    else
                    {
                        NotificationHelper.ShowError("Không tìm thấy hồ sơ nhân sự liên kết với tài khoản này.");
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi hệ thống khi tải hồ sơ: " + ex.Message);
            }
        }
    }
}