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
            // Học sinh
            ViewPersonalInfo, ViewOwnGrades,
            // IT Admin
            ManageEmployees, ManageAccounts,
            // GVBM
            EditSubjectGrades, ViewSubjectReports,
            // GVCN
            ManageHomeroom, SubmitTermReport,
            // Hiệu Trưởng
            ViewGlobalStudents, ApproveRequests, ViewGlobalReports,
            // Giáo vụ
            ManageGlobalStudents, ManageClasses, ManageSubjects, ManageSystemConfig
        }

        private static readonly Dictionary<UserRole, HashSet<Feature>> _roleFeatures = new()
        {
            [UserRole.HocSinh] = new() { Feature.ViewPersonalInfo, Feature.ViewOwnGrades },
            [UserRole.ITAdmin] = new() { Feature.ManageEmployees, Feature.ManageAccounts },
            [UserRole.GVBM] = new() { Feature.EditSubjectGrades, Feature.ViewSubjectReports },
            [UserRole.GVCN] = new() { Feature.ManageHomeroom, Feature.SubmitTermReport },
            [UserRole.HieuTruong] = new() { Feature.ViewGlobalStudents, Feature.ViewGlobalReports, Feature.ApproveRequests },
            [UserRole.GiaoVu] = new() { Feature.ManageGlobalStudents, Feature.ManageClasses, Feature.ManageSubjects, Feature.ManageSystemConfig, Feature.ViewGlobalStudents, Feature.ApproveRequests }
        };

        // Check if the current user has the specified feature/permission
        public static bool HasFeature(Feature feature)
        {
            var role = CurrentUser.Instance.Role;
            return _roleFeatures.TryGetValue(role, out var features) && features.Contains(feature);
        }
    }
}
