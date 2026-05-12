using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Data;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.ViewModels
{
    public class GlobalReportItem
    {
        public int STT { get; set; }
        public string ClassName { get; set; }
        public int TotalStudents { get; set; }
        public int PassedStudents { get; set; }
        public string PassRate { get; set; }
    }

    // Class chứa dữ liệu chi tiết học sinh trong Popup
    public class StudentStatusItem
    {
        public int STT { get; set; }
        public string StudentId { get; set; }
        public string FullName { get; set; }
        public string Status { get; set; }
    }

    public partial class GlobalSummaryReportViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _selectedSemester = "Học kỳ 1";

        [ObservableProperty]
        private string _selectedAcademicYear = "2025-2026";

        public ObservableCollection<string> SemesterList { get; } = new ObservableCollection<string> { "Học kỳ 1", "Học kỳ 2" };
        public ObservableCollection<string> AcademicYearList { get; } = new ObservableCollection<string> { "2024-2025", "2025-2026", "2026-2027" };

        [ObservableProperty]
        private ObservableCollection<GlobalReportItem> _reportData = new();

        [ObservableProperty]
        private bool _isDataReady = false;

        // --- CÁC BIẾN CHO TÍNH NĂNG POPUP CHI TIẾT ---
        [ObservableProperty]
        private GlobalReportItem _selectedReportItem;

        [ObservableProperty]
        private ObservableCollection<StudentStatusItem> _detailList = new();

        [ObservableProperty]
        private bool _isDetailOpen = false;

        [ObservableProperty]
        private string _detailClassName = "";

        partial void OnSelectedReportItemChanged(GlobalReportItem value)
        {
            if (value != null)
            {
                LoadClassDetail(value.ClassName);
            }
        }

        [RelayCommand]
        private void CloseDetail()
        {
            IsDetailOpen = false;
            SelectedReportItem = null;
        }

        [RelayCommand]
        private void LoadReport()
        {
            try
            {
                IsDataReady = false;
                ReportData.Clear();

                string checkLockQuery = @"
                    DECLARE @TotalClasses INT = (SELECT COUNT(*) FROM Class WHERE AcademicYear = @AcademicYear);
                    DECLARE @LockedReports INT = (SELECT COUNT(*) FROM ClassReport WHERE Semester = @Semester AND AcademicYear = @AcademicYear AND IsLocked = 1);
                    SELECT @TotalClasses AS TotalClasses, @LockedReports AS LockedReports;";

                SqlParameter[] lockParams = {
                    new SqlParameter("@Semester", SelectedSemester),
                    new SqlParameter("@AcademicYear", SelectedAcademicYear)
                };

                DataTable dtCheck = DatabaseHelper.ExecuteQuery(checkLockQuery, lockParams);
                if (dtCheck.Rows.Count > 0)
                {
                    int totalClasses = Convert.ToInt32(dtCheck.Rows[0]["TotalClasses"]);
                    int lockedReports = Convert.ToInt32(dtCheck.Rows[0]["LockedReports"]);

                    if (totalClasses == 0)
                    {
                        StatusMessage = $"Không có lớp học nào được khởi tạo trong năm học {SelectedAcademicYear}.";
                        NotificationHelper.ShowWarning(StatusMessage);
                        return;
                    }

                    if (lockedReports < totalClasses)
                    {
                        StatusMessage = $"Chưa thể lập báo cáo toàn trường! Hiện mới có {lockedReports}/{totalClasses} lớp hoàn tất chốt sổ.";
                        NotificationHelper.ShowError(StatusMessage);
                        return;
                    }
                }

                string query = @"
                    DECLARE @PassingGrade DECIMAL(5,2) = ISNULL((SELECT Value FROM Parameter WHERE ParameterName = 'NumPassingGrade'), 5.0);

                    SELECT 
                        c.ClassName,
                        cr.TotalStudents,
                        SUM(CASE WHEN SubQ.IsPassed = 1 THEN 1 ELSE 0 END) AS PassedStudents
                    FROM Class c
                    JOIN ClassReport cr ON c.ClassID = cr.ClassID AND cr.Semester = @Semester AND cr.AcademicYear = @AcademicYear
                    LEFT JOIN (
                        SELECT 
                            cp.ClassID,
                            s.StudentID,
                            CASE WHEN MIN(sc.AverageScore) >= @PassingGrade 
                                 AND AVG(CASE WHEN sub.SubjectName <> N'Giáo dục thể chất' THEN sc.AverageScore ELSE NULL END) >= @PassingGrade 
                            THEN 1 ELSE 0 END AS IsPassed
                        FROM Student s
                        JOIN ClassPlacement cp ON s.StudentID = cp.StudentID
                        LEFT JOIN Score sc ON s.StudentID = sc.StudentID AND sc.Semester = @Semester AND sc.AcademicYear = @AcademicYear
                        LEFT JOIN Subject sub ON sc.SubjectID = sub.SubjectID
                        GROUP BY cp.ClassID, s.StudentID
                    ) SubQ ON c.ClassID = SubQ.ClassID
                    WHERE cr.IsLocked = 1
                    GROUP BY c.ClassName, cr.TotalStudents
                    ORDER BY c.ClassName";

                SqlParameter[] reportParams = {
                    new SqlParameter("@Semester", SelectedSemester),
                    new SqlParameter("@AcademicYear", SelectedAcademicYear)
                };

                DataTable dtReport = DatabaseHelper.ExecuteQuery(query, reportParams);

                int stt = 1;
                foreach (DataRow row in dtReport.Rows)
                {
                    int total = Convert.ToInt32(row["TotalStudents"]);
                    int passed = row["PassedStudents"] != DBNull.Value ? Convert.ToInt32(row["PassedStudents"]) : 0;
                    double rate = total > 0 ? ((double)passed / total) * 100 : 0;

                    ReportData.Add(new GlobalReportItem
                    {
                        STT = stt++,
                        ClassName = row["ClassName"].ToString(),
                        TotalStudents = total,
                        PassedStudents = passed,
                        PassRate = rate.ToString("0.0") + "%"
                    });
                }

                IsDataReady = true;
                StatusMessage = $"Báo cáo đã sẵn sàng. Toàn bộ {dtReport.Rows.Count} lớp đã chốt sổ.";
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi tải báo cáo: " + ex.Message);
            }
        }

        // Lấy dữ liệu chi tiết của 1 lớp khi click vào DataGrid
        private void LoadClassDetail(string className)
        {
            try
            {
                DetailClassName = className;
                DetailList.Clear();

                string query = @"
                    DECLARE @PassingGrade DECIMAL(5,2) = ISNULL((SELECT Value FROM Parameter WHERE ParameterName = 'NumPassingGrade'), 5.0);

                    SELECT 
                        s.StudentID,
                        s.FullName,
                        CASE 
                            WHEN MIN(sc.AverageScore) >= @PassingGrade 
                                 AND AVG(CASE WHEN sub.SubjectName <> N'Giáo dục thể chất' THEN sc.AverageScore ELSE NULL END) >= @PassingGrade 
                            THEN N'Đạt' ELSE N'Không đạt' 
                        END AS Status
                    FROM Student s
                    JOIN ClassPlacement cp ON s.StudentID = cp.StudentID
                    JOIN Class c ON cp.ClassID = c.ClassID
                    LEFT JOIN Score sc ON s.StudentID = sc.StudentID AND sc.Semester = @Semester AND sc.AcademicYear = @AcademicYear
                    LEFT JOIN Subject sub ON sc.SubjectID = sub.SubjectID
                    WHERE c.ClassName = @ClassName 
                      AND cp.AcademicYear = @AcademicYear 
                      AND cp.EffectiveTo IS NULL
                    GROUP BY s.StudentID, s.FullName
                    ORDER BY s.FullName";

                SqlParameter[] parameters = {
                    new SqlParameter("@ClassName", className),
                    new SqlParameter("@Semester", SelectedSemester),
                    new SqlParameter("@AcademicYear", SelectedAcademicYear)
                };

                DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);
                int stt = 1;
                foreach (DataRow r in dt.Rows)
                {
                    DetailList.Add(new StudentStatusItem
                    {
                        STT = stt++,
                        StudentId = r["StudentID"].ToString(),
                        FullName = r["FullName"].ToString(),
                        Status = r["Status"].ToString()
                    });
                }

                IsDetailOpen = true;
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi tải chi tiết lớp: " + ex.Message);
            }
        }

        [RelayCommand]
        private void ExportExcel()
        {
            if (!IsDataReady || ReportData.Count == 0) return;

            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    FileName = $"BaoCaoToanTruong_{SelectedSemester}_{SelectedAcademicYear}.xlsx",
                    Title = "Lưu báo cáo tổng kết"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Báo Cáo Tổng Kết");

                        // Tiêu đề lớn
                        worksheet.Cell(1, 1).Value = $"BÁO CÁO TỔNG KẾT TOÀN TRƯỜNG - {SelectedSemester.ToUpper()} - {SelectedAcademicYear}";
                        var titleRange = worksheet.Range("A1:E1");
                        titleRange.Merge().Style.Font.SetBold().Font.FontSize = 16;
                        titleRange.Style.Font.FontColor = XLColor.FromHtml("#1A237E");
                        titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // Tiêu đề cột
                        worksheet.Cell(3, 1).Value = "STT";
                        worksheet.Cell(3, 2).Value = "Lớp";
                        worksheet.Cell(3, 3).Value = "Sĩ số";
                        worksheet.Cell(3, 4).Value = "Số lượng Đạt";
                        worksheet.Cell(3, 5).Value = "Tỉ lệ Đạt";

                        var headerRange = worksheet.Range("A3:E3");
                        headerRange.Style.Font.SetBold().Font.FontSize = 12;
                        headerRange.Style.Font.FontColor = XLColor.White;
                        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1A237E");
                        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        // Đổ dữ liệu
                        int row = 4;
                        foreach (var item in ReportData)
                        {
                            worksheet.Cell(row, 1).Value = item.STT;
                            worksheet.Cell(row, 2).Value = item.ClassName;
                            worksheet.Cell(row, 3).Value = item.TotalStudents;
                            worksheet.Cell(row, 4).Value = item.PassedStudents;
                            worksheet.Cell(row, 5).Value = item.PassRate;

                            worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            worksheet.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            worksheet.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            worksheet.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            // Màu nền xen kẽ
                            if (row % 2 == 0)
                            {
                                worksheet.Range($"A{row}:E{row}").Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F6FA");
                            }

                            row++;
                        }

                        // Kẻ khung
                        var dataRange = worksheet.Range($"A3:E{row - 1}");
                        dataRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                        dataRange.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
                        dataRange.Style.Border.SetOutsideBorderColor(XLColor.FromHtml("#B2BEC3"));
                        dataRange.Style.Border.SetInsideBorderColor(XLColor.FromHtml("#DFE6E9"));

                        worksheet.Column(1).Width = 8;  // STT
                        worksheet.Column(2).Width = 25; // Lớp
                        worksheet.Column(3).Width = 15; // Sĩ số
                        worksheet.Column(4).Width = 20; // Số lượng Đạt
                        worksheet.Column(5).Width = 18; // Tỉ lệ Đạt
                        worksheet.Rows().AdjustToContents(); // Tự giãn chiều cao nếu cần

                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    NotificationHelper.ShowSuccess("Xuất file Excel thành công!");
                }
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Lỗi xuất Excel: Vui lòng đóng file nếu đang mở. Chi tiết: " + ex.Message);
            }
        }
    }
}