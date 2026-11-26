using Core.Model.Reports;
using Core.Model.Temperature;
using System.Text.RegularExpressions;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Serialization;

namespace Calibration.IO.toml
{
    internal static class Temperature
    {
        #region Чтение

        public static TemperatureData ReadFile(string path)
        {
            var text = File.ReadAllText(path);

            TomlTable root = TomlSerializer.Deserialize(text, CalibrationTomlContext.Default.TomlTable)
                ?? throw new Exception("Файл пустой или неверный формат");

            var paramsBlock = (TomlTable)root["params"];
            var dataBlock = (TomlTable)root["data"];

            string data = (string)dataBlock["measurements_csv"];

            string[] dataLines = data.Split('\n');
            string[] cells;

            double[] t_adc = new double[dataLines.Length];
            double[] t_ref = new double[dataLines.Length];
            int[] indexes = new int[dataLines.Length];
            int calibratedPoints = 0;

            int index = 0;
            while (index < dataLines.Length)
            {
                string? line = dataLines[index];
                if (line == null || line == "") break;

                cells = Regex.Split(line.Replace(',', '.'), @"[\s;]+");

                if (double.TryParse(cells[0], out double t1))
                    t_adc[index] = t1;
                else
                    break;

                if (double.TryParse(cells[1], out double t2))
                    t_ref[index] = t2;
                else
                    break;

                if (cells[2] == "*" || cells[2] == "true")
                {
                    indexes[calibratedPoints] = index;
                    calibratedPoints++;
                }

                index++;
            }

            if (index < dataLines.Length)
            {
                Array.Resize(ref t_adc, index);
                Array.Resize(ref t_ref, index);
            }

            if (calibratedPoints == 0)
                for (calibratedPoints = 0; calibratedPoints < t_adc.Length; calibratedPoints++)
                    indexes[calibratedPoints] = calibratedPoints;

            if (calibratedPoints < indexes.Length)
                Array.Resize(ref indexes, calibratedPoints);

            return new TemperatureData(
                (bool)paramsBlock["normilize"],
                (int)paramsBlock["pow"],
                indexes,
                t_adc,
                t_ref,
                (double)paramsBlock["t_adc_min"],
                (double)paramsBlock["t_adc_max"],
                (double)paramsBlock["t_min"],
                (double)paramsBlock["t_max"]
            );
        }

        #endregion

        #region Запись

        internal static void Print(Identification id, Exception ex, string path)
        {
            using StreamWriter stream = new(path, false);
            Console.WriteLine(ex.Message);
            stream.WriteLine(ex.Message);

            stream.Flush();
        }

        public static void Write(Identification id, TemperatureData data, TemperatureCalibrationResult result, string path)
        {

        }

        #endregion
    }

    [TomlSerializable(typeof(TomlTable))]
    internal partial class CalibrationTomlContext : TomlSerializerContext
    {
    }
}
