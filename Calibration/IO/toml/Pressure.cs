using Core.Model.Pressure;
using Core.Model.Reports;
using OfficeIMO.Toml;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Calibration.IO.toml
{
    internal static class Pressure
    {
        #region Чтение

        public static (PressureData, Identification) ReadFile(string path)
        {
            var text = File.ReadAllText(path);

            using var document = TomlDocument.Parse(text);
            TomlTable root = document.Root;

            var identificationBlock = root["identification"].AsTable();
            var paramsBlock = root["model_parameters"].AsTable();
            var dataBlock = root["data"].AsTable();

            string data = dataBlock["measurements_csv"].AsString();

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

                if (double.TryParse(cells[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double p1))
                    p_adc[index] = p1;
                else
                    break;

                if (double.TryParse(cells[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double t))
                    t_adc[index] = t;
                else
                    break;

                if (double.TryParse(cells[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double p2))
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

            var time = identificationBlock["timestamp"].GetValue<DateTime>();

            bool normalize = paramsBlock["normalize"].AsString() == "true";

            var adc_range_p = paramsBlock["adc_range_p"].AsArray()
                .Select(x => x.GetValue<double>()).ToArray();
            var adc_range_t = paramsBlock["adc_range_t"].AsArray()
                .Select(x => x.GetValue<double>()).ToArray();
            var range_p = paramsBlock["range_p"].AsArray()
                .Select(x => x.GetValue<double>()).ToArray();
            var range_t = paramsBlock["range_t"].AsArray()
                .Select(x => x.GetValue<double>()).ToArray();

            var identification = new Identification(
                time,
                identificationBlock["model"].AsString(),
                identificationBlock["serial_number"].AsString(),
                identificationBlock["software_version"].AsString(),
                identificationBlock["stand_id"].AsString(),
                identificationBlock["user"].AsString()
            );

            var pressureData = new PressureData(
                normalize,
                (int)paramsBlock["polynomial_degree_p"].GetValue<long>(),
                (int)paramsBlock["polynomial_degree_t"].GetValue<long>(),
                indexes,
                paramsBlock["unit_p"].AsString(),
                paramsBlock["unit_t"].AsString(),
                p_adc,
                t_adc,
                p_ref,
                adc_range_p,
                adc_range_t,
                range_p,
                range_t,
                paramsBlock["max_error_limit"].GetValue<double>(),
                paramsBlock["max_error_pct_limit"].GetValue<double>(),
                paramsBlock["rms_error_limit"].GetValue<double>(),
                paramsBlock["rms_error_pct_limit"].GetValue<double>()
            );

            return (pressureData, identification);
        }

        #endregion

        #region Запись

        public static void Write(Identification? id, PressureData? data, Exception ex, string path)
        {
            using var document = TomlDocument.Create();
            TomlTable root = document.Root;

            if (id != null)
            {
                TomlTable identification = root.AddTable("identification");
                identification.Add("timestamp", id.TimeStamp.ToString("yyyy-MM-ddTHH:mm:ss"))
                    .SetComment("Время калибровки");
                identification.Add("model", id.Model).SetComment("Модель прибора");
                identification.Add("serial_number", id.SerialNumber).SetComment("Серийный номер");
                identification.Add("software_version", id.SoftwareVersion).SetComment("Версия ПО");
                identification.Add("stand_id", id.StandId).SetComment("Идентификатор стенда");
                identification.Add("user", id.User).SetComment("Пользователь");
            }

            if (data != null)
            {
                TomlTable modelParams = root.AddTable("model_parameters");
                modelParams.Add("method", "Direct QR").SetComment("Метод калибровки");
                modelParams.Add("unit_p", data.Unit_p).SetComment("Единица измерения давления");
                modelParams.Add("unit_t", data.Unit_t).SetComment("Единица измерения температуры");
                modelParams.Add("normalize", data.Normilize ? "true" : "false").SetComment("Была ли нормализация");
                modelParams.Add("polynomial_degree_p", data.P_Pow).SetComment("Степень полинома по давлению");
                modelParams.Add("polynomial_degree_t", data.T_Pow).SetComment("Степень полинома по температуре");

                modelParams.Add("range_p", new[] { data.Range_p[0], data.Range_p[1] })
                    .SetComment("Диапазон давления");
                modelParams.Add("range_t", new[] { data.Range_t[0], data.Range_t[1] })
                    .SetComment("Диапазон температуры");
                modelParams.Add("adc_range_p", new[] { data.Adc_Range_p[0], data.Adc_Range_p[1] })
                    .SetComment("Диапазон АЦП давления");
                modelParams.Add("adc_range_t", new[] { data.Adc_Range_t[0], data.Adc_Range_t[1] })
                    .SetComment("Диапазон АЦП температуры");
                modelParams.Add("max_error_limit", data.LimitMaxError).SetComment("Допуск макс. погрешности");
                modelParams.Add("rms_error_limit", data.LimitStandardError).SetComment("Допуск СКО");
                modelParams.Add("max_error_pct_limit", data.LimitMaxRelativeError)
                    .SetComment("Допуск макс. приведённой погрешности");
                modelParams.Add("rms_error_pct_limit", data.LimitStandardRelativeError)
                    .SetComment("Допуск СКО приведённой погрешности");
            }

            TomlTable resultTable = root.AddTable("result");
            resultTable.Add("status", FormatStatus(false)).SetComment("Итоговый статус");
            resultTable.Add("error_info", ex.Message).SetComment("Описание ошибки");

            string tomlText = document.ToString();

            const string header =
                "#                ПРОТОКОЛ КАЛИБРОВКИ ДАВЛЕНИЯ С ОШИБКОЙ\n" +
                "#==============================================================================\n\n";

            File.WriteAllText(path, header + tomlText);
            Console.WriteLine(header + tomlText);
        }

        public static void Write(Identification id, PressureData data, PressureCalibrationResult result, string path)
        {
            using var document = TomlDocument.Create();
            TomlTable root = document.Root;

            // identification
            TomlTable identification = root.AddTable("identification");
            identification.Add("timestamp", id.TimeStamp.ToString("yyyy-MM-ddTHH:mm:ss"))
                .SetComment("Время калибровки");
            identification.Add("model", id.Model).SetComment("Модель прибора");
            identification.Add("serial_number", id.SerialNumber).SetComment("Серийный номер");
            identification.Add("software_version", id.SoftwareVersion).SetComment("Версия ПО");
            identification.Add("stand_id", id.StandId).SetComment("Идентификатор стенда");
            identification.Add("user", id.User).SetComment("Пользователь");

            // model_parameters
            TomlTable modelParams = root.AddTable("model_parameters");
            modelParams.Add("method", "Direct QR").SetComment("Метод калибровки");
            modelParams.Add("unit_p", data.Unit_p).SetComment("Единица измерения давления");
            modelParams.Add("unit_t", data.Unit_t).SetComment("Единица измерения температуры");
            modelParams.Add("normalize", data.Normilize ? "true" : "false").SetComment("Была ли нормализация");
            modelParams.Add("polynomial_degree_p", data.P_Pow).SetComment("Степень полинома по давлению");
            modelParams.Add("polynomial_degree_t", data.T_Pow).SetComment("Степень полинома по температуре");
            modelParams.Add("range_p", new[] { data.Range_p[0], data.Range_p[1] })
                .SetComment("Диапазон давления");
            modelParams.Add("range_t", new[] { data.Range_t[0], data.Range_t[1] })
                .SetComment("Диапазон температуры");
            modelParams.Add("adc_range_p", new[] { data.Adc_Range_p[0], data.Adc_Range_p[1] })
                .SetComment("Диапазон АЦП давления");
            modelParams.Add("adc_range_t", new[] { data.Adc_Range_t[0], data.Adc_Range_t[1] })
                .SetComment("Диапазон АЦП температуры");
            modelParams.Add("max_error_limit", data.LimitMaxError).SetComment("Допуск макс. погрешности");
            modelParams.Add("rms_error_limit", data.LimitStandardError).SetComment("Допуск СКО");
            modelParams.Add("max_error_pct_limit", data.LimitMaxRelativeError)
                .SetComment("Допуск макс. приведённой погрешности");
            modelParams.Add("rms_error_pct_limit", data.LimitStandardRelativeError)
                .SetComment("Допуск СКО приведённой погрешности");

            // metrology_calib_result
            bool calibOk = result.FitReport.CalibrationMaxError <= data.LimitMaxError
                && result.FitReport.CalibrationStandardError <= data.LimitStandardError
                && result.FitReport.CalibrationMaxRelativeError * 100 <= data.LimitMaxRelativeError
                && result.FitReport.CalibrationRelativeStandardError * 100 <= data.LimitStandardRelativeError;

            string calibError = "Ошибок нет";
            if (!calibOk)
            {
                var errors = new List<string>();
                if (result.FitReport.CalibrationMaxError > data.LimitMaxError)
                    errors.Add("Максимальная погрешность больше допустимой");
                if (result.FitReport.CalibrationStandardError > data.LimitStandardError)
                    errors.Add("Среднеквадратичная погрешность больше допустимой");
                if (result.FitReport.CalibrationMaxRelativeError * 100 > data.LimitMaxRelativeError)
                    errors.Add("Максимальная приведённая погрешность больше допустимой");
                if (result.FitReport.CalibrationRelativeStandardError * 100 > data.LimitStandardRelativeError)
                    errors.Add("Среднеквадратичная приведённая погрешность больше допустимой");
                calibError = string.Join(", ", errors);
            }

            TomlTable calibTable = root.AddTable("metrology_calib_result");
            calibTable.Add("status", FormatStatus(calibOk));
            calibTable.Add("error_info", calibError);
            calibTable.Add("max_error", result.FitReport.CalibrationMaxError)
                .SetComment("Максимальная погрешность");
            calibTable.Add("rms_error", result.FitReport.CalibrationStandardError)
                .SetComment("СКО погрешности");
            calibTable.Add("max_error_pct", result.FitReport.CalibrationMaxRelativeError * 100)
                .SetComment("Максимальная приведённая погрешность, %");
            calibTable.Add("rms_error_pct", result.FitReport.CalibrationRelativeStandardError * 100)
                .SetComment("СКО приведённой погрешности, %");

            // metrology_test_result
            bool testOk = result.FitReport.TestMaxError <= data.LimitMaxError
                && result.FitReport.TestStandardError <= data.LimitStandardError
                && result.FitReport.TestMaxRelativeError * 100 <= data.LimitMaxRelativeError
                && result.FitReport.TestRelativeStandardError * 100 <= data.LimitStandardRelativeError;

            string testError = "Ошибок нет";
            if (!testOk)
            {
                var errors = new List<string>();
                if (result.FitReport.TestMaxError > data.LimitMaxError)
                    errors.Add("Максимальная погрешность больше допустимой");
                if (result.FitReport.TestStandardError > data.LimitStandardError)
                    errors.Add("Среднеквадратичная погрешность больше допустимой");
                if (result.FitReport.TestMaxRelativeError * 100 > data.LimitMaxRelativeError)
                    errors.Add("Максимальная приведённая погрешность больше допустимой");
                if (result.FitReport.TestRelativeStandardError * 100 > data.LimitStandardRelativeError)
                    errors.Add("Среднеквадратичная приведённая погрешность больше допустимой");
                testError = string.Join(", ", errors);
            }

            TomlTable testTable = root.AddTable("metrology_test_result");
            testTable.Add("status", FormatStatus(testOk));
            testTable.Add("error_info", testError);
            testTable.Add("max_error", result.FitReport.TestMaxError)
                .SetComment("Максимальная погрешность");
            testTable.Add("rms_error", result.FitReport.TestStandardError)
                .SetComment("СКО погрешности");
            testTable.Add("max_error_pct", result.FitReport.TestMaxRelativeError * 100)
                .SetComment("Максимальная приведённая погрешность, %");
            testTable.Add("rms_error_pct", result.FitReport.TestRelativeStandardError * 100)
                .SetComment("СКО приведённой погрешности, %");

            // math_stability
            bool mathOk = result.MonotonicityReport.Verdict == MonotonicityVerdict.Ok
                && result.FitReport.Rank >= result.Coefficients.Length;

            string mathError = "Ошибок нет";
            if (!mathOk)
            {
                var mathErrors = new List<string>();
                if (result.MonotonicityReport.Verdict == MonotonicityVerdict.WeakMargin)
                    mathErrors.Add("Малый запас по монотонности");
                if (result.MonotonicityReport.Verdict == MonotonicityVerdict.Violated)
                    mathErrors.Add("Нарушение монотонности");
                if (result.FitReport.Rank < result.Coefficients.Length)
                    mathErrors.Add($"rank(H)={result.FitReport.Rank} < {result.Coefficients.Length}: матрица вырождена");
                mathError = string.Join(", ", mathErrors);
            }

            TomlTable mathTable = root.AddTable("math_stability");
            mathTable.Add("status", FormatStatus(mathOk));
            mathTable.Add("error_info", mathError);
            mathTable.Add("check_grid", new[] { result.MonotonicityReport.Nu, result.MonotonicityReport.Nv }).SetComment("Сетка проверки");
            mathTable.Add("monotonicity_ok", result.MonotonicityReport.Verdict == MonotonicityVerdict.Ok)
                .SetComment("Монотонность соблюдена");
            mathTable.Add("monotonicity_violations", result.MonotonicityReport.ViolationsCount)
                .SetComment("Количество нарушений монотонности");
            mathTable.Add("min_derivative_g", result.MonotonicityReport.MinAbsG)
                .SetComment("Минимальная производная");
            mathTable.Add("mean_derivative_g", result.MonotonicityReport.MeanAbsG)
                .SetComment("Средняя производная");
            mathTable.Add("condition_number", result.FitReport.ConditionNumber)
                .SetComment("Число обусловленности");
            mathTable.Add("matrix_rank", result.FitReport.Rank).SetComment("Ранг матрицы");

            // polynomial
            int rows = result.Coefficients.GetLength(0);
            int cols = result.Coefficients.GetLength(1);

            var coeffsJagged = new double[rows][];
            for (int i = 0; i < rows; i++)
            {
                coeffsJagged[i] = new double[cols];
                for (int j = 0; j < cols; j++)
                    coeffsJagged[i][j] = result.Coefficients[i, j];
            }

            TomlTable polyTable = root.AddTable("polynomial");
            polyTable.Add("number_of_coefficients", result.FitReport.Coefficients > 0
                    ? result.FitReport.Coefficients
                    : rows * cols)
                .SetComment("Количество коэффициентов");
            polyTable.Add("formula",
                    $"P(u,v) = sum a[i,j] * u^i * v^j,  i=0..{data.P_Pow}, j=0..{data.T_Pow}")
                .SetComment("Формула полинома");

            polyTable.Add("coefficients", coeffsJagged);

            // data
            TomlTable dataTable = root.AddTable("data");
            dataTable.Add("number_of_points", data.P_Adc.Length).SetComment("Общее количество точек");
            dataTable.Add("measurements_csv", BuildMeasurementsCsv(data, result));

            string tomlText = ApplyCustomFormatting(document.ToString());

            const string header =
                "#                ПРОТОКОЛ КАЛИБРОВКИ ДАВЛЕНИЯ\n" +
                "#==============================================================================\n\n";

            File.WriteAllText(path, header + tomlText);
            Console.WriteLine(header + tomlText);
        }

        private static string FormatStatus(bool ok) => ok ? "✅️ - PASS" : "❌️ - FAIL";

        private static string BuildMeasurementsCsv(PressureData data, PressureCalibrationResult result)
        {
            var lines = new List<string>
            {
                "callib;   adc_p     ;    adc_t    ;  reference  ;    value    ;   abs.error"
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

        private static string ApplyCustomFormatting(string tomlText)
        {

            tomlText = FormatCoefficients(tomlText);

            tomlText = FormatCsvString(tomlText);

            return tomlText;
        }

        private static string FormatCoefficients(string tomlText)
        {

            var pattern = @"coefficients\s*=\s*\[\[(.*)\]\]";
            var match = Regex.Match(tomlText, pattern);
            if (!match.Success) return tomlText;

            string inner = match.Groups[1].Value.Trim();

            var rows = Regex.Split(inner, @"\]\s*,\s*\[");

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("coefficients = [");
            foreach (var row in rows)
            {

                var cleanRow = row.Trim().TrimStart('[').TrimEnd(']');
                sb.AppendLine($"    [{cleanRow}],");
            }

            sb.Length -= 3; 
            sb.AppendLine("\n]");

            return tomlText.Substring(0, match.Index) +
                   sb.ToString() +
                   tomlText.Substring(match.Index + match.Length);
        }

        private static string FormatCsvString(string tomlText)
        {

            var pattern = @"(measurements_csv\s*=\s*)""(.*?)""";
            var match = Regex.Match(tomlText, pattern, RegexOptions.Singleline);
            if (!match.Success) return tomlText;

            string prefix = match.Groups[1].Value; 
            string rawContent = match.Groups[2].Value;

            string multilineContent = rawContent.Replace("\\n", "\n");

            string replacement = $"{prefix}\"\"\"\n{multilineContent}\n\"\"\"";

            return tomlText.Substring(0, match.Index) +
                   replacement +
                   tomlText.Substring(match.Index + match.Length);
        }

        #endregion
    }
}