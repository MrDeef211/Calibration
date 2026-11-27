using Core.Model.Reports;

namespace Core.Model.Temperature
{
    public class TemperatureCalibrationResult
    {
        public double[] Coefficients { get; init; }
        /// <summary>
        /// Вычисленные значения
        /// </summary>
        public double[] T { get; init; }
        /// <summary>
        /// Абсолютная погрешность в каждой точке
        /// </summary>
        public double[] DT { get; init; }
        /// <summary>
        /// Приведённая погрешность в каждой точке
        /// </summary>
        public double[] ET { get; init; }
        /// <summary>
        /// Приведённая погрешность
        /// </summary>
        public double StandardDeviation { get; init; }
        /// <summary>
        /// Среднеквадратичная приведённая погрешность
        /// </summary>
        public double NormilizedStandardDeviation { get; init; }
        public PointsNormalizer Normilizer { get; init; }
        public TemperatureMonotonicityReport MonotonicityReport { get; init; }

        public TemperatureCalibrationResult(
            double[] coefficients,
            double[] t,
            double[] dT,
            double[] eT,
            double standardDeviation,
            double normilizedStandardDeviation,
            PointsNormalizer normilizer,
            TemperatureMonotonicityReport monotonicityReport)
        {
            Coefficients = coefficients;
            T = t;
            DT = dT;
            ET = eT;
            StandardDeviation = standardDeviation;
            NormilizedStandardDeviation = normilizedStandardDeviation;
            Normilizer = normilizer;
            MonotonicityReport = monotonicityReport;
        }
    }
}
