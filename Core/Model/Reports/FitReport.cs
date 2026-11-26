using static Core.MathTool;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Core.Model.Reports
{
    public sealed class FitReport
    {
        public double ConditionNumber { get; init; }
        public int Rank { get; init; }
        public int CalibrationPoints { get; init; }
        public int Coefficients { get; init; }
        public double TestStandardError { get; init; }
        public double TestRelativeStandardError { get; init; }
        public double TestMaxError { get; init; }
        public double TestMaxRelativeError { get; init; }
        public double CalibrationStandardError { get; init; }
        public double CalibrationRelativeStandardError { get; init; }
        public double CalibrationMaxError { get; init; }
        public double CalibrationMaxRelativeError { get; init; }

        public FitReport(
            int[] calibration_indexes,
            double[] calibration_p_adc,
            double[] calibration_t_adc,
            double[] dp,
            double[] ep,
            double[,] coeffs)
        {
            var H = BuildDesignMatrix(coeffs.GetLength(0) - 1, coeffs.GetLength(1) - 1, calibration_p_adc, calibration_t_adc);

            var svd = H.Svd(computeVectors: false);
            double cond = svd.S[0] / svd.S[^1];
            int rank = svd.S.Count(s => s > 1e-10 * svd.S[0]);

            double[] cal_dp = new double[calibration_indexes.Length];
            double[] cal_ep = new double[calibration_indexes.Length];

            for (int i = 0; i < calibration_indexes.Length; i++)
            {
                cal_dp[i] = dp[calibration_indexes[i]];
                cal_ep[i] = ep[calibration_indexes[i]];
            }

            ConditionNumber = cond;
            Rank = rank;
            CalibrationPoints = calibration_p_adc.Length;
            Coefficients = coeffs.GetLength(0) * coeffs.GetLength(1);
            TestStandardError = GetStandardDeviation(dp);
            TestRelativeStandardError = GetStandardDeviation(ep);
            TestMaxError = dp.Max(d => double.IsFinite(d) ? Math.Abs(d) : 0);
            TestMaxRelativeError = ep.Max(d => double.IsFinite(d) ? Math.Abs(d) : 0);
            CalibrationStandardError = GetStandardDeviation(cal_dp);
            CalibrationRelativeStandardError = GetStandardDeviation(cal_ep);
            CalibrationMaxError = cal_dp.Max(d => double.IsFinite(d) ? Math.Abs(d) : 0);
            CalibrationMaxRelativeError = cal_ep.Max(d => double.IsFinite(d) ? Math.Abs(d) : 0);
        }
    }
}
