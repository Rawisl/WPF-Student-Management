using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Student_Management.Services
{
    public enum UserRole
    {
        // Will fetch from database, but for now we hardcode it here for simplicity
        HocSinh,
        ITAdmin,
        GVBM, // Giáo viên bộ môn
        GVCN, // Giáo viên chủ nhiệm
        HieuTruong,
        GiaoVu
    }
}
