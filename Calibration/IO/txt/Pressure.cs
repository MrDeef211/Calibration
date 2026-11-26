using Core.Model.Pressure;
using Core.Model.Reports;
using System.Text;
using System.Text.RegularExpressions;
/*
namespace Calibration.IO.txt
{
    internal static class Pressure
    {
        #region Чтение

        internal static PressureData ReadFile(string path)
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

            double[] p_adc = new double[lineCount - 2];
            double[] t_adc = new double[lineCount - 2];
            double[] p_ref = new double[lineCount - 2];
            int[] indexes = new int[lineCount - 2];
            int calibratedPoints = 0;

            using StreamReader stream = new(path);

            string ReadNext() => (stream.ReadLine()
                    ?? throw new Exception("Пустой файл или нет данных"))
                .Replace(',', '.').Trim();

            if (!int.TryParse(ReadNext()[5..], out int p_pow)
                || !int.TryParse(ReadNext()[5..], out int t_pow))
                throw new Exception("Ошибка при чтении показателя степени полинома");

            if (!double.TryParse(ReadNext()[8..], out double p_AdcMin)
                || !double.TryParse(ReadNext()[8..], out double p_AdcMax)
                || !double.TryParse(ReadNext()[8..], out double t_AdcMin)
                || !double.TryParse(ReadNext()[8..], out double t_AdcMax)
                || !double.TryParse(ReadNext()[5..], out double p_Min)
                || !double.TryParse(ReadNext()[5..], out double p_Max)
                || !double.TryParse(ReadNext()[5..], out double t_Min)
                || !double.TryParse(ReadNext()[5..], out double t_max))
                throw new Exception("Ошибка при чтении диапазонов работы");


            string head = "";
            // Ищу заголовки
            while (!head.StartsWith("pADC"))
            {
                head = stream.ReadLine() ?? "";
            }

            int index = 0;
            while (index < lineCount - 2)
            {
                string? line = stream.ReadLine();
                if (line == null || line == "") break;

                string[] lines = Regex.Split(line.Replace(',', '.'), @"[\s;]+");
                if (lines.Length < 3)
                    throw new Exception(
                        "Неверный формат файла: неправильный или отсутствующий " +
                        "разделитель или неправильное число столбцов");

                if (double.TryParse(lines[0], out double p1))
                    p_adc[index] = p1;
                else
                    break;

                if (double.TryParse(lines[1], out double t))
                    t_adc[index] = t;

                if (double.TryParse(lines[2], out double p2))
                    p_ref[index] = p2;

                if (lines.Length > 3 && lines[3] == "*")
                {
                    indexes[calibratedPoints] = index;
                    calibratedPoints++;
                }

                index++;
            }

            if (index < lineCount - 2)
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

            return new PressureData(
                true,
                p_pow,
                t_pow,
                indexes,
                p_adc,
                t_adc,
                p_ref,
                p_AdcMin,
                p_AdcMax,
                t_AdcMin,
                t_AdcMax,
                p_Min,
                p_Max,
                t_Min,
                t_max);
        }

        #endregion

        #region Запись

        internal static void Print(Exception ex, string path)
        {
            using StreamWriter stream = new(path, false);
            Console.WriteLine(ex.Message);
            stream.WriteLine(ex.Message);

            stream.Flush();
        }

        internal static void PrintMonotonicityReport(PressureMonotonicityReport report, StreamWriter stream)
        {
            string separator = new('-', 52);
            string header = $"--------------- Отчет о монотонности ---------------";
            string verdict = $"Вердикт:                        {report.Verdict}";
            string sign = $"Знак производной:               {(report.Sign > 0 ? "+" : "-")}";
            string minAbsG = $"Мин. абс. G (Мин. производная): {report.MinAbsG:F6}";
            string uAtMin = $"U при мин. G:                   {report.UAtMin:F4}";
            string vAtMin = $"V при мин. G:                   {report.VAtMin:F4}";
            string delta = $"Дельта G:                       {report.Delta:E4}";
            string meanAbsG = $"Средний абс. G:                 {report.MeanAbsG:F6}";
            string grid = $"Сетка (Nu x Nv):                {report.Nu} x {report.Nv}";
            string violations = $"Кол-во нарушений:               {report.ViolationsCount}";
            string uFirstViol = $"U первого нарушения:            {report.UAtFirstViolation:F4}";
            string vFirstViol = $"V первого нарушения:            {report.VAtFirstViolation:F4}";

            Console.WriteLine();
            Console.WriteLine(header);
            Console.WriteLine(verdict);
            Console.WriteLine(sign);
            Console.WriteLine(minAbsG);
            Console.WriteLine(uAtMin);
            Console.WriteLine(vAtMin);
            Console.WriteLine(delta);
            Console.WriteLine(meanAbsG);
            Console.WriteLine(grid);
            Console.WriteLine(violations);
            Console.WriteLine(uFirstViol);
            Console.WriteLine(vFirstViol);
            Console.WriteLine(separator);
            Console.WriteLine();

            stream.WriteLine();
            stream.WriteLine(header);
            stream.WriteLine(verdict);
            stream.WriteLine(sign);
            stream.WriteLine(minAbsG);
            stream.WriteLine(uAtMin);
            stream.WriteLine(vAtMin);
            stream.WriteLine(delta);
            stream.WriteLine(meanAbsG);
            stream.WriteLine(grid);
            stream.WriteLine(violations);
            stream.WriteLine(uFirstViol);
            stream.WriteLine(vFirstViol);
            stream.WriteLine(separator);
            stream.WriteLine();

            stream.Flush();
        }

        internal static void PrintFitReport(FitReport report, StreamWriter stream)
        {
            string separator = new('-', 52);
            string header = $"-------------- Отчет об аппроксимации --------------";
            string cond = $"Число обусловленности:              {report.ConditionNumber:F2} (Норма от 1 до 100000)";
            string rank = $"Ранг матрицы:                       {report.Rank} (Должен быть не меньше чем число коэффициентов (pow_t+1)*(pow_p+1))";
            string points = $"Количество калибровочных точек:     {report.CalibrationPoints}";
            string coef = $"Количество коэффициентов:           {report.Coefficients}";
            string deviations = $"Среднеквадратичная погрешность:     {report.TestStandardError:f5} ({report.TestRelativeStandardError:p5}) (Чем меньше тем лучше)";
            string maxDeviations = $"Максимальная погрешность:          {report.TestMaxError:f5} ({report.TestMaxRelativeError:p5}) (Среди заданных точек, чем меньше тем лучше)";
            string confidenceInterval = $"Доверительный интервал (P = 99.7%): +- {(report.TestStandardError * 3):f5} (Максимальная погрешность с вероятностью P)";

            Console.WriteLine();
            Console.WriteLine(header);
            Console.WriteLine(cond);
            Console.WriteLine(rank);
            Console.WriteLine(points);
            Console.WriteLine(coef);
            Console.WriteLine(deviations);
            Console.WriteLine(maxDeviations);
            Console.WriteLine(confidenceInterval);
            Console.WriteLine(separator);
            Console.WriteLine();

            stream.WriteLine();
            stream.WriteLine(header);
            stream.WriteLine(cond);
            stream.WriteLine(rank);
            stream.WriteLine(points);
            stream.WriteLine(coef);
            stream.WriteLine(deviations);
            stream.WriteLine(maxDeviations);
            stream.WriteLine(confidenceInterval);
            stream.WriteLine(separator);
            stream.WriteLine();

            stream.Flush();
        }

        internal static void PrintSummaryReport(
            PressureMonotonicityReport monoReport,
            FitReport fitReport, StreamWriter stream)
        {
            string separator = new('=', 65);
            string header = "============== ИТОГОВЫЙ СТАТУС КАЛИБРОВКИ ДАВЛЕНИЯ ==============";

            string status;
            string recommendation;

            if (fitReport.CalibrationPoints < fitReport.Coefficients || fitReport.Rank < fitReport.Coefficients)
            {
                status = "КРИТИЧЕСКАЯ ОШИБКА: Ошибка математической модели.";
                recommendation = "НЕОБХОДИМО ПЕРЕКАЛИБРОВАТЬ. Слишком мало точек измерения или они " +
                    "расположены на одной линии/плоскости. Добавьте новые разнообразные точки измерений.";
            }
            else if (monoReport.Verdict == MonotonicityVerdict.Violated)
            {
                status = "ВНИМАНИЕ: Нарушена логика физического процесса (монотонность).";
                recommendation = "НЕОБХОДИМО ПЕРЕКАЛИБРОВАТЬ. Точки измерения распределены " +
                    "неравномерно или содержат сильные шумы (ошибки). Проверьте датчики и " +
                    "повторите замеры в других диапазонах.";
            }
            else if (fitReport.ConditionNumber > 1e6 || monoReport.Verdict == MonotonicityVerdict.WeakMargin)
            {
                status = "ПРЕДУПРЕЖДЕНИЕ: Нестабильная калибровка (плохие данные).";
                recommendation = "РЕКОМЕНДУЕТСЯ ПЕРЕКАЛИБРОВАТЬ. Точки измерения распределены " +
                    "неравномерно или содержат сильные шумы (ошибки). Проверьте датчики и " +
                    "повторите замеры в других диапазонах.";
            }
            else if (fitReport.TestRelativeStandardError > 0.0005 || fitReport.TestMaxRelativeError > 0.0015)
            {
                status = "ПРЕДУПРЕЖДЕНИЕ: Нестабильная калибровка (высокая погрешность).";
                recommendation = "РЕКОМЕНДУЕТСЯ ПЕРЕКАЛИБРОВАТЬ. Точки измерения распределены " +
                    "неравномерно или содержат сильные шумы (ошибки), либо точек недостаточно." +
                    " Проверьте датчики и повторите замеры в других диапазонах, либо увеличьте степень полинома.";
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

        internal static void PrintRanges(PressureData data, StreamWriter stream)
        {
            string line;

            stream.WriteLine("");
            Console.WriteLine("");

            line = $"Диапазон нормирования АЦП Давления: {data.P_AdcMin} - " +
                $"{data.P_AdcMax}, для значений давления: ({data.P_Min} - {data.P_Max})";

            stream.WriteLine(line);
            Console.WriteLine(line);

            line = $"Диапазон нормирования АЦП Температуры: {data.T_AdcMin} - " +
                $"{data.T_AdcMax}, для значений  температуры: ({data.T_Min} - {data.T_Max})";

            stream.WriteLine(line);
            Console.WriteLine(line);

            stream.WriteLine("");
            Console.WriteLine("");

            stream.Flush();
        }

        internal static void PrintCoefficients(double[,] coeffs, StreamWriter stream)
        {
            string line;

            int n = coeffs.GetLength(0) - 1;
            int m = coeffs.GetLength(1) - 1;

            const int w = 24;

            stream.WriteLine();
            Console.WriteLine();

            line = $"Коэффициенты: P(u,v) = sum a[i,j] * u^i * v^j,  i=0..{n}, j=0..{m}";
            stream.WriteLine(line);
            Console.WriteLine(line);

            stream.WriteLine();
            Console.WriteLine();

            line = "Строки — степень по u (АЦП Давления), столбцы — степень по v (АЦП Температуры)";
            stream.WriteLine(line);
            Console.WriteLine(line);

            stream.WriteLine();
            Console.WriteLine();

            var sb = new StringBuilder();
            for (int j = 0; j <= m; j++)
                sb.Append($"v^{j}".PadLeft(w));

            line = sb.ToString();
            stream.WriteLine(line);
            Console.WriteLine(line);

            line = new string('-', w * (m + 2));
            stream.WriteLine(line);
            Console.WriteLine(line);

            for (int i = n; i >= 0; i--)
            {
                sb.Clear();
                sb.Append($"u^{i}".PadRight(4));

                for (int j = 0; j <= m; j++)
                {
                    double c = coeffs[i, j];
                    string s;

                    if (c == 0.0)
                        s = "·";
                    else
                        s = $"{c}\t";

                    sb.Append(s.PadLeft(w));
                }

                line = sb.ToString();
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
            PressureData data,
            double[] p,
            double[] dp,
            double[] ep)
        {
            // Заголовок
            int @char = 120 - head.Length;

            string headDecor = new('=', @char / 2);

            string line = $"{headDecor}|{head}|{headDecor}";

            stream.WriteLine(line);
            Console.WriteLine(line);

            // Заголовки
            line = $"{"АЦП_Давления",15} {"АЦП_Температуры",16} {"Эталон",10}" +
                $" {"Результат",12} {"Абс._Погрешность",18} {"Прив._Погрешность_%",16} {"Калибр.",5}";

            stream.WriteLine(line);
            Console.WriteLine(line);


            int k = 0;
            bool calibr;
            // Значения
            for (int i = 0; i < p.Length; i++)
            {
                calibr = data.CalibrationIndexes.Length > k && data.CalibrationIndexes[k] == i;
                if (calibr)
                    k++;

                line = $"{data.P_Adc[i],12:f3} {data.T_Adc[i],15:f3}" +
                    $" {data.P_Values[i],14:f3} {p[i],12:f3} {dp[i],13:f3} {ep[i]*100,16:f3} {(calibr ? "*" : ""),10}";

                stream.WriteLine(line);
                Console.WriteLine(line);

                stream.Flush();
            }

        }

        #endregion
    }

}
*/
