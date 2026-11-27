using Core.Model.Exceptions;
using MathNet.Numerics.LinearAlgebra;

namespace Core.Model
{
    public class PointsNormalizer
    {
        public double Max { get; init; }

        public double Min { get; init; }

        public double Tolerance { get; init; }

        public double Center { get; init; }

        public double Half { get; init; }

        public double InvHalf { get; init; }

        public PointsNormalizer(double min, double max, double tolerance = 0)
        {
            if (!(max > min))
                throw new ArgumentException($"max ({max}) должен быть больше min ({min})");

            Max = max;
            Min = min;
            Tolerance = tolerance;
            Center = (min + max) / 2;
            Half = (max - min) / 2;
            InvHalf = 2 / (max - min);
        }

        public double[] Normalize(double[] values)
        {
            ValidateRawPoints(values);
            var vec = Vector<double>.Build.Dense(values);

            var normalized = (vec - Center) * InvHalf;

            return normalized.AsArray(); 
        }

        public double Normalize(double value)
        {
            ValidateRawPoints([value]);
            return (value - Center) * InvHalf;
        }

        public bool TryNormalize(double[] values, out double[] normalized)
        {
            try
            {
                ValidateRawPoints(values);
                var vec = Vector<double>.Build.Dense(values);

                var normalizedVector = (vec - Center) * InvHalf;

                normalized = normalizedVector.AsArray();

                return true;
            }
            catch
            {
                normalized = [];
                return false;
            }            
        }

        public bool TryNormalize(double value, out double normalized)
        {
            try
            {
                ValidateRawPoints([value]);
                normalized = (value - Center) * InvHalf;
                return true;
            }
            catch
            {
                normalized = 0;
                return false;
            }
            
        }

        private void ValidateRawPoints(double[] values)
        {
            double length = Max - Min;

            foreach (var value in values)
            {
                if (value > Max + Tolerance * length || value < Min - Tolerance * length)
                    throw new DataErrorException(
                        $"значение точки {value} выходит за диапазон "
                        + $"{Min} - {Max} с запасом {Tolerance:p0}, " +
                        $"уберите точку, либо поменяйте диапазон");
            }
        }
    }
}
