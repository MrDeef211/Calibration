using Core.Model.Pressure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Model.Reports
{
    public static class PressureValidator
    {
        public static bool CheckCalibrationError(PressureData data, PressureCalibrationResult result, out string errors)
        {
            var errorsList = new List<string>();
            if (result.FitReport.CalibrationMaxError > data.LimitMaxError)
                errorsList.Add("Максимальная погрешность больше допустимой");
            if (result.FitReport.CalibrationStandardError > data.LimitStandardError)
                errorsList.Add("Среднеквадратичная погрешность больше допустимой");
            if (result.FitReport.CalibrationMaxRelativeError * 100 > data.LimitMaxRelativeError)
                errorsList.Add("Максимальная приведённая погрешность больше допустимой");
            if (result.FitReport.CalibrationRelativeStandardError * 100 > data.LimitStandardRelativeError)
                errorsList.Add("Среднеквадратичная приведённая погрешность больше допустимой");

            if (errorsList.Count == 0)
                errors = "Ошибок нет";
            else 
                errors = string.Join(", ", errorsList);
            return errorsList.Count == 0;
        }

        public static bool CheckTestError(PressureData data, PressureCalibrationResult result, out string errors)
        {
            var errorsList = new List<string>();
            if (result.FitReport.TestMaxError > data.LimitMaxError)
                errorsList.Add("Максимальная погрешность больше допустимой");
            if (result.FitReport.TestStandardError > data.LimitStandardError)
                errorsList.Add("Среднеквадратичная погрешность больше допустимой");
            if (result.FitReport.TestMaxRelativeError * 100 > data.LimitMaxRelativeError)
                errorsList.Add("Максимальная приведённая погрешность больше допустимой");
            if (result.FitReport.TestRelativeStandardError * 100 > data.LimitStandardRelativeError)
                errorsList.Add("Среднеквадратичная приведённая погрешность больше допустимой");

            if (errorsList.Count == 0)
                errors = "Ошибок нет";
            else
                errors = string.Join(", ", errorsList);
            return errorsList.Count == 0;
        }


        public static bool CheckMathError(PressureData data, PressureCalibrationResult result, out string errors)
        {
            var errorsList = new List<string>();
            if (result.MonotonicityReport.Verdict == MonotonicityVerdict.WeakMargin)
                errorsList.Add("Малый запас по монотонности");
            if (result.MonotonicityReport.Verdict == MonotonicityVerdict.Violated)
                errorsList.Add("Нарушение монотонности");
            if (result.FitReport.Rank < result.Coefficients.Length)
                errorsList.Add($"rank(H)={result.FitReport.Rank} < {result.Coefficients.Length}: матрица вырождена");
            if (result.FitReport.ConditionNumber > 100000)
                errorsList.Add($"Число обусловленности больше 10^5, решение нестабильное");

            if (errorsList.Count == 0)
                errors = "Ошибок нет";
            else
                errors = string.Join(", ", errorsList);
            return errorsList.Count == 0;
        }
    }


    public static class TemperatureValidator
    {

    }
}
