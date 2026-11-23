using Core.Model.Reports;

namespace Core.Model.Pressure
{
    public class PressureCalibrationResult
    {
        public double[,] Coefficients { get; init; }
        /// <summary>
        /// Вычисленные значения
        /// </summary>
        public double[] P { get; init; }
        /// <summary>
        /// Абсолютная погрешность в каждой точке
        /// </summary>
        public double[] DP { get; init; }
        /// <summary>
        /// Приведённая погрешность в каждой точке
        /// </summary>
        public double[] EP { get; init; }
        public PointsNormilizer PNormilizer { get; init; }
        public PointsNormilizer TNormilizer { get; init; }
        public FitReport FitReport { get; init; }
        public PressureMonotonicityReport MonotonicityReport { get; init; }

        public PressureCalibrationResult(
            double[,] coefficients,
            double[] p,
            double[] dP,
            double[] eP,
            PointsNormilizer pNormilizer,
            PointsNormilizer tNormilizer,
            FitReport fitReport,
            PressureMonotonicityReport monotonicityReport)
        {
            Coefficients = coefficients;
            P = p;
            DP = dP;
            EP = eP;
            PNormilizer = pNormilizer;
            TNormilizer = tNormilizer;
            FitReport = fitReport;
            MonotonicityReport = monotonicityReport;
        }
    }
}
