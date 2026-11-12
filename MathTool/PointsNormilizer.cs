using MathNet.Numerics.Providers.LinearAlgebra;

namespace Calibration
{
    public class PointsNormilizer
    {
        public double Max { get; init; }

        public double Min { get; init; }

        public double Tolerance { get; init; }

        public double Center { get; init; }

        public double Half { get; init; }

        public double InvHalf { get; init; }



        public PointsNormilizer(double min, double max, double tolerance = 0)
        {
            Max = max;
            Min = min;
            Tolerance = tolerance;
            Center = (min + max) / 2;
            Half = (max - min) / 2;
            InvHalf = 2 / (max - min);
        }

        public double[] Normilize(double[] values)
        {
            ValidateRawPoints(values);
            double[] result = new double[values.Length];

            // TODO поискать вариант получше
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = values[i] - Center;
            }

            LinearAlgebraControl.Provider.ScaleArray(InvHalf, result, result);

            return result;
        }

        public double Normilize(double value)
        {
            ValidateRawPoints([value]);
            return (value - Center) *  InvHalf;
        }

        private void ValidateRawPoints(double[] values)
        {
            double length = Max - Min;

            foreach (var value in values)
            {
                if (value > Max + Tolerance * length || value < Min - Tolerance * length)
                    throw new ArgumentException(
                        $"значение точки {value} выходит за диапазон "
                        + $"{Min} - {Max} с запасом {Tolerance:p0}%");
            }
        }
    }
}
