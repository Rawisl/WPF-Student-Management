using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.ViewModels
{
    public partial class SidebarViewModel : ObservableObject
    {
        public string DisplayName => CurrentUser.Instance?.FullName ?? "Người Dùng";
        public string DisplayRole => GetRoleName(CurrentUser.Instance?.Role);

        public Visibility UserInfoVisibility => CurrentUser.Instance != null && CurrentUser.Instance.UserId > 0
                                                ? Visibility.Visible : Visibility.Collapsed;

        public SidebarViewModel()
        {
            CurrentUser.Instance.UserChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(DisplayRole));
                OnPropertyChanged(nameof(UserInfoVisibility));
            };
        }

        private string GetRoleName(Services.UserRole? role)
        {
            if (role == null)
                return string.Empty;
            return role switch
            {
                Services.UserRole.HieuTruong => "HIỆU TRƯỞNG",
                Services.UserRole.GVCN => "GV. CHỦ NHIỆM",
                Services.UserRole.GVBM => "GV. BỘ MÔN",
                Services.UserRole.GiaoVu => "GIÁO VỤ",
                Services.UserRole.ITAdmin => "QUẢN TRỊ VIÊN",
                Services.UserRole.HocSinh => "HỌC SINH",
                _ => "NGƯỜI DÙNG"
            };
        }
    }
}