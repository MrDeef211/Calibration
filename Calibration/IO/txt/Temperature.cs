using Core.Model.Reports;
using Core.Model.Temperature;
using System.Text.RegularExpressions;

namespace Calibration.IO.txt
{
    internal static class Temperature
    {
        #region Чтение

        internal static TemperatureData ReadFile(string path)
        {
            if (path == null || !File.Exists(path))
                throw new Exception("Отсутствует файл с данными");

            int lineCount = 0;
            using (var reader = new StreamReader(path))
            {
                while (reader.ReadLine() != null) lineCount++;
            }

            if (lineCount <= 2) throw new Exception(
                        "Пустой файл или нет данных");

            double[] adc = new double[lineCount - 2];
            double[] @ref = new double[lineCount - 2];
            int[] indexes = new int[lineCount - 2];
            int calibratedPoints = 0;

            using StreamReader stream = new(path);

            string ReadNext() =>
                (stream.ReadLine() ?? throw new Exception("Пустой файл или нет данных"))
                    .Replace(',', '.').Trim();

            string? powString = stream.ReadLine()?[4..];

            if (powString == null || !int.TryParse(powString.Trim(), out int pow))
                throw new Exception("Ошибка при чтении показателя степени полинома");

            if (!double.TryParse(ReadNext()[8..], out double t_AdcMin)
                || !double.TryParse(ReadNext()[8..], out double t_AdcMax)
                || !double.TryParse(ReadNext()[5..], out double t_Min)
                || !double.TryParse(ReadNext()[5..], out double t_max))
                throw new Exception("Ошибка при чтении диапазонов работы");

            string head = "";
            // Ищу заголовки
            while (!head.StartsWith("tADC"))
            {
                head = stream.ReadLine() ?? "";
            }

            int index = 0;
            while (index < lineCount - 2)
            {
                string? line = stream.ReadLine();
                if (line == null || line == "") break;

                string[] lines = Regex.Split(line.Replace(',', '.'), @"[\s;]+");
                if (lines.Length < 2)
                    throw new Exception(
                        "Неверный формат файла: неправильный или отсутствующий " +
                        "разделитель или неправильное число столбцов");

                if (double.TryParse(lines[0], out double t1))
                    adc[index] = t1;
                else
                    break;

                if (double.TryParse(lines[1], out double t2))
                    @ref[index] = t2;

                if (lines.Length > 2 && lines[2] == "*")
                {
                    indexes[calibratedPoints] = index;
                    calibratedPoints++;
                }

                index++;
            }

            if (index < lineCount - 2)
            {
                Array.Resize(ref adc, index);
                Array.Resize(ref @ref, index);
            }

            if (calibratedPoints == 0)
                for (calibratedPoints = 0; calibratedPoints < adc.Length; calibratedPoints++)
                    indexes[calibratedPoints] = calibratedPoints;

            if (calibratedPoints < indexes.Length)
                Array.Resize(ref indexes, calibratedPoints);

            return new TemperatureData(true, pow, indexes, adc, @ref, t_AdcMin, t_AdcMax, t_Min, t_max);
        }

        #endregion

        #region Запись

        internal static void PrintDeviations(TemperatureCalibrationResult result, StreamWriter stream)
        {
            string deviation = $"Среднеквадратичная погрешность = {result.StandardDeviation:f5} ({result.NormilizedStandardDeviation:p5}) (Чем меньше тем лучше)";
            string maxDeviation = $"Наибольшая погрешность = {result.DT.Max(d => double.IsFinite(d) ? Math.Abs(d) : 0)}" +
                $" ({result.ET.Max(d => double.IsFinite(d) ? Math.Abs(d) : 0):p5}) (Среди заданных точек, чем меньше тем лучше)";
            string confidenceInterval = $"Доверительный интервал (P = 99.7%) = +- {(result.StandardDeviation * 3):f5} ({(result.NormilizedStandardDeviation * 3):p5}) (Максимальная погрешность с вероятностью P)";

            Console.WriteLine(deviation);
            Console.WriteLine(maxDeviation);
            Console.WriteLine(confidenceInterval);
            Console.WriteLine();

            stream.WriteLine(deviation);
            stream.WriteLine(maxDeviation);
            stream.WriteLine(confidenceInterval);
            stream.WriteLine();

            stream.Flush();
        }

        internal static void Print(string text, string path)
        {
            Console.WriteLine(text);
            Console.WriteLine();

            using StreamWriter stream = new(path, true);
            stream.WriteLine(text);
            stream.WriteLine();

            stream.Flush();
        }

        internal static void PrintReport(TemperatureMonotonicityReport report, StreamWriter stream)
        {
            string separator = new('-', 64);
            string header = "------ Отчет о монотонности (температура с нормированием) ------";
            string verdict = $"Вердикт:                        {report.Verdict}";
            string sign = $"Знак производной:               {(report.Sign > 0 ? "+" : "-")}";
            string minAbsG = $"Мин. абс. G (Мин. производная): {report.MinAbsG:F6}";
            string tAtMin = $"T при мин. G:                   {report.TAtMin:F4}";
            string delta = $"Дельта G:                       {report.Delta:E4}";
            string meanAbsG = $"Средний абс. G:                 {report.MeanAbsG:F6}";
            string grid = $"Сетка:                          {report.Nt}";
            string violations = $"Кол-во нарушений:               {report.ViolationsCount}";
            string tFirstViol = $"T первого нарушения:            {report.TAtFirstViolation:F4}";

            Console.WriteLine();
            Console.WriteLine(header);
            Console.WriteLine(verdict);
            Console.WriteLine(sign);
            Console.WriteLine(minAbsG);
            Console.WriteLine(tAtMin);
            Console.WriteLine(delta);
            Console.WriteLine(meanAbsG);
            Console.WriteLine(grid);
            Console.WriteLine(violations);
            Console.WriteLine(tFirstViol);
            Console.WriteLine(separator);
            Console.WriteLine();

            stream.WriteLine();
            stream.WriteLine(header);
            stream.WriteLine(verdict);
            stream.WriteLine(sign);
            stream.WriteLine(minAbsG);
            stream.WriteLine(tAtMin);
            stream.WriteLine(delta);
            stream.WriteLine(meanAbsG);
            stream.WriteLine(grid);
            stream.WriteLine(violations);
            stream.WriteLine(tFirstViol);
            stream.WriteLine(separator);
            stream.WriteLine();

            stream.Flush();
        }

        internal static void PrintSummaryReport(
            TemperatureMonotonicityReport monoReport,
            TemperatureCalibrationResult result, StreamWriter stream)
        {
            string separator = new('=', 66);
            string header = "============= ИТОГОВЫЙ СТАТУС КАЛИБРОВКИ ТЕМПЕРАТУРЫ =============";

            string status;
            string recommendation;

            if (monoReport.Verdict == MonotonicityVerdict.Violated)
            {
                status = "ВНИМАНИЕ: Нарушена логика физического процесса (монотонность).";
                recommendation = "НЕОБХОДИМО ПЕРЕКАЛИБРОВАТЬ. Точки измерения распределены " +
                    "неравномерно или содержат сильные шумы (ошибки). Проверьте датчики и " +
                    "повторите замеры в других диапазонах.";
            }
            else if (monoReport.Verdict == MonotonicityVerdict.WeakMargin)
            {
                status = "ПРЕДУПРЕЖДЕНИЕ: Нестабильная калибровка (плохие данные).";
                recommendation = "РЕКОМЕНДУЕТСЯ ПЕРЕКАЛИБРОВАТЬ. Точки измерения распределены " +
                    "неравномерно или содержат сильные шумы (ошибки). Проверьте датчики и " +
                    "повторите замеры в других диапазонах.";
            }
            else if (result.NormilizedStandardDeviation > 0.0005 || result.ET.Max(d => double.IsFinite(d) ? Math.Abs(d) : 0) > 0.0015)
            {
                status = "ПРЕДУПРЕЖДЕНИЕ: Нестабильная калибровка (высокая погрешность).";
                recommendation = "РЕКОМЕНДУЕТСЯ ПЕРЕКАЛИБРОВАТЬ. Точки измерения распределены " +
                    "неравномерно, содержат сильные шумы (ошибки), либо точек недостаточно. " +
                    "Проверьте датчики и повторите замеры в других диапазонах, либо увеличьте степень полинома.";
            }
            else
            {
                status = "УСПЕХ: Калибровка завершена успешно!";
                recommendation = "Всё отлично, никаких действий не требуется. Датчик готов к работе.";
            }

            string[] lines =
            [
                "",
                header,
                $"Статус: {status}",
                recommendation,
                separator,
                ""
            ];

            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }

            foreach (var line in lines)
            {
                stream.WriteLine(line);
            }

            stream.Flush();
        }

        internal static void Print(Exception ex, string path, bool append = false)
        {
            using StreamWriter stream = new(path, append);
            Console.WriteLine(ex.Message);
            stream.WriteLine(ex.Message);

            stream.Flush();
        }

        internal static void PrintRanges(TemperatureData data, StreamWriter stream)
        {
            string line;

            stream.WriteLine("");
            Console.WriteLine("");

            line = $"Диапазон нормирования АЦП: {data.T_AdcMin} - " +
                $"{data.T_AdcMax}, для значений температуры: ({data.T_Min} - {data.T_Max})";

            stream.WriteLine(line);
            Console.WriteLine(line);

            stream.WriteLine("");
            Console.WriteLine("");

            stream.Flush();
        }

        internal static void PrintCoefficients(double[] coeffs, StreamWriter stream)
        {
            stream.WriteLine("");
            Console.WriteLine("");

            stream.Write("Коэффициенты = ");
            Console.Write("Коэффициенты = ");

            string line;

            for (int i = coeffs.Length - 1; i >= 0; i--)
            {
                if (Math.Abs(coeffs[i]) >= 0.001 && Math.Abs(coeffs[i]) < 1)
                    line = (i != 0)
                        ? $"{coeffs[i]:f3}x^{i} + "
                        : $"{coeffs[i]:f3}";
                else if (Math.Abs(coeffs[i]) >= 1)
                    line = (i != 0)
                        ? $"{coeffs[i]:f2}x^{i} + "
                        : $"{coeffs[i]:f2}";
                else
                    line = (i != 0)
                        ? $"{coeffs[i]:e2}x^{i} + "
                        : $"{coeffs[i]:e2}";

                stream.Write(line);
                Console.Write(line);
            }

            stream.WriteLine("\n");
            Console.WriteLine("\n");

            stream.WriteLine("В полном виде:");
            Console.WriteLine($"В полном виде:");

            for (int i = coeffs.Length - 1; i >= 0; i--)
            {
                line = $"{coeffs[i]}";
                stream.WriteLine(line);
                Console.WriteLine(line);
            }

            stream.WriteLine("");
            Console.WriteLine("");

            stream.Flush();
        }

        internal static void WriteFile(
            StreamWriter stream,
            string head,
            TemperatureData data,
            double[] t,
            double[] dt,
            double[] et)
        {
            // Заголовок
            int c = 105 - head.Length;

            string headDecor = new('=', c / 2);

            string line = $"{headDecor}|{head}|{headDecor}";

            stream.WriteLine(line);
            Console.WriteLine(line);

            // Заголовки
            line = $"{"АЦП",8} {"Эталон",18} {"Результат",16} {"Абс._Погрешность",19} {"Прив._Погрешность_%",18} {"Калибр.",5}";

            stream.WriteLine(line);
            Console.WriteLine(line);

            int j = 0;
            bool calibr;
            // Значения
            for (int i = 0; i < t.Length; i++)
            {
                calibr = data.CalibrationIndexes.Length > j && data.CalibrationIndexes[j] == i;
                if (calibr)
                    j++;

                line = $"{data.T_Adc[i],11:f3} {data.T_Values[i],15:f3} {t[i],15:f3} {dt[i],15:f3} {et[i]*100,18:f3} {(calibr ? "*" : ""),10}";

                stream.WriteLine(line);
                Console.WriteLine(line);

                stream.Flush();
            }
        }

        #endregion
    }
}
