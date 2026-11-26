namespace Core.Model.Pressure
{
    public class PressureData
    {
        public bool Normilize { get; set; }
        public int P_Pow { get; init; }
        public int T_Pow { get; init; }
        public int[] CalibrationIndexes { get; init; }
        public string Unit_p { get; init; }
        public string Unit_t { get; init; }
        public double[] P_Adc { get; init; }
        public double[] T_Adc { get; init; }
        public double[] P_Values { get; init; }
        public double[] Range_p { get; init; }
        public double[] Range_t { get; init; }
        public double[] Adc_Range_p { get; init; }
        public double[] Adc_Range_t { get; init; }
        public double LimitMaxError { get; init; }
        public double LimitMaxRelativeError { get; init; }
        public double LimitStandardError { get; init; }
        public double LimitStandardRelativeError { get; init; }

        public PressureData(
            bool normilize,
            int p_pow,
            int t_pow,
            int[] calibrationIndexes,
            string unit_p,
            string unit_t,
            double[] p_adc,
            double[] t_adc,
            double[] refer,
            double[] adc_range_p,
            double[] adc_range_t,
            double[] range_p,
            double[] range_t,
            double limitMaxError,
            double limitMaxRelativeError,
            double limitStandardError,
            double limitStandardRelativeError)
        {
            Normilize = normilize;
            P_Pow = p_pow;
            T_Pow = t_pow;
            CalibrationIndexes = calibrationIndexes;
            Unit_p = unit_p;
            Unit_t = unit_t;
            P_Adc = p_adc;
            T_Adc = t_adc;
            P_Values = refer;
            Adc_Range_p = adc_range_p;
            Adc_Range_t = adc_range_t;
            Range_p = range_p;
            Range_t = range_t;
            LimitMaxError = limitMaxError;
            LimitMaxRelativeError = limitMaxRelativeError;
            LimitStandardError = limitStandardError;
            LimitStandardRelativeError = limitStandardRelativeError;
        }
    }
}
