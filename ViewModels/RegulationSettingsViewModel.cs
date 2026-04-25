using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_Student_Management.Helpers;

namespace WPF_Student_Management.ViewModels
{
    public partial class RegulationSettingsViewModel : ObservableObject
    {
        private bool _isLoaded = false;

        [ObservableProperty]
        private bool _hasUnsavedChanges = false;

        // CÁC BIẾN NULLABLE ĐỂ CHO PHÉP XÓA RỖNG
        [ObservableProperty] private int? _minAge;
        [ObservableProperty] private int? _maxAge;
        [ObservableProperty] private int? _maxClassSize;
        [ObservableProperty] private double? _numPassingGrade;
        [ObservableProperty] private double? _regularScoreCoefficient;
        [ObservableProperty] private double? _midtermScoreCoefficient;
        [ObservableProperty] private double? _finalScoreCoefficient;

        ObservableCollection<Models.Regulation> regulations;
        //String hiển thị công thức tính điểm trung bình theo hệ số
        // Tử số
        public string FormulaNumerator =>
            $"(TX x {RegularScoreCoefficient ?? 0}) + (GK x {MidtermScoreCoefficient ?? 0}) + (CK x {FinalScoreCoefficient ?? 0})";

        // Mẫu số
        public string FormulaDenominator =>
            $"{RegularScoreCoefficient ?? 0} + {MidtermScoreCoefficient ?? 0} + {FinalScoreCoefficient ?? 0}";
        public RegulationSettingsViewModel()
        {
            LoadDataFromDatabase();
        }
        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // Danh sách các trường cần theo dõi "biến"
            string[] monitoredProps = {
            nameof(MinAge), nameof(MaxAge), nameof(MaxClassSize),
            nameof(NumPassingGrade), nameof(RegularScoreCoefficient),
            nameof(MidtermScoreCoefficient), nameof(FinalScoreCoefficient)
        };

            if (_isLoaded && monitoredProps.Contains(e.PropertyName))
            {
                HasUnsavedChanges = true;

                // Khi bất kỳ hệ số nào đổi, vẽ lại công thức
                if (e.PropertyName.Contains("Coefficient"))
                {
                    OnPropertyChanged(nameof(FormulaNumerator));
                    OnPropertyChanged(nameof(FormulaDenominator));
                }
            }
        }
        public void LoadDataFromDatabase()
        {
            _isLoaded = false;

            try
            {
                var regs = Models.Regulation.GetAllRegulations();
                MinAge = (int?)(regs.FirstOrDefault(r => r.RegulationId == 1)?.Value);
                MaxAge = (int?)(regs.FirstOrDefault(r => r.RegulationId == 2)?.Value);
                MaxClassSize = (int?)(regs.FirstOrDefault(r => r.RegulationId == 3)?.Value);
                NumPassingGrade = (double?)(regs.FirstOrDefault(r => r.RegulationId == 4)?.Value);
                RegularScoreCoefficient = (double?)(regs.FirstOrDefault(r => r.RegulationId == 5)?.Value);
                MidtermScoreCoefficient = (double?)(regs.FirstOrDefault(r => r.RegulationId == 6)?.Value);
                FinalScoreCoefficient = (double?)(regs.FirstOrDefault(r => r.RegulationId == 7)?.Value);

                _isLoaded = true;
                HasUnsavedChanges = false;
            }
            catch (Exception ex)
            {
                NotificationHelper.ShowError("Không thể tải quy định: " + ex.Message);
            }
        }

        //--- LOGIC TĂNG/GIẢM  ---
        [RelayCommand]
        private void IncreaseMinAge()
        {
            MinAge ??= 0; MaxAge ??= 0;
            if (MinAge < MaxAge && MinAge < 100) MinAge++;
        }

        [RelayCommand]
        private void DecreaseMinAge()
        {
            MinAge ??= 0;
            if (MinAge > 13) MinAge--;
        }

        [RelayCommand]
        private void IncreaseMaxAge()
        {
            MaxAge ??= 0;
            if (MaxAge < 18) MaxAge++;
        }

        [RelayCommand]
        private void DecreaseMaxAge()
        {
            MaxAge ??= 0; MinAge ??= 0;
            if (MaxAge > MinAge && MaxAge > 0) MaxAge--;
        }

        [RelayCommand]
        private void IncreaseClassSize()
        {
            MaxClassSize ??= 0;
            MaxClassSize++;
        }

        [RelayCommand]
        private void ReduceClassSize()
        {
            MaxClassSize ??= 0;
            if (MaxClassSize > 0) MaxClassSize--;
        }

        [RelayCommand]
        private void IncreaseNumPassingGrade()
        {
            NumPassingGrade ??= 0.0;
            if (NumPassingGrade < 10.0) NumPassingGrade = System.Math.Round((double)NumPassingGrade.Value + 0.1, 1);
        }

        [RelayCommand]
        private void DecreaseNumPassingGrade()
        {
            NumPassingGrade ??= 0.0;
            if (NumPassingGrade > 0.0) NumPassingGrade = System.Math.Round((double)NumPassingGrade.Value - 0.1, 1);
        }

        [RelayCommand]
        private void IncreaseRegularCoefficient()
        {
            RegularScoreCoefficient ??= 0;
            if (RegularScoreCoefficient < 5.0)
                RegularScoreCoefficient = System.Math.Round((double)RegularScoreCoefficient.Value + 1, 1);
        }
        [RelayCommand]
        private void DecreaseRegularCoefficient()
        {
            RegularScoreCoefficient ??= 0;
            if (RegularScoreCoefficient > 0.0) RegularScoreCoefficient = System.Math.Round((double)RegularScoreCoefficient.Value - 1, 1);
        }
        [RelayCommand]
        private void IncreaseMidtermScoreCoefficient()
        {
            MidtermScoreCoefficient ??= 0;
            if (MidtermScoreCoefficient < 5.0)
                MidtermScoreCoefficient = System.Math.Round((double)MidtermScoreCoefficient.Value + 1, 1);
        }
        [RelayCommand]
        private void DecreaseMidtermScoreCoefficient()
        {
            MidtermScoreCoefficient ??= 0;
            if (MidtermScoreCoefficient > 0.0) MidtermScoreCoefficient = System.Math.Round((double)MidtermScoreCoefficient.Value - 1, 1);
        }
        [RelayCommand]
        private void IncreaseFinalScoreCoefficient()
        {
            FinalScoreCoefficient ??= 0;
            if (FinalScoreCoefficient < 5.0)
                FinalScoreCoefficient = System.Math.Round((double)FinalScoreCoefficient.Value + 1, 1);
        }
        [RelayCommand]
        private void DecreaseFinalScoreCoefficient()
        {
            FinalScoreCoefficient ??= 0;
            if (FinalScoreCoefficient > 0.0) FinalScoreCoefficient = System.Math.Round((double)FinalScoreCoefficient.Value - 1, 1);
        }

        // --- LƯU DỮ LIỆU ---
        [RelayCommand]
        private void SaveSettings()
        {
            if (MinAge == null || MaxAge == null || MaxClassSize == null || NumPassingGrade == null || RegularScoreCoefficient == null || MidtermScoreCoefficient == null || FinalScoreCoefficient == null)
            {
                NotificationHelper.ShowWarning("Vui lòng nhập đầy đủ các quy định, không được để trống ô nào!");
                return;
            }
            if (MinAge < 13 || MinAge > MaxAge)
            {
                NotificationHelper.ShowWarning("Tuổi tối thiểu phải >= 13 và KHÔNG ĐƯỢC LỚN HƠN Tuổi tối đa!");
                return;
            }
            if (MaxAge > 18)
            {
                NotificationHelper.ShowWarning("Tuổi tối đa không được vượt quá 18!");
                return;
            }
            if (MaxClassSize < 0)
            {
                NotificationHelper.ShowWarning("Sĩ số tối đa không được nhỏ hơn 0!");
                return;
            }
            if (NumPassingGrade < 0.0 || NumPassingGrade > 10.0)
            {
                NotificationHelper.ShowWarning("Điểm đạt môn phải nằm trong khoảng từ 0.0 đến 10.0!");
                return;
            }
            if (RegularScoreCoefficient < 0.0 || RegularScoreCoefficient > 5.0)
            {
                NotificationHelper.ShowWarning("Hệ số điểm thường xuyên phải nằm trong khoảng từ 0.0 đến 5.0!");
                return;
            }
            if (MidtermScoreCoefficient < 0.0 || MidtermScoreCoefficient > 5.0)
            {
                NotificationHelper.ShowWarning("Hệ số điểm kiểm tra giữa kỳ phải nằm trong khoảng từ 0.0 đến 5.0!");
                return;
            }
            if (FinalScoreCoefficient < 0.0 || FinalScoreCoefficient > 5.0)
            {
                NotificationHelper.ShowWarning("Hệ số điểm kiểm tra cuối kỳ phải nằm trong khoảng từ 0.0 đến 5.0!");
                return;
            }

            double totalCoefficient = (RegularScoreCoefficient ?? 0) +
                                  (MidtermScoreCoefficient ?? 0) +
                                  (FinalScoreCoefficient ?? 0);
            if (totalCoefficient == 0)
            {
                NotificationHelper.ShowWarning("Tổng các hệ số điểm không được bằng 0!");
                return;
            }
            try
            {

                var updateList = new List<Models.Regulation>
                {
                    new Models.Regulation { RegulationId = 1, RegulationName = "MinAge", Value = (decimal)MinAge },
                    new Models.Regulation { RegulationId = 2, RegulationName = "MaxAge", Value = (decimal)MaxAge },
                    new Models.Regulation { RegulationId = 3, RegulationName = "MaxClassSize", Value = (decimal)MaxClassSize },
                    new Models.Regulation { RegulationId = 4, RegulationName = "NumPassingGrade", Value = (decimal)NumPassingGrade },
                    new Models.Regulation { RegulationId = 5, RegulationName = "RegularScoreCoefficient", Value = (decimal)RegularScoreCoefficient },
                    new Models.Regulation { RegulationId = 6, RegulationName = "MidtermScoreCoefficient", Value = (decimal)MidtermScoreCoefficient },
                    new Models.Regulation { RegulationId = 7, RegulationName = "FinalScoreCoefficient", Value = (decimal)FinalScoreCoefficient }
                };

                bool allSuccess = true;

                foreach (var reg in updateList)
                {
                    if (!reg.UpdateRegulation())
                        allSuccess = false;
                }

                if (allSuccess)
                {
                    HasUnsavedChanges = false;
                    NotificationHelper.ShowSuccess("Cập nhật quy định hệ thống thành công!");
                }
            }
            catch (System.Exception ex)
            {
                NotificationHelper.ShowError("Có lỗi xảy ra khi lưu: " + ex.Message);
            }
        }
    }
}
