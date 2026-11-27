using Core.Model;
using Core.Model.Pressure;
using Core.Model.Reports;
using Core.Model.Temperature;
using static Core.MathTool;

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

        PointsNormalizer normalizer;

        normalizer = new PointsNormalizer(min, max, 0.1);

        if (temperatureData.Normilize)
        {
            calibrationt_adc = normalizer.Normalize(calibrationt_adc);
            t_adc = normalizer.Normalize(t_adc);
        }

        coefficients = Polyfit1(
            pow,
            calibrationt_adc,
            calibrationt_ref
        );

        t = Calculate(
            coefficients,
            t_adc
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
            normalizer,
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

        double p_adcMin = pressureData.Adc_Range_p[0];
        double p_adcMax = pressureData.Adc_Range_p[1];

        double t_adcMin = pressureData.Adc_Range_t[0];
        double t_adcMax = pressureData.Adc_Range_t[1];

        double[,] coefficients;

        PointsNormalizer p_normalizer;
        PointsNormalizer t_normalizer;


        p_normalizer = new(p_adcMin, p_adcMax, 0);
        t_normalizer = new(t_adcMin, t_adcMax, 0);

        double[] p_adc = pressureData.P_Adc;
        double[] t_adc = pressureData.T_Adc;

        if (pressureData.Normilize)
        {
            calibrationt_p_adc = p_normalizer.Normalize(calibrationt_p_adc);
            calibrationt_t_adc = t_normalizer.Normalize(calibrationt_t_adc);
            p_adc = p_normalizer.Normalize(pressureData.P_Adc);
            t_adc = t_normalizer.Normalize(pressureData.T_Adc);
        }

        coefficients = Polyfit2(
            pressureData.P_Pow,
            pressureData.T_Pow,
            calibrationt_p_adc,
            calibrationt_t_adc,
            calibrationt_ref);

        p = Calculate(
            coefficients,
            p_adc,
            t_adc
        );

        dp = Difference(p, pressureData.P_Values);

        double[] ep = Quotient(dp, pressureData.Range_p[1] - pressureData.Range_p[0]);

        PressureMonotonicityReport monotonicityReport = new(coefficients);

        FitReport fitReport = new(
            pressureData.CalibrationIndexes,
            calibrationt_p_adc,
            calibrationt_t_adc,
            dp,
            ep,
            coefficients
        );

        return new PressureCalibrationResult(coefficients, p, dp, ep, p_normalizer, t_normalizer, fitReport, monotonicityReport);
    }
}
