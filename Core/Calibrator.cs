using Core.Model;
using Core.Model.Pressure;
using Core.Model.Reports;
using Core.Model.Temperature;
using static MathTools.MathTool;

namespace Core;

public static class Calibrator
{
    public static TemperatureCalibrationResult CalibrateTemperature(TemperatureData temperatureData)
    {
        int pow;
        int[] t_calibrationIndexes;
        double[] t_adc, t_ref;

        pow = temperatureData.T_Pow;
        t_calibrationIndexes = temperatureData.CalibrationIndexes;
        t_adc = temperatureData.T_Adc;
        t_ref = temperatureData.T_Values;


        double[] calibrationt_adc = new double[t_calibrationIndexes.Length];
        double[] calibrationt_ref = new double[t_calibrationIndexes.Length];

        for (int i = 0; i < t_calibrationIndexes.Length; i++)
        {
            calibrationt_adc[i] = t_adc[t_calibrationIndexes[i]];
            calibrationt_ref[i] = t_ref[t_calibrationIndexes[i]];
        }

        double[] t;
        double[] dt;
        double[] et;

        double min = temperatureData.T_AdcMin;
        double max = temperatureData.T_AdcMax;

        double[] coefficients;

        PointsNormilizer normilizer;

        normilizer = new PointsNormilizer(min, max, 0.1);

        coefficients = Polyfit1(
            pow,
            normilizer.Normilize(calibrationt_adc),
            calibrationt_ref
        );

        t = Calculate(
            coefficients,
            normilizer.Normilize(t_adc)
        );

        dt = Difference(t, t_ref);
        et = Quotient(dt, temperatureData.T_Max - temperatureData.T_Min);

        TemperatureMonotonicityReport report = new(coefficients);

        return new TemperatureCalibrationResult(
            coefficients,
            t,
            dt,
            et,
            GetStandardDeviation(dt),
            GetStandardDeviation(et),
            normilizer,
            report
        );
    }

    public static PressureCalibrationResult CalibratePressure(PressureData pressureData)
    {
        double[] calibrationt_p_adc = new double[pressureData.CalibrationIndexes.Length];
        double[] calibrationt_t_adc = new double[pressureData.CalibrationIndexes.Length];
        double[] calibrationt_ref = new double[pressureData.CalibrationIndexes.Length];

        for (int i = 0; i < pressureData.CalibrationIndexes.Length; i++)
        {
            calibrationt_p_adc[i] = pressureData.P_Adc[pressureData.CalibrationIndexes[i]];
            calibrationt_t_adc[i] = pressureData.T_Adc[pressureData.CalibrationIndexes[i]];
            calibrationt_ref[i] = pressureData.P_Values[pressureData.CalibrationIndexes[i]];
        }

        double[] p;
        double[] dp;

        double p_adcMin = pressureData.P_AdcMin;
        double p_adcMax = pressureData.P_AdcMax;

        double t_adcMin = pressureData.T_AdcMin;
        double t_adcMax = pressureData.T_AdcMax;

        double[,] coefficients;

        PointsNormilizer p_normilizer;
        PointsNormilizer t_normilizer;


        p_normilizer = new(p_adcMin, p_adcMax, 0);
        t_normilizer = new(t_adcMin, t_adcMax, 0);

        coefficients = Polyfit2(
        pressureData.P_Pow,
        pressureData.T_Pow,
        p_normilizer.Normilize(calibrationt_p_adc),
        t_normilizer.Normilize(calibrationt_t_adc),
        calibrationt_ref);

        p = Calculate(
            coefficients,
            p_normilizer.Normilize(pressureData.P_Adc),
            t_normilizer.Normilize(pressureData.T_Adc));

        dp = Difference(p, pressureData.P_Values);

        double[] ep = Quotient(dp, pressureData.P_Max - pressureData.P_Min);

        PressureMonotonicityReport monotonicityReport = new(coefficients);

        FitReport fitReport = new(
            p_normilizer.Normilize(calibrationt_p_adc),
            t_normilizer.Normilize(calibrationt_t_adc),
            dp,
            ep,
            coefficients
        );

        return new PressureCalibrationResult(coefficients, p, dp, ep, p_normilizer, t_normilizer, fitReport, monotonicityReport);
    }
}
