using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Student_Management.ViewModels
{
    public class ClassModel
    {
        public int OrdinalNumber { get; set; }
        public string ClassID { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string DeleteIcon { get; set; } = "X";
    }

    public partial class ClassManagementViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ClassModel> _classList = new ObservableCollection<ClassModel>();

        // --- LOGIC THÊM LỚP/MÔN ---
        [RelayCommand]
        private void AddClass()
        {
            //DanhSachLop.Add(new LopHocModel { OrdinalNumber = DanhSachLop.Count + 1, Khoi = "", TenLop = "" });
        }
    }



}
