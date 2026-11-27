using Calibration.IO.toml;
using Core;
using Core.Model.Pressure;
using Core.Model.Reports;
using Core.Model.Temperature;

internal class Program
{
    private static void Main(string[] args)
    {
        string calibrationType =
            args.Length > 0
            ? args[0] : string.Empty;

        const string defaultTemperatureFile = "TemperatureInput.toml";
        const string defaultPressureFile = "PressureInput.toml";
        const string defaultTemperatureResultFile = "TemperatureReport.toml";
        const string defaultPressureResultFile = "PressureReport.toml";


        string dataFileName;
        bool firstParametrType = true;

        if (calibrationType.EndsWith(".toml"))
        {
            dataFileName = calibrationType;
            firstParametrType = false;
        }
        else
        {
            dataFileName = (args.ElementAtOrDefault(1), calibrationType) switch
            {
                (string file, _) when file.EndsWith(".txt") || file.EndsWith(".toml") => file,
                (_, "--temperature") => defaultTemperatureFile,
                (_, "-t") => defaultTemperatureFile,
                (_, "--pressure") => defaultPressureFile,
                (_, "-p") => defaultPressureFile,
                _ => string.Empty
            };
        }

        
        bool noParameters = calibrationType == string.Empty || dataFileName == string.Empty || !firstParametrType; //TODO
        bool temperature = noParameters || calibrationType == "--temperature" || calibrationType == "-t";
        bool pressure = noParameters || calibrationType == "--pressure" || calibrationType == "-p";

        string resultFileName = "";        

        if (!noParameters)
        {
            string dataFileOnlyName = dataFileName[..dataFileName.LastIndexOf('.')];

            resultFileName = args.Length > 2
                && (args[2].Contains(".txt")
                || args[2].Contains(".toml"))
                ? args[2] :  dataFileOnlyName + " Report.toml";
        }
        if (!firstParametrType)
        {
            string dataFileOnlyName = dataFileName[..dataFileName.LastIndexOf('.')];

            resultFileName = args.Length > 1
                && (args[1].Contains(".txt")
                || args[1].Contains(".toml"))
                ? args[1] : dataFileOnlyName + " Report.toml";
        }
        
        if (temperature)
        {
            /*
            dataFileName = noParameters ? defaultTemperatureFile : dataFileName;
            resultFileName = noParameters ? defaultTemperatureResultFile : resultFileName;
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
            */
        }
        if (pressure)
        {
            dataFileName = noParameters && firstParametrType ? defaultPressureFile : dataFileName;
            resultFileName = noParameters && firstParametrType ? defaultPressureResultFile : resultFileName;

            PressureData? pressureData = null;
            Identification? id = null;
            try
            {
                (pressureData, id) = Pressure.ReadFile(dataFileName);
            }
            catch (Exception ex)
            {
                Pressure.Write(id, pressureData, ex, resultFileName);
                return;
            }

            try
            {
                PressureCalibrationResult result = Calibrator.CalibratePressure(pressureData);

                Pressure.Write(id, pressureData, result, resultFileName);
            }
            catch (Exception ex)
            {
                Pressure.Write(id, pressureData, ex, resultFileName);
                return;
            }
        }
    }
}