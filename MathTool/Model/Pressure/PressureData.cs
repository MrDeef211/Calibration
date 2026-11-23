namespace Core.Model.Pressure
{
    public class PressureData
    {
        public int P_Pow { get; init; }
        public int T_Pow { get; init; }
        public int[] CalibrationIndexes { get; init; }
        public double[] P_Adc { get; init; }
        public double[] T_Adc { get; init; }
        public double[] P_Values { get; init; }
        public double P_AdcMin { get; init; }
        public double P_AdcMax { get; init; }
        public double T_AdcMin { get; init; }
        public double T_AdcMax { get; init; }
        public double P_Min { get; init; }
        public double P_Max { get; init; }
        public double T_Min { get; init; }
        public double T_Max { get; init; }

        public PressureData(
            int p_pow,
            int t_pow,
            int[] calibrationIndexes,
            double[] p_adc,
            double[] t_adc,
            double[] refer,
            double p_adcMin,
            double p_adcMax,
            double t_adcMin,
            double t_adcMax,
            double p_min,
            double p_max,
            double t_min,
            double t_max)
        {
            P_Pow = p_pow;
            T_Pow = t_pow;
            CalibrationIndexes = calibrationIndexes;
            P_Adc = p_adc;
            T_Adc = t_adc;
            P_Values = refer;
            P_AdcMin = p_adcMin;
            P_AdcMax = p_adcMax;
            T_AdcMin = t_adcMin;
            T_AdcMax = t_adcMax;
            P_Min = p_min;
            P_Max = p_max;
            T_Min = t_min;
            T_Max = t_max;
        }

        public PressureData()
        {
            P_Pow = 0;
            T_Pow = 0;
            CalibrationIndexes = [];
            P_Adc = [];
            T_Adc = [];
            P_Values = [];
            P_AdcMin = 0;
            P_AdcMax = 0;
            T_AdcMin = 0;
            T_AdcMax = 0;
            P_Min = 0;
            P_Max = 0;
            T_Min = 0;
            T_Max = 0;
        }
    }
}
