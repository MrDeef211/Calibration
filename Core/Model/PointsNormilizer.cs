using Core.Model.Exceptions;
using MathNet.Numerics.LinearAlgebra;

namespace Core.Model
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
            if (!(max > min))
                throw new ArgumentException($"max ({max}) должен быть больше min ({min})");

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
            var vec = Vector<double>.Build.Dense(values);

            var normalized = (vec - Center) * InvHalf;

            return normalized.AsArray(); 
        }

        public double Normilize(double value)
        {
            ValidateRawPoints([value]);
            return (value - Center) * InvHalf;
        }

        public bool TryNormilize(double[] values, out double[] normilized)
        {
            try
            {
                ValidateRawPoints(values);
                var vec = Vector<double>.Build.Dense(values);

                var normalized = (vec - Center) * InvHalf;

                normilized = normalized.AsArray();

                return true;
            }
            catch
            {
                normilized = [];
                return false;
            }            
        }

        public bool TryNormilize(double value, out double normilized)
        {
            try
            {
                ValidateRawPoints([value]);
                normilized = (value - Center) * InvHalf;
                return true;
            }
            catch
            {
                normilized = 0;
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
