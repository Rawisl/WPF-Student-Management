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
    public class SubjectModel
    {
        public int OrdinalNumber { get; set; }
        public string SubjectID { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string DeleteIcon { get; set; } = "X";
    }

    public partial class SubjectManagementViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<SubjectModel> _subjectList = new ObservableCollection<SubjectModel>();
        [RelayCommand]
        private void AddSubject()
        {
            //DanhSachMon.Add(new MonHocModel { OrdinalNumber = DanhSachMon.Count + 1, TenMon = "" });
        }
    }
}
