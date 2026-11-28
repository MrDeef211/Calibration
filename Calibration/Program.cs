using Calibration.IO.toml;
using Core;
using Core.Model.Exceptions;
using Core.Model.Pressure;
using Core.Model.Reports;
using Tomlyn;
using Tomlyn.Model;

internal class Program
{

    private enum CalibrateType
    {
        Pressure,
        Temperature
    }

    private static void Main(string[] args)
    {
        string firstArg =
            args.Length > 0
            ? args[0] : string.Empty;

        const string defaultFile = "Data.toml";
        const string defaultResultFile = " Report.toml";


        string dataFileName;
        bool firstArgName = false;
        CalibrateType? type = null;

        if (HasValidExtension(firstArg))
        {
            dataFileName = firstArg;

            string? calibrationType = null;

            try
            {
                if (File.Exists(dataFileName))
                {
                    firstArgName = true;

                    string text = File.ReadAllText(dataFileName);

                    TomlTable? root = TomlSerializer.Deserialize(text, CalibrationTomlContext.Default.TomlTable);

                    var identificationBlock = (TomlTable?)root?["identification"];

                    calibrationType = (string?)identificationBlock?["type"];
                }
                else
                {
                    throw new FileErrorException("Указанный файл не существует");
                }
            }
            catch (Exception ex)
            {
                Pressure.Write(null, null, ex, "error " + dataFileName);
                return;
            }

            type = calibrationType?.ToLowerInvariant() switch
            {
                "p" or "press" or "pressure" => CalibrateType.Pressure,
                "t" or "temp" or "temperature" => CalibrateType.Temperature,
                _ => null
            };
        }
        else
        {
            type = firstArg.ToLowerInvariant() switch
            {
                "--temperature" or "-temp" or "-t" => CalibrateType.Temperature,
                "--pressure" or "-press" or "-p" => CalibrateType.Pressure,
                _ => null
            };

            if (type == null && args.Length > 0)
                return;

            dataFileName = args.ElementAtOrDefault(1) switch
            {
                string file when HasValidExtension(file) => file,
                _ => defaultFile
            };
        }

        bool unknownType = type == null;
        bool temperature = unknownType || type == CalibrateType.Temperature;
        bool pressure = unknownType || type == CalibrateType.Pressure;

        string resultFileName = "";

        if (!firstArgName)
        {
            string dataFileOnlyName = Path.GetFileNameWithoutExtension(dataFileName);

            resultFileName = args.Length > 2 && HasValidExtension(args[2])
                ? args[2] : dataFileOnlyName + defaultResultFile;
        }
        else if (firstArgName)
        {
            string dataFileOnlyName = Path.GetFileNameWithoutExtension(dataFileName); ;

            resultFileName = args.Length > 1 && HasValidExtension(args[1])
                ? args[1] : dataFileOnlyName + defaultResultFile;
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
            string pressureResultFileName = unknownType ? "Pressure " + resultFileName : resultFileName;

            PressureData? pressureData = null;
            Identification? id = null;

            try
            {
                (pressureData, id) = Pressure.ReadFile(dataFileName);

                PressureCalibrationResult result = Calibrator.CalibratePressure(pressureData);

                Pressure.Write(id, pressureData, result, pressureResultFileName);
            }
            catch (Exception ex)
            {
                Pressure.Write(id, pressureData, ex, "error " + pressureResultFileName);
            }
        }

    }

    private static bool HasValidExtension(string fileName) =>
        !string.IsNullOrWhiteSpace(fileName) &&
        (fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
         fileName.EndsWith(".toml", StringComparison.OrdinalIgnoreCase));

}