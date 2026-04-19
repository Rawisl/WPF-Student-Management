using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Student_Management.Services;

namespace WPF_Student_Management.Helpers
{
    public sealed class CurrentUser
    {
        private static CurrentUser? _instance;
        private static readonly object _lock = new();
        // (Singleton Instance) can be accessed globally via CurrentUser.Instance
        public static CurrentUser Instance
        {
            get
            {
                lock (_lock)
                    return _instance ??= new CurrentUser();
            }
        }
        // Login state properties
        public int UserId { get; private set; }
        public string FullName { get; private set; } = string.Empty;
        public UserRole Role { get; private set; }
        // UserRole enum defined in other part of the project, e.g.:
        // Only for Teachers: Assigned classes and subjects
        public List<int> AssignedClasses { get; private set; } = new();
        public List<string> AssignedSubjects { get; private set; } = new();
        private CurrentUser() { } // Private constructor to prevent external instantiation via 'new CurrentUser()'
        public void Login(int userId, string fullName, UserRole role, List<int>? classes = null, List<string>? subjects = null)
        {
            UserId = userId;
            FullName = fullName;
            Role = role;
            AssignedClasses = classes ?? new();
            AssignedSubjects = subjects ?? new();
        }

        public void Logout()
        {
            UserId = 0;
            FullName = string.Empty;
            Role = UserRole.HocSinh; // Default to a non-privileged role on logout
            AssignedClasses.Clear();
            AssignedSubjects.Clear();
        }
    }
}
