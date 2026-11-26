using Core.Model.Exceptions;
using Core.Model.Pressure;
using Core.Model.Reports;
using Core.Model.Temperature;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Tomlyn;
using Tomlyn.Model;

namespace Calibration.IO.toml
{
    internal static class Pressure
    {
        #region Чтение

        public static (PressureData, Identification) ReadFile(string path)
        {
            var text = File.ReadAllText(path);

            TomlTable root = TomlSerializer.Deserialize(text, CalibrationTomlContext.Default.TomlTable)
                ?? throw new FileErrorException("Файл пустой или неверный формат");

            var identificationBlock = (TomlTable)root["identification"];
            var paramsBlock = (TomlTable)root["model_parameters"];
            var dataBlock = (TomlTable)root["data"];

            string data = (string)dataBlock["measurements_csv"];

            string[] dataLines = data.Split('\n')[1..];
            string[] cells;

            double[] p_adc = new double[dataLines.Length];
            double[] t_adc = new double[dataLines.Length];
            double[] p_ref = new double[dataLines.Length];
            int[] indexes = new int[dataLines.Length];
            int calibratedPoints = 0;

            int index = 0;
            while (index < dataLines.Length)
            {
                string? line = dataLines[index];
                if (line == null || line == "") break;

                cells = Regex.Split(line.Replace(',', '.'), @"[\s;]+");

                if (cells.Length > 0 && (cells[0] == "*" || cells[0] == "true"))
                {
                    indexes[calibratedPoints] = index;
                    calibratedPoints++;
                }

                if (double.TryParse(cells[1], out double p1))
                    p_adc[index] = p1;
                else
                    break;

                if (double.TryParse(cells[2], out double t))
                    t_adc[index] = t;
                else
                    break;

                if (double.TryParse(cells[3], out double p2))
                    p_ref[index] = p2;
                else
                    break;

                index++;
            }

            if (index < dataLines.Length)
            {
                Array.Resize(ref p_adc, index);
                Array.Resize(ref t_adc, index);
                Array.Resize(ref p_ref, index);
            }

            if (calibratedPoints == 0)
                for (calibratedPoints = 0; calibratedPoints < p_adc.Length; calibratedPoints++)
                    indexes[calibratedPoints] = calibratedPoints;

            if (calibratedPoints < indexes.Length)
                Array.Resize(ref indexes, calibratedPoints);

            var time = (TomlDateTime)identificationBlock["timestamp"];

            bool normalize = (string)paramsBlock["normalize"] == "true";

            TomlArray adc_range_p = (TomlArray)paramsBlock["adc_range_p"];
            TomlArray adc_range_t = (TomlArray)paramsBlock["adc_range_t"];
            TomlArray range_p = (TomlArray)paramsBlock["range_p"];
            TomlArray range_t = (TomlArray)paramsBlock["range_t"];


            var identification = new Identification(
                time.DateTime,
                (string)identificationBlock["model"],
                (string)identificationBlock["serial_number"],
                (string)identificationBlock["software_version"],
                (string)identificationBlock["stand_id"],
                (string)identificationBlock["user"]
                );

            var pressureData = new PressureData(
                normalize,
                (int)(long)paramsBlock["polynomial_degree_p"],
                (int)(long)paramsBlock["polynomial_degree_t"],
                indexes,
                (string)paramsBlock["unit_p"],
                (string)paramsBlock["unit_t"],
                p_adc,
                t_adc,
                p_ref,
                adc_range_p.Select(x => Convert.ToDouble(x, CultureInfo.InvariantCulture)).ToArray(),
                adc_range_t.Select(x => Convert.ToDouble(x, CultureInfo.InvariantCulture)).ToArray(),
                range_p.Select(x => Convert.ToDouble(x, CultureInfo.InvariantCulture)).ToArray(),
                range_t.Select(x => Convert.ToDouble(x, CultureInfo.InvariantCulture)).ToArray(),
                Convert.ToDouble(paramsBlock["max_error_limit"], CultureInfo.InvariantCulture),
                Convert.ToDouble(paramsBlock["max_error_pct_limit"], CultureInfo.InvariantCulture),
                Convert.ToDouble(paramsBlock["rms_error_limit"], CultureInfo.InvariantCulture),                
                Convert.ToDouble(paramsBlock["rms_error_pct_limit"], CultureInfo.InvariantCulture)
            );

            return (pressureData, identification);
        }

        #endregion

        #region Запись

        public static void Write(Identification? id, PressureData? data, Exception ex, string path)
        {
            var root = new TomlTable();

            if (id != null)
            {
                root["identification"] = new TomlTable
                {
                    ["timestamp"] = id.TimeStamp.ToString("yyyy-MM-ddTHH:mm:ss"),
                    ["model"] = id.Model,
                    ["serial_number"] = id.SerialNumber,
                    ["software_version"] = id.SoftwareVersion,
                    ["stand_id"] = id.StandId,
                    ["user"] = id.User
                };
            }

            if (data != null)
            {
                root["model_parameters"] = new TomlTable
                {
                    ["method"] = "Direct QR",
                    ["unit_p"] = data.Unit_p,
                    ["unit_t"] = data.Unit_t,
                    ["normalize"] = data.Normilize ? "true" : "false",
                    ["polynomial_degree_p"] = data.P_Pow,
                    ["polynomial_degree_t"] = data.T_Pow,
                    ["range_p"] = new TomlArray { data.Range_p[0], data.Range_p[1] },
                    ["range_t"] = new TomlArray { data.Range_t[0], data.Range_t[1] },
                    ["adc_range_p"] = new TomlArray { data.Adc_Range_p[0], data.Adc_Range_p[1] },
                    ["adc_range_t"] = new TomlArray { data.Adc_Range_t[0], data.Adc_Range_t[1] },
                    ["max_error_limit"] = data.LimitMaxError,
                    ["rms_error_limit"] = data.LimitStandardError,
                    ["max_error_pct_limit"] = data.LimitMaxRelativeError,
                    ["rms_error_pct_limit"] = data.LimitStandardRelativeError
                };
            }

            root["result"] = new TomlTable
            {
                ["status"] = FormatStatus(false),
                ["error_info"] = ex.Message
            };

            var options = new TomlSerializerOptions
            {
                BlankLineBeforeTableHeaders = true
            };

            var tomlRootText = TomlSerializer.Serialize(root, options);

            const string header =
                "#                ПРОТОКОЛ КАЛИБРОВКИ ДАВЛЕНИЯ С ОШИБКОЙ\n" +
                "#==============================================================================\n\n";

            File.WriteAllText(path, header + tomlRootText);
            Console.WriteLine(header + tomlRootText);
        }

        public static void Write(Identification id, PressureData data, PressureCalibrationResult result, string path)
        {
            var root = new TomlTable
            {
                ["identification"] = new TomlTable
                {
                    ["timestamp"] = id.TimeStamp.ToString("yyyy-MM-ddTHH:mm:ss"),
                    ["model"] = id.Model,
                    ["serial_number"] = id.SerialNumber,
                    ["software_version"] = id.SoftwareVersion,
                    ["stand_id"] = id.StandId,
                    ["user"] = id.User
                },

                ["model_parameters"] = new TomlTable
                {
                    ["method"] = "Direct QR",
                    ["unit_p"] = data.Unit_p,
                    ["unit_t"] = data.Unit_t,
                    ["normalize"] = data.Normilize ? "true" : "false",
                    ["polynomial_degree_p"] = data.P_Pow,
                    ["polynomial_degree_t"] = data.T_Pow,
                    ["range_p"] = new TomlArray { data.Range_p[0], data.Range_p[1] },
                    ["range_t"] = new TomlArray { data.Range_t[0], data.Range_t[1] },
                    ["adc_range_p"] = new TomlArray { data.Adc_Range_p[0], data.Adc_Range_p[1] },
                    ["adc_range_t"] = new TomlArray { data.Adc_Range_t[0], data.Adc_Range_t[1] },
                    ["max_error_limit"] = data.LimitMaxError,
                    ["rms_error_limit"] = data.LimitStandardError,
                    ["max_error_pct_limit"] = data.LimitMaxRelativeError,
                    ["rms_error_pct_limit"] = data.LimitStandardRelativeError
                }
            };

            bool calibOk = result.FitReport.CalibrationMaxError <= data.LimitMaxError
                && result.FitReport.CalibrationStandardError <= data.LimitStandardError
                && result.FitReport.CalibrationMaxRelativeError*100 <= data.LimitMaxRelativeError
                && result.FitReport.CalibrationRelativeStandardError*100 <= data.LimitStandardRelativeError;

            bool testOk = result.FitReport.TestMaxError <= data.LimitMaxError
                && result.FitReport.TestStandardError <= data.LimitStandardError
                && result.FitReport.TestMaxRelativeError*100 <= data.LimitMaxRelativeError
                && result.FitReport.TestRelativeStandardError*100 <= data.LimitStandardRelativeError;


            bool mathOk = result.MonotonicityReport.Verdict == MonotonicityVerdict.Ok 
                && (result.FitReport.Rank >= result.Coefficients.Length);

            string calibError = "Ошибок нет";

            if (!calibOk)
            {
                List<string> errors = [];
                if (result.FitReport.CalibrationMaxError > data.LimitMaxError)
                    errors.Add("Максимальная погрешность больше допустимой");
                if (result.FitReport.CalibrationStandardError > data.LimitStandardError)
                    errors.Add("Среднеквадратичная погрешность больше допустимой");
                if (result.FitReport.CalibrationMaxRelativeError*100 > data.LimitMaxRelativeError)
                    errors.Add("Максимальная приведённая погрешность больше допустимой");
                if (result.FitReport.CalibrationRelativeStandardError*100 > data.LimitStandardRelativeError)
                    errors.Add("Среднеквадратичная приведённая погрешность больше допустимой");

                calibError = String.Join(", ", errors);
            }

            root["metrology_calib_result"] = new TomlTable
            {
                ["status"] = FormatStatus(calibOk),
                ["error_info"] = calibError,
                ["max_error"] = result.FitReport.CalibrationMaxError,      
                ["rms_error"] = result.FitReport.CalibrationStandardError, 
                ["max_error_pct"] = result.FitReport.CalibrationMaxRelativeError*100,
                ["rms_error_pct"] = result.FitReport.CalibrationRelativeStandardError*100
            };

            string testError = "Ошибок нет";

            if (!testOk)
            {
                List<string> errors = [];
                if (result.FitReport.TestMaxError > data.LimitMaxError)
                    errors.Add("Максимальная погрешность больше допустимой");
                if (result.FitReport.TestStandardError > data.LimitStandardError)
                    errors.Add("Среднеквадратичная погрешность больше допустимой");
                if (result.FitReport.TestMaxRelativeError*100 > data.LimitMaxRelativeError)
                    errors.Add("Максимальная приведённая погрешность больше допустимой");
                if (result.FitReport.TestRelativeStandardError*100 > data.LimitStandardRelativeError)
                    errors.Add("Среднеквадратичная приведённая погрешность больше допустимой");

                testError = String.Join(", ", errors);
            }

            root["metrology_test_result"] = new TomlTable
            {
                ["status"] = FormatStatus(testOk),
                ["error_info"] = testError,
                ["max_error"] = result.FitReport.TestMaxError,
                ["rms_error"] = result.FitReport.TestStandardError,
                ["max_error_pct"] = result.FitReport.TestMaxRelativeError*100,     
                ["rms_error_pct"] = result.FitReport.TestRelativeStandardError*100 
            };

            string mathError = "Ошибок нет";

            if (!mathOk)
            {
                List<string> mathErrors = [];
                if (result.MonotonicityReport.Verdict == MonotonicityVerdict.WeakMargin)
                    mathErrors.Add("Малый запас по монотонности");
                if (result.MonotonicityReport.Verdict == MonotonicityVerdict.Violated)
                    mathErrors.Add("Нарушение монотонности");
                if (result.FitReport.Rank < result.Coefficients.Length)
                    mathErrors.Add($"rank(H)={result.FitReport.Rank} < {result.Coefficients.Length}" +
                        $": матрица вырождена, значения имеют линейную зависимость, либо лежат на одной прямой " +
                        $"Уменьшите степени или добавьте РАЗНЫЕ точки.");
                mathError = String.Join(", ", mathErrors);
            }

            root["math_stability"] = new TomlTable
            {
                ["status"] = FormatStatus(mathOk),
                ["error_info"] = mathError,
                ["check_grid"] = new TomlArray { 100, 100 },
                ["monotonicity_ok"] = result.MonotonicityReport.Verdict == MonotonicityVerdict.Ok,
                ["monotonicity_violations"] = result.MonotonicityReport.ViolationsCount,
                ["min_derivative_g"] = result.MonotonicityReport.MinAbsG,
                ["mean_derivative_g"] = result.MonotonicityReport.MeanAbsG,
                ["condition_number"] = result.FitReport.ConditionNumber,
                ["matrix_rank"] = result.FitReport.Rank    
            };


            int rows = result.Coefficients.GetLength(0);
            int cols = result.Coefficients.GetLength(1);
            var coeffsArray = new TomlArray();
            for (int i = 0; i < rows; i++)
            {
                var row = new TomlArray();
                for (int j = 0; j < cols; j++)
                    row.Add(result.Coefficients[i, j]);
                coeffsArray.Add(row);
            }

            TomlTable multiline = new TomlTable
            {
                ["polynomial"] = new TomlTable
                {
                    ["number_of_coefficients"] = result.FitReport.Coefficients > 0
                                                ? result.FitReport.Coefficients
                                                : rows * cols,
                    ["formula"] = $"P(u,v) = sum a[i,j] * u^i * v^j,  i=0..{data.P_Pow}, j=0..{data.T_Pow}",
                    ["coefficients"] = coeffsArray
                },

                ["data"] = new TomlTable
                {
                    ["number_of_points"] = data.P_Adc.Length,
                    ["measurements_csv"] = BuildMeasurementsCsv(data, result)
                }
            };

            var options = new TomlSerializerOptions
            {
                BlankLineBeforeTableHeaders = true
            };

            var multilineOptions = new TomlSerializerOptions
            {
                WriteIndented = true,
                IndentSize = 4,
                BlankLineBeforeTableHeaders = true,

                ArrayLayout = TomlArrayLayout.Multiline,

                StringStylePreferences = new TomlStringStylePreferences
                {
                    DefaultStyle = TomlStringStyle.MultilineBasic
                }
            };

            var tomlRootText = TomlSerializer.Serialize(root, options);

            var tolmMultilineText = TomlSerializer.Serialize(multiline, multilineOptions);

            const string header =
                "#                ПРОТОКОЛ КАЛИБРОВКИ ДАВЛЕНИЯ\n" +
                "#==============================================================================\n\n";

            File.WriteAllText(path, header + tomlRootText + "\n" + tolmMultilineText);
            Console.WriteLine(header + tomlRootText + "\n" + tolmMultilineText);
        }

        private static string FormatStatus(bool ok) => ok ? "✅️ - PASS" : "❌️ - FAIL";

        private static string BuildMeasurementsCsv(PressureData data, PressureCalibrationResult result)
        {
            var lines = new List<string>
            {
                "\ncallib;   adc_p     ;    adc_t    ;  reference  ;    value    ;   abs.error"
            }; 
            for (int i = 0; i < data.P_Adc.Length; i++)
            {
                bool isCalib = data.CalibrationIndexes.Contains(i);
                lines.Add(
                    $"{(isCalib ? "true" : ""),-6};   " +
                    $"{data.P_Adc[i],-10:F3};   " +
                    $"{data.T_Adc[i],-10:F3};   " +
                    $"{data.P_Values[i],-10:F3};   " +
                    $"{result.P[i],-10:F3};   " +
                    $"{result.DP[i]:F6}");
            }
            return string.Join("\n", lines);   
        }

        #endregion
    }
}
