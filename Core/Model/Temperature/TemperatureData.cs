namespace Core.Model.Temperature
{
    public class TemperatureData
    {
        public int T_Pow { get; init; }
        public int[] CalibrationIndexes { get; init; }
        public double[] T_Adc { get; init; }
        public double[] T_Values { get; init; }
        public double T_AdcMin { get; init; }
        public double T_AdcMax { get; init; }
        public double T_Min { get; init; }
        public double T_Max { get; init; }

        public TemperatureData(
            int t_pow,
            int[] calibrationIndexes,
            double[] t_adc,
            double[] refer,
            double t_adcMin,
            double t_adcMax,
            double t_min,
            double t_max)
        {
            T_Pow = t_pow;
            CalibrationIndexes = calibrationIndexes;
            T_Adc = t_adc;
            T_Values = refer;
            T_AdcMin = t_adcMin;
            T_AdcMax = t_adcMax;
            T_Min = t_min;
            T_Max = t_max;
        }
        public TemperatureData()
        {
            T_Pow = 0;
            CalibrationIndexes = [];
            T_Adc = [];
            T_Values = [];
            T_AdcMin = 0;
            T_AdcMax = 0;
            T_Min = 0;
            T_Max = 0;
        }
    }
}
