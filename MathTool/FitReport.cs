using static Calibration.MathTool;

namespace Calibration
{
    public sealed class FitReport
    {
        public double ConditionNumber { get; init; }
        public int Rank { get; init; }
        public int Points { get; init; }
        public int Coefficients { get; init; }
        public double StandardDeviation { get; init; }

        public FitReport(
            double[] p_ads,
            double[] t_ads,
            double[] p_ref,
            double[] dp,
            double[,] coeffs)
        {
            int K = p_ref.Length;
            var H = BuildDesignMatrix(coeffs.GetLength(0) - 1, coeffs.GetLength(1) - 1, p_ads, t_ads);

            var svd = H.Svd(computeVectors: false);
            double cond = svd.S[0] / svd.S[^1];
            int rank = svd.S.Count(s => s > 1e-10 * svd.S[0]);

            ConditionNumber = cond;
            Rank = rank;
            Points = K;
            Coefficients = coeffs.GetLength(0) * coeffs.GetLength(1);
            StandardDeviation = GetStandardDeviation(dp);
        }
    }
}
