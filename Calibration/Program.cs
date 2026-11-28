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

    private sealed record RunOptions(CalibrateType? Type, string InputFile, string BaseOutputFile);

    private const string DefaultInputFile = "Data.toml";
    private const string DefaultOutputSuffix = " Report.toml";

    private static void Main(string[] args)
    {
        RunOptions? options;
        try
        {
            options = ParseArguments(args);
        }
        catch (Exception ex)
        {
            string fallbackName = args.Length > 0 ? args[0] : DefaultInputFile;
            Pressure.Write(null, null, ex, BuildPrefixedPath(fallbackName, "error_"));
            return;
        }

        if (options is null)
            return;

        bool runAll = options.Type is null;

        if (runAll || options.Type == CalibrateType.Temperature)
        {
            string outputFile = runAll
                ? BuildPrefixedPath(options.BaseOutputFile, "Temperature ")
                : options.BaseOutputFile;

            RunTemperatureCalibration(options.InputFile, outputFile);
        }

        if (runAll || options.Type == CalibrateType.Pressure)
        {
            string outputFile = runAll
                ? BuildPrefixedPath(options.BaseOutputFile, "Pressure ")
                : options.BaseOutputFile;

            RunPressureCalibration(options.InputFile, outputFile);
        }
    }

    private static RunOptions? ParseArguments(string[] args)
    {
        if (args.Length == 0)
        {
            return new RunOptions(null, DefaultInputFile, BuildDefaultOutputName(DefaultInputFile));
        }

        string firstArg = args[0];

        if (HasValidExtension(firstArg))
        {
            if (!File.Exists(firstArg))
                throw new FileErrorException($"Указанный файл не существует: {firstArg}");

            CalibrateType? detectedType = DetectTypeFromFile(firstArg);
            string outputFile = args.Length > 1 && HasValidExtension(args[1])
                ? args[1]
                : BuildDefaultOutputName(firstArg);

            return new RunOptions(detectedType, firstArg, outputFile);
        }

        CalibrateType? flagType = firstArg.ToLowerInvariant() switch
        {
            "--temperature" or "-temp" or "-t" => CalibrateType.Temperature,
            "--pressure" or "-press" or "-p" => CalibrateType.Pressure,
            _ => null
        };

        if (flagType is null)
            return null;

        string inputFile = args.Length > 1 && HasValidExtension(args[1])
            ? args[1]
            : DefaultInputFile;

        string flagOutputFile = args.Length > 2 && HasValidExtension(args[2])
            ? args[2]
            : BuildDefaultOutputName(inputFile);

        return new RunOptions(flagType, inputFile, flagOutputFile);
    }

    private static CalibrateType? DetectTypeFromFile(string filePath)
    {
        TomlTable? root;

        try
        {
            string text = File.ReadAllText(filePath);
            root = TomlSerializer.Deserialize(text, CalibrationTomlContext.Default.TomlTable);
        }
        catch
        {
            return null;
        }

        if (root?.TryGetValue("identification", out object? idObj) == true &&
            idObj is TomlTable idBlock &&
            idBlock.TryGetValue("type", out object? typeObj) &&
            typeObj is string rawType)
        {
            return rawType.ToLowerInvariant() switch
            {
                "p" or "press" or "pressure" => CalibrateType.Pressure,
                "t" or "temp" or "temperature" => CalibrateType.Temperature,
                _ => null
            };
        }

        return null;
    }

    private static void RunPressureCalibration(string inputFile, string outputFile)
    {
        PressureData? pressureData = null;
        Identification? id = null;

        try
        {
            (pressureData, id) = Pressure.ReadFile(inputFile);
            PressureCalibrationResult result = Calibrator.CalibratePressure(pressureData);
            Pressure.Write(id, pressureData, result, outputFile);
        }
        catch (Exception ex)
        {
            Pressure.Write(id, pressureData, ex, BuildPrefixedPath(outputFile, "error_"));
        }
    }

    private static void RunTemperatureCalibration(string inputFile, string outputFile)
    {
        // TODO
    }

    private static string BuildDefaultOutputName(string inputPath)
    {
        string dir = Path.GetDirectoryName(inputPath) ?? string.Empty;
        string nameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);
        return Path.Combine(dir, nameWithoutExt + DefaultOutputSuffix);
    }

    private static string BuildPrefixedPath(string filePath, string prefix)
    {
        string dir = Path.GetDirectoryName(filePath) ?? string.Empty;
        string fileName = Path.GetFileName(filePath);
        return Path.Combine(dir, prefix + fileName);
    }

    private static bool HasValidExtension(string? fileName) =>
        !string.IsNullOrWhiteSpace(fileName) &&
        (fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
         fileName.EndsWith(".toml", StringComparison.OrdinalIgnoreCase));
}
