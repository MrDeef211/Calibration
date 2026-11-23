using static MathTools.MathTool;

namespace Core.Model.Reports
{
    public sealed class FitReport
    {
        public double ConditionNumber { get; init; }
        public int Rank { get; init; }
        public int Points { get; init; }
        public int Coefficients { get; init; }
        public double StandardDeviation { get; init; }
        public double RelativeStandardDeviation { get; init; }
        public double MaxDeviation { get; init; }
        public double MaxRelativeDeviation { get; init; }

        public FitReport(
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

            ConditionNumber = cond;
            Rank = rank;
            Points = calibration_p_adc.Length;
            Coefficients = coeffs.GetLength(0) * coeffs.GetLength(1);
            StandardDeviation = GetStandardDeviation(dp);
            RelativeStandardDeviation = GetStandardDeviation(ep);
            MaxDeviation = dp.Max(d => double.IsFinite(d) ? Math.Abs(d) : 0);
            MaxRelativeDeviation = ep.Max(d => double.IsFinite(d) ? Math.Abs(d) : 0);

        }
    }
}
