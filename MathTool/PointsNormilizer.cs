using MathNet.Numerics.Providers.LinearAlgebra;

namespace Calibration
{
    public class PointsNormilizer
    {
        public double Max { get; init; }

        public double Min { get; init; }

        public double Tolerance { get; init; }

        public PointsNormilizer(double min, double max, double tolerance = 0)
        {
            Max = max;
            Min = min;
            Tolerance = tolerance;
        }

        public double[] Normilize(double[] values)
        {
            ValidateRawPoints(values);
            double center = (Min + Max) / 2;
            double invHalf = 2 / (Max - Min);
            double[] result = new double[values.Length];

            // TODO поискать вариант получше
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = values[i] - center;
            }

            LinearAlgebraControl.Provider.ScaleArray(invHalf, result, result);

            return result;
        }

        public double Normilize(double value)
        {
            ValidateRawPoints([value]);
            double center = (Min + Max) / 2;
            double half = (Max - Min) / 2;
            return (value - center) / half;
        }

        private void ValidateRawPoints(double[] values)
        {
            double length = Max - Min;

            foreach (var value in values)
            {
                if (value > Max + Tolerance * length || value < Min - Tolerance * length)
                    throw new ArgumentException(
                        $"значение точки {value} выходит за диапазон "
                        + $"{Min} - {Max} с запасом {Tolerance * 100}%");
            }
        }
    }
}
