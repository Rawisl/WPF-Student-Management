using System;

namespace WPF_Student_Management.Helpers
{
    public static class ScoreHelper
    {
        public static string GetDisplayScore(string subjectName, double score)
        {
            if (string.IsNullOrWhiteSpace(subjectName))
                return score.ToString("0.##");

            string lowerSubject = subjectName.Trim().ToLower();

            // Áp dụng cho cả GDTC và GDCD (các môn đánh giá bằng chữ)
            if (lowerSubject.Contains("thể chất") || lowerSubject.Contains("công dân"))
            {
                // Vì database đã encode: 10 là Đạt, 0 là Không Đạt
                return score == 10 ? "Đạt" : "Không đạt";
            }

            return score.ToString("0.##");
        }
    }
}