using Core.Model.Exceptions;
using Core.Model.Pressure;
using Core.Model.Reports;
using System.Globalization;
using System.Text.RegularExpressions;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;

namespace Calibration.IO.toml
{
    internal static class Pressure
    {
        #region Чтение

        public static (PressureData, Identification) ReadFile(string path)
        {
            var text = File.ReadAllText(path);

            TomlTable root = TomlSerializer.Deserialize<TomlTable>(text, CalibrationTomlContext.Default.TomlTable)
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
                [.. adc_range_p.Select(x => Convert.ToDouble(x, CultureInfo.InvariantCulture))],
                [.. adc_range_t.Select(x => Convert.ToDouble(x, CultureInfo.InvariantCulture))],
                [.. range_p.Select(x => Convert.ToDouble(x, CultureInfo.InvariantCulture))],
                [.. range_t.Select(x => Convert.ToDouble(x, CultureInfo.InvariantCulture))],
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
            var doc = new DocumentSyntax();

            if (id != null)
            {
                var idTable = new TableSyntax("identification");
                idTable.AddLeadingTriviaNewLine();

                AddKeyValue(idTable, "timestamp", new StringValueSyntax(id.TimeStamp.ToString("yyyy-MM-ddTHH:mm:ss")), "Время калибровки");
                AddKeyValue(idTable, "model", new StringValueSyntax(id.Model), "Модель прибора");
                AddKeyValue(idTable, "serial_number", new StringValueSyntax(id.SerialNumber), "Серийный номер");
                AddKeyValue(idTable, "software_version", new StringValueSyntax(id.SoftwareVersion), "Версия ПО");
                AddKeyValue(idTable, "stand_id", new StringValueSyntax(id.StandId), "Идентификатор стенда");
                AddKeyValue(idTable, "user", new StringValueSyntax(id.User), "Пользователь");

                doc.Tables.Add(idTable);
            }

            if (data != null)
            {
                var paramsTable = new TableSyntax("model_parameters");
                paramsTable.AddLeadingTriviaNewLine();

                AddKeyValue(paramsTable, "method", new StringValueSyntax("Direct QR"), "Метод калибровки");
                AddKeyValue(paramsTable, "unit_p", new StringValueSyntax(data.Unit_p), "Единица измерения давления");
                AddKeyValue(paramsTable, "unit_t", new StringValueSyntax(data.Unit_t), "Единица измерения температуры");
                AddKeyValue(paramsTable, "normalize", new StringValueSyntax(data.Normilize ? "true" : "false"), "Присутствует нормализация");
                AddKeyValue(paramsTable, "polynomial_degree_p", new IntegerValueSyntax(data.P_Pow), "Степень полинома по давлению");
                AddKeyValue(paramsTable, "polynomial_degree_t", new IntegerValueSyntax(data.T_Pow), "Степень полинома по температуре");

                AddKeyValue(paramsTable, "range_p", CreateArray(data.Range_p), "Диапазон давления");
                AddKeyValue(paramsTable, "range_t", CreateArray(data.Range_t), "Диапазон температуры");
                AddKeyValue(paramsTable, "adc_range_p", CreateArray(data.Adc_Range_p), "Диапазон АЦП давления");
                AddKeyValue(paramsTable, "adc_range_t", CreateArray(data.Adc_Range_t), "Диапазон АЦП температуры");

                AddKeyValue(paramsTable, "max_error_limit", new FloatValueSyntax(data.LimitMaxError), "Допуск макс. погрешности");
                AddKeyValue(paramsTable, "rms_error_limit", new FloatValueSyntax(data.LimitStandardError), "Допуск СКО");
                AddKeyValue(paramsTable, "max_error_pct_limit", new FloatValueSyntax(data.LimitMaxRelativeError), "Допуск макс. приведённой погрешности");
                AddKeyValue(paramsTable, "rms_error_pct_limit", new FloatValueSyntax(data.LimitStandardRelativeError), "Допуск приведённого СКО");

                doc.Tables.Add(paramsTable);
            }

            var resultTable = new TableSyntax("result");
            resultTable.AddLeadingTriviaNewLine();
            AddKeyValue(resultTable, "status", new StringValueSyntax(FormatStatus(false)));
            AddKeyValue(resultTable, "error_info", new StringValueSyntax(ex.Message));
            doc.Tables.Add(resultTable);

            string tomlText = doc.ToString();

            const string header =
                "#                ПРОТОКОЛ КАЛИБРОВКИ ДАВЛЕНИЯ С ОШИБКОЙ\n" +
                "#==============================================================================\n\n";

            File.WriteAllText(path, header + tomlText);
            Console.WriteLine(header + tomlText);
        }

        public static void Write(Identification id, PressureData data, PressureCalibrationResult result, string path)
        {
            var doc = new DocumentSyntax();

            // [identification]
            var idTable = new TableSyntax("identification");
            idTable.AddLeadingTriviaNewLine();
            AddKeyValue(idTable, "timestamp", new StringValueSyntax(id.TimeStamp.ToString("yyyy-MM-ddTHH:mm:ss")), "Время калибровки");
            AddKeyValue(idTable, "model", new StringValueSyntax(id.Model), "Модель прибора");
            AddKeyValue(idTable, "serial_number", new StringValueSyntax(id.SerialNumber), "Серийный номер");
            AddKeyValue(idTable, "software_version", new StringValueSyntax(id.SoftwareVersion), "Версия ПО");
            AddKeyValue(idTable, "stand_id", new StringValueSyntax(id.StandId), "Идентификатор стенда");
            AddKeyValue(idTable, "user", new StringValueSyntax(id.User), "Пользователь");
            doc.Tables.Add(idTable);

            // [model_parameters]
            var paramsTable = new TableSyntax("model_parameters");
            paramsTable.AddLeadingTriviaNewLine();
            AddKeyValue(paramsTable, "method", new StringValueSyntax("Direct QR"), "Метод калибровки");
            AddKeyValue(paramsTable, "unit_p", new StringValueSyntax(data.Unit_p), "Единица измерения давления");
            AddKeyValue(paramsTable, "unit_t", new StringValueSyntax(data.Unit_t), "Единица измерения температуры");
            AddKeyValue(paramsTable, "normalize", new StringValueSyntax(data.Normilize ? "true" : "false"), "Присутствует нормализация");
            AddKeyValue(paramsTable, "polynomial_degree_p", new IntegerValueSyntax(data.P_Pow), "Степень полинома по давлению");
            AddKeyValue(paramsTable, "polynomial_degree_t", new IntegerValueSyntax(data.T_Pow), "Степень полинома по температуре");
            AddKeyValue(paramsTable, "range_p", CreateArray(data.Range_p), "Диапазон давления");
            AddKeyValue(paramsTable, "range_t", CreateArray(data.Range_t), "Диапазон температуры");
            AddKeyValue(paramsTable, "adc_range_p", CreateArray(data.Adc_Range_p), "Диапазон АЦП давления");
            AddKeyValue(paramsTable, "adc_range_t", CreateArray(data.Adc_Range_t), "Диапазон АЦП температуры");
            AddKeyValue(paramsTable, "max_error_limit", new FloatValueSyntax(data.LimitMaxError), "Допуск макс. погрешности");
            AddKeyValue(paramsTable, "rms_error_limit", new FloatValueSyntax(data.LimitStandardError), "Допуск СКО");
            AddKeyValue(paramsTable, "max_error_pct_limit", new FloatValueSyntax(data.LimitMaxRelativeError), "Допуск макс. приведённой погрешности");
            AddKeyValue(paramsTable, "rms_error_pct_limit", new FloatValueSyntax(data.LimitStandardRelativeError), "Допуск приведённого СКО");
            doc.Tables.Add(paramsTable);

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

            // [metrology_calib_result]
            var calibTable = new TableSyntax("metrology_calib_result");
            calibTable.AddLeadingTriviaNewLine();
            AddKeyValue(calibTable, "status", new StringValueSyntax(FormatStatus(calibOk)));
            AddKeyValue(calibTable, "error_info", new StringValueSyntax(calibError));
            AddKeyValue(calibTable, "max_error", new FloatValueSyntax(result.FitReport.CalibrationMaxError), "Максимальная погрешность");
            AddKeyValue(calibTable, "rms_error", new FloatValueSyntax(result.FitReport.CalibrationStandardError), "СКО");
            AddKeyValue(calibTable, "max_error_pct", new FloatValueSyntax(result.FitReport.CalibrationMaxRelativeError * 100), "Максимальная приведённая погрешность, %");
            AddKeyValue(calibTable, "rms_error_pct", new FloatValueSyntax(result.FitReport.CalibrationRelativeStandardError * 100), "приведённое СКО, %");
            doc.Tables.Add(calibTable);

            // [metrology_test_result]
            var testTable = new TableSyntax("metrology_test_result");
            testTable.AddLeadingTriviaNewLine();
            AddKeyValue(testTable, "status", new StringValueSyntax(FormatStatus(testOk)));
            AddKeyValue(testTable, "error_info", new StringValueSyntax(testError));
            AddKeyValue(testTable, "max_error", new FloatValueSyntax(result.FitReport.TestMaxError), "Максимальная погрешность");
            AddKeyValue(testTable, "rms_error", new FloatValueSyntax(result.FitReport.TestStandardError), "СКО");
            AddKeyValue(testTable, "max_error_pct", new FloatValueSyntax(result.FitReport.TestMaxRelativeError * 100), "Максимальная приведённая погрешность, %");
            AddKeyValue(testTable, "rms_error_pct", new FloatValueSyntax(result.FitReport.TestRelativeStandardError * 100), "приведённое СКО, %");
            doc.Tables.Add(testTable);

            // [math_stability]
            var mathTable = new TableSyntax("math_stability");
            mathTable.AddLeadingTriviaNewLine();
            AddKeyValue(mathTable, "status", new StringValueSyntax(FormatStatus(mathOk)));
            AddKeyValue(mathTable, "error_info", new StringValueSyntax(mathError));
            AddKeyValue(mathTable, "check_grid", CreateArray([result.MonotonicityReport.Nu, result.MonotonicityReport.Nv]), "Сетка проверки");
            AddKeyValue(mathTable, "monotonicity_ok", new BooleanValueSyntax(result.MonotonicityReport.Verdict == MonotonicityVerdict.Ok), "Монотонность соблюдена");
            AddKeyValue(mathTable, "monotonicity_violations", new IntegerValueSyntax(result.MonotonicityReport.ViolationsCount), "Количество нарушений монотонности");
            AddKeyValue(mathTable, "min_derivative_g", new FloatValueSyntax(result.MonotonicityReport.MinAbsG), "Минимальная производная");
            AddKeyValue(mathTable, "mean_derivative_g", new FloatValueSyntax(result.MonotonicityReport.MeanAbsG), "Средняя производная");
            AddKeyValue(mathTable, "condition_number", new FloatValueSyntax(result.FitReport.ConditionNumber), "Число обусловленности");
            AddKeyValue(mathTable, "matrix_rank", new IntegerValueSyntax(result.FitReport.Rank), "Ранг матрицы");
            doc.Tables.Add(mathTable);

            // [polynomial]
            var polyTable = new TableSyntax("polynomial");
            polyTable.AddLeadingTriviaNewLine();
            AddKeyValue(polyTable, "number_of_coefficients", new IntegerValueSyntax(
                result.FitReport.Coefficients > 0 ? result.FitReport.Coefficients : result.Coefficients.Length), "Количество коэффициентов");
            AddKeyValue(polyTable, "formula", new StringValueSyntax(
                $"P(u,v) = sum a[i,j] * u^i * v^j,  i=0..{data.P_Pow}, j=0..{data.T_Pow}"), "Формула полинома");

            int rows = result.Coefficients.GetLength(0);
            int cols = result.Coefficients.GetLength(1);

            var coeffsArray = new ArraySyntax
            {
                OpenBracket = new SyntaxToken(TokenKind.OpenBracket, "[")
                {
                    TrailingTrivia = [new(TokenKind.NewLine, "\n")]
                },
                CloseBracket = new SyntaxToken(TokenKind.CloseBracket, "]")
            };

            for (int i = 0; i < rows; i++)
            {
                var rowArray = new ArraySyntax
                {
                    OpenBracket = new SyntaxToken(TokenKind.OpenBracket, "[")
                    {
                        LeadingTrivia = [new(TokenKind.Whitespaces, "    ")]
                    },
                    CloseBracket = new SyntaxToken(TokenKind.CloseBracket, "]")
                };

                for (int j = 0; j < cols; j++)
                {
                    var item = new ArrayItemSyntax
                    {
                        Value = new FloatValueSyntax(result.Coefficients[i, j])
                    };

                    if (j < cols - 1)
                    {
                        SyntaxToken syntaxToken = new(TokenKind.Comma, ",")
                        {
                            TrailingTrivia = [new(TokenKind.Whitespaces, " ")]
                        };
                        item.Comma = syntaxToken;
                    }

                    rowArray.Items.Add(item);
                }

                var rowItem = new ArrayItemSyntax { Value = rowArray };

                if (i < rows - 1)
                {
                    rowItem.Comma = new SyntaxToken(TokenKind.Comma, ",")
                    {
                        TrailingTrivia = [new(TokenKind.NewLine, "\n")]
                    };
                }
                else
                {
                    rowArray.CloseBracket.TrailingTrivia =
                    [
                        new(TokenKind.NewLine, "\n")
                    ];
                }

                coeffsArray.Items.Add(rowItem);
            }

            var coeffsKeyValue = new KeyValueSyntax("coefficients", coeffsArray);
            polyTable.Items.Add(coeffsKeyValue);
            doc.Tables.Add(polyTable);

            // [data]
            var dataTable = new TableSyntax("data");
            dataTable.AddLeadingTriviaNewLine();
            AddKeyValue(dataTable, "number_of_points", new IntegerValueSyntax(data.P_Adc.Length), "Общее количество точек");

            string rawText = CreateCsvContent(data, result);

            string escapedText = rawText.Replace("\"\"\"", "\\\"\"\"");

            var token = new SyntaxToken(TokenKind.StringMulti, $"\"\"\"\n{escapedText}\"\"\"");

            var csvString = new StringValueSyntax { Token = token };
            var csvItem = new KeyValueSyntax("measurements_csv", csvString);
            dataTable.Items.Add(csvItem);
            doc.Tables.Add(dataTable);

            string tomlText = doc.ToString();

            const string header =
                "#                ПРОТОКОЛ КАЛИБРОВКИ ДАВЛЕНИЯ\n" +
                "#==============================================================================\n\n";

            File.WriteAllText(path, header + tomlText);
            Console.WriteLine(header + tomlText);
        }

        private static string FormatStatus(bool ok) => ok ? "✅️ - PASS" : "❌️ - FAIL";

        private static void AddKeyValue(TableSyntax table, string key, ValueSyntax value, string? comment = null)
        {
            var item = new KeyValueSyntax(key, value);
            if (comment != null)
                item.AddTrailingComment(comment);
            table.Items.Add(item);
        }

        private static ArraySyntax CreateArray<T>(T[] values) where T : notnull
        {
            var array = new ArraySyntax
            {
                OpenBracket = new SyntaxToken(TokenKind.OpenBracket, "["),
                CloseBracket = new SyntaxToken(TokenKind.CloseBracket, "]")
            };

            for (int i = 0; i < values.Length; i++)
            {
                ValueSyntax valueSyntax = values[i] switch
                {
                    double d => new FloatValueSyntax(d),
                    int i2 => new IntegerValueSyntax(i2),
                    long l => new IntegerValueSyntax(l),
                    string s => new StringValueSyntax(s),
                    bool b => new BooleanValueSyntax(b),
                    _ => throw new NotSupportedException($"Тип {typeof(T)} не поддерживается")
                };

                var item = new ArrayItemSyntax { Value = valueSyntax };

                if (i < values.Length - 1)
                {
                    item.Comma = new SyntaxToken(TokenKind.Comma, ",")
                    {
                        TrailingTrivia =
                        [
                            new(TokenKind.Whitespaces, " ")
                        ]
                    };
                }

                array.Items.Add(item);
            }

            return array;
        }

        private static string CreateCsvContent(PressureData data, PressureCalibrationResult result)
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
            lines.Add("");
            return string.Join("\n", lines);
        }

        #endregion
    }
}