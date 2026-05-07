using System;

namespace WPF_Student_Management.Helpers
{
    public static class ScoreHelper
    {
        private const double PASSING_SCORE = 5.0;

        public static string GetDisplayScore(string subjectName, double score)
        {
            if (string.IsNullOrWhiteSpace(subjectName))
                return score.ToString("0.##");

            string lowerSubject = subjectName.Trim().ToLower();

            if (lowerSubject.Contains("giáo dục thể chất"))
            {
                return score >= PASSING_SCORE ? "Đạt" : "Không đạt";
            }

            return score.ToString("0.##");
        }
    }
}