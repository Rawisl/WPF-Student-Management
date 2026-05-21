using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Student_Management.Services;

namespace WPF_Student_Management.Helpers
{
    public static class PermissionService
    {
        public enum Feature
        {
            // Người dùng hệ thống
            //Login, Logout, ChangePassWord, PersonalInfoLookup, tạm comment vì hiện tại users thì không cần phân quyền phức tạp lmg
            // Học sinh
            ViewOwnGrades,
            // IT Admin
            ManageEmployees, ManageAccounts,
            // GVBM
            EditSubjectGrades, EditSubjectReports,//
            // GVCN
            ManageHomeroom, SubmitTermReport, ResetHomeroomStudentPW, CreateRequestApplication,
            // Hiệu Trưởng
            ViewGlobalStudents, ViewEmployeeList, ViewGlobalReports, ApproveRequests, ViewRequests,
            // Giáo vụ
            ManageGlobalStudents, ManageClasses, ManageSubjects, ManageSystemConfig, ManageTeachingAssign, ExecuteRequests,
        }

        private static readonly Dictionary<UserRole, HashSet<Feature>> _roleFeatures = new()
        {
            [UserRole.HocSinh] = new() { Feature.ViewOwnGrades },
            [UserRole.ITAdmin] = new() { Feature.ViewEmployeeList, Feature.ManageEmployees, Feature.ManageAccounts },
            [UserRole.GVBM] = new() { Feature.EditSubjectGrades, Feature.EditSubjectReports },
            [UserRole.GVCN] = new() { Feature.ManageHomeroom, Feature.SubmitTermReport, Feature.ResetHomeroomStudentPW, Feature.CreateRequestApplication, Feature.EditSubjectGrades, Feature.EditSubjectReports },
            [UserRole.HieuTruong] = new() { Feature.ViewGlobalStudents, Feature.ViewGlobalReports, Feature.ApproveRequests, Feature.ViewEmployeeList, Feature.ViewRequests },
            [UserRole.GiaoVu] = new() { Feature.ManageGlobalStudents, Feature.ManageClasses, Feature.ManageSubjects, Feature.ManageSystemConfig, Feature.ViewGlobalStudents, Feature.ExecuteRequests, Feature.ManageTeachingAssign, Feature.ViewRequests }
        };

        // Check if the current user has the specified feature/permission
        public static bool HasFeature(Feature feature)
        {
            var role = CurrentUser.Instance.Role;
            return _roleFeatures.TryGetValue(role, out var features) && features.Contains(feature);
        }
    }
}
