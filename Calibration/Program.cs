using Calibration.IO;
using Core;
using Core.Model.Pressure;
using Core.Model.Temperature;

internal class Program
{
    private static void Main(string[] args)
    {
        string calibrationType =
            args.Length > 0
            ? args[0] : string.Empty;

        const string defaultTemperatureFile = "Temperature.txt";
        const string defaultPressureFile = "Pressure.txt";

        string dataFileName = (args.ElementAtOrDefault(1), calibrationType) switch
        {
            (string file, _) when file.EndsWith(".txt") || file.EndsWith(".svg") => file,
            (_, "--temperature") => defaultTemperatureFile,
            (_, "--pressure") => defaultPressureFile,
            _ => string.Empty
        };

        bool noParameters = calibrationType == string.Empty || dataFileName == string.Empty;
        bool temperature = noParameters || calibrationType == "--temperature";
        bool pressure = noParameters || calibrationType == "--pressure";

        string fileNameOnly;
        string resultFileName = "";

        if (!noParameters)
        {
            fileNameOnly = dataFileName[..dataFileName.LastIndexOf('.')];
            resultFileName = args.Length > 2
                && (args[2].Contains(".txt")
                || args[2].Contains(".svg"))
                ? args[2] : fileNameOnly + "Result.txt";
        }

        if (temperature)
        {
            dataFileName = noParameters ? defaultTemperatureFile : dataFileName;
            resultFileName = noParameters ? dataFileName[..dataFileName.LastIndexOf('.')]
                + "Result.txt" : resultFileName;

            try
            {
                TemperatureData temperatureData = Temperature.ReadFile(dataFileName);
                TemperatureCalibrationResult result = Calibrator.CalibrateTemperature(temperatureData);

                using StreamWriter stream = new(resultFileName, false);

                Temperature.PrintSummaryReport(result.MonotonicityReport, result, stream);

                Temperature.PrintCoefficients(result.Coefficients, stream);

                Temperature.PrintRanges(temperatureData, stream);

                Temperature.PrintDeviations(result, stream);

                Temperature.PrintReport(result.MonotonicityReport, stream);

                Temperature.WriteFile(
                    stream,
                    "Калибровка температуры",
                    temperatureData,
                    result.T, result.DT, result.ET
                );

            }
            catch (Exception ex)
            {
                Temperature.Print(ex, resultFileName);
            }

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
        }
        if (pressure)
        {
            dataFileName = noParameters ? defaultPressureFile : dataFileName;
            resultFileName = noParameters ? dataFileName[..dataFileName.LastIndexOf('.')]
                + "Result.txt" : resultFileName;

            try
            {
                PressureData pressureData = Pressure.ReadFile(dataFileName);
                PressureCalibrationResult result = Calibrator.CalibratePressure(pressureData);

                StreamWriter stream = new(resultFileName, false);

                Pressure.PrintSummaryReport(result.MonotonicityReport, result.FitReport, stream);

                Pressure.PrintCoefficients(result.Coefficients, stream);

                Pressure.PrintRanges(pressureData, stream);

                Pressure.PrintFitReport(result.FitReport, stream);

                Pressure.PrintMonotonicityReport(result.MonotonicityReport, stream);

                Pressure.WriteFile(
                    stream,
                    "Калибровка давления",
                    pressureData,
                    result.P, result.DP, result.EP
                );
            }
            catch (Exception ex)
            {
                Pressure.Print(ex, resultFileName);
            }
        }
    }
}