using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.ViewModels
{
    public partial class StudentGradeDisplay : ObservableObject
    {
        public int OrdinalNumber { get; set; }
        public string StudentID { get; set; } // ID ẩn để lưu CSDL
        public string FullName { get; set; }

        // === ĐIỂM 15 PHÚT - Ràng buộc 1-10 ===
        private double? _diem15Phut;
        public double? Diem15Phut
        {
            get => _diem15Phut;
            set
            {
                if (value.HasValue)
                {
                    if (value.Value < 0) value = 0;
                    if (value.Value > 10) value = 10;
                }

                // Nếu giá trị có thay đổi thì gán và báo cho Giao diện để tính điểm TB
                if (SetProperty(ref _diem15Phut, value))
                {
                    OnPropertyChanged(nameof(DiemTB));
                }
            }
        }

        // === ĐIỂM 1 TIẾT ===
        private double? _diem1Tiet;
        public double? Diem1Tiet
        {
            get => _diem1Tiet;
            set
            {
                if (value.HasValue)
                {
                    if (value.Value < 0) value = 0;
                    if (value.Value > 10) value = 10;
                }

                if (SetProperty(ref _diem1Tiet, value))
                {
                    OnPropertyChanged(nameof(DiemTB));
                }
            }
        }

        // === CỘT ĐIỂM TB
        public double? DiemTB
        {
            get
            {
                if (Diem15Phut.HasValue && Diem1Tiet.HasValue)
                    return Math.Round((Diem15Phut.Value + Diem1Tiet.Value * 2) / 3, 1);
                return null;
            }
        }
    }
    public partial class SubjectGradebookViewModel : ObservableObject
    {
        // === COMBOBOX ===

        //public ObservableCollection<Lop> DanhSachLop { get; set; } = new();
        //public ObservableCollection<MonHoc> DanhSachMon { get; set; } = new();
        public ObservableCollection<int> DanhSachHocKy { get; set; } = new() { 1, 2 };

        //[ObservableProperty] private Lop? _lopDuocChon;
        //[ObservableProperty] private MonHoc? _monDuocChon;

        [ObservableProperty] private int _hocKyDuocChon = 1;

        // Năm học lấy chuẩn theo DB của Long
        private string _namHocHienTai = "2023 - 2024";

        // === DATAGRID ===
        public ObservableCollection<StudentGradeDisplay> DanhSachHocSinh { get; set; } = new();

        public SubjectGradebookViewModel()
        {
            ComboBoxLoadData();
        }

        private void ComboBoxLoadData()
        {
            //var dsLop = Lop.LayDanhSach();
            //var dsMon = MonHoc.LayDanhSach();

            //foreach (var l in dsLop) DanhSachLop.Add(l);
            //foreach (var m in dsMon) DanhSachMon.Add(m);

            //if (DanhSachLop.Count > 0) LopDuocChon = DanhSachLop[0];
            //if (DanhSachMon.Count > 0) MonDuocChon = DanhSachMon[0];
        }

        // === LẤY DANH SÁCH ===
        [RelayCommand]
        private void GetList()
        {
            //if (LopDuocChon == null || MonDuocChon == null)
            //{
            //    MessageBox.Show("Vui lòng chọn Lớp và Môn học!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}

            //    try
            //    {
            //        DanhSachHocSinh.Clear();

            //        string sqlQuery = @"
            //        SELECT hs.StudentID, hs.FullName, d.Diem15p, d.Diem1Tiet 
            //        FROM HOCSINH hs
            //        INNER JOIN PHANLOP pl ON hs.StudentID = pl.StudentID
            //        LEFT JOIN DIEMSO d ON pl.MaPhanLop = d.MaPhanLop AND d.MaMonHoc = @MaMonHoc
            //        WHERE pl.MaLop = @MaLop AND pl.HocKy = @HocKy AND pl.NamHoc = @NamHoc";

            //        SqlParameter[] sqlParams = {
            //        new SqlParameter("@MaMonHoc", MonDuocChon.MaMonHoc),
            //        new SqlParameter("@MaLop", LopDuocChon.MaLop),
            //        new SqlParameter("@HocKy", HocKyDuocChon),
            //        new SqlParameter("@NamHoc", _namHocHienTai)
            //    };

            //        DataTable dt = DatabaseHelper.ExecuteQuery(sqlQuery, sqlParams);

            //        if (dt == null || dt.Rows.Count == 0)
            //        {
            //            NotificationHelper.ShowWarning("Không tìm thấy học sinh nào trong Lớp/Học kỳ này!");
            //            return;
            //        }

            //        int count = 1;
            //        foreach (DataRow row in dt.Rows)
            //        {
            //            var hs = new StudentGradeDisplay
            //            {
            //                OrdinalNumber = count++,
            //                StudentID = row["StudentID"]?.ToString() ?? "",
            //                FullName = row["FullName"]?.ToString() ?? "N/A"
            //            };

            //            // Ép kiểu điểm số an toàn
            //            if (row["Diem15p"] != DBNull.Value) hs.Diem15Phut = Convert.ToDouble(row["Diem15p"]);
            //            if (row["Diem1Tiet"] != DBNull.Value) hs.Diem1Tiet = Convert.ToDouble(row["Diem1Tiet"]);

            //            DanhSachHocSinh.Add(hs);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        NotificationHelper.ShowError($"Lỗi Database:\n{ex.Message}");
            //    }
            //}
        }

        // === LƯU BẢNG ĐIỂM ===
        [RelayCommand]
        private void SaveGradeData()
        {
            //if (LopDuocChon == null || MonDuocChon == null) return;

            if (DanhSachHocSinh.Count == 0)
            {
                NotificationHelper.ShowWarning("Bảng điểm đang trống, không có gì để lưu!");
                return;
            }

            int thanhCong = 0;
            int thatBai = 0;

            //foreach (var hs in DanhSachHocSinh)
            //{
            //    // Chỉ lưu những bạn có nhập đủ cả 2 cột điểm
            //    if (hs.Diem15Phut.HasValue && hs.Diem1Tiet.HasValue)
            //    {
            //        try
            //        {
            //            bool result = DiemSo.LuuDiem(
            //                StudentID: hs.StudentID,
            //                maMonHoc: MonDuocChon.MaMonHoc,
            //                hocKy: HocKyDuocChon,
            //                namHoc: _namHocHienTai,
            //                diem15p: hs.Diem15Phut.Value,
            //                diem1Tiet: hs.Diem1Tiet.Value
            //            );

            //            if (result) thanhCong++;
            //            else thatBai++;
            //        }
            //        catch (Exception)
            //        {
            //            thatBai++;
            //        }
            //    }
            //}
            NotificationHelper.ShowSuccess("Đã lưu thành công!");
        }
    }
}
