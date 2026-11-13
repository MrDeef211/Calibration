using System.Text;

namespace Calibration
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string dataFileName =
                args.Length > 0
                && (args[0].Contains(".txt")
                || args[0].Contains(".svg"))
                ? args[0] : "Pressure.txt";

            string fileNameOnly = dataFileName.Substring(0, dataFileName.LastIndexOf('.'));
            string resultFileName = args.Length > 1
                && (args[1].Contains(".txt")
                || args[1].Contains(".svg"))
                ? args[1] : fileNameOnly + "Result.txt";

            int t_pow, p_pow;
            int[] t_calibrationIndexes;
            double[] p_ads, t_ads, p_ref;

            try
            {
                (p_pow, t_pow, t_calibrationIndexes, p_ads, t_ads, p_ref) = ReadFile(dataFileName);
            }
            catch (Exception ex)
            {
                Print(ex, resultFileName);
                return;
            }

            double[] calibrationt_p_ads = new double[t_calibrationIndexes.Length];
            double[] calibrationt_t_ads = new double[t_calibrationIndexes.Length];
            double[] calibrationt_ref = new double[t_calibrationIndexes.Length];

            for (int i = 0; i < t_calibrationIndexes.Length; i++)
            {
                calibrationt_p_ads[i] = p_ads[t_calibrationIndexes[i]];
                calibrationt_t_ads[i] = t_ads[t_calibrationIndexes[i]];
                calibrationt_ref[i] = p_ref[t_calibrationIndexes[i]];
            }

            double[] p = [];
            double[] dp = [];

            #region Прямой 

            double p_min = p_ads.Min();
            double p_max = p_ads.Max();

            double t_min = t_ads.Min();
            double t_max = t_ads.Max();

            double[,] coefficients = new double[0, 0];

            PointsNormilizer p_normilizer;
            PointsNormilizer t_normilizer;

            try
            {

                p_normilizer = new(p_min, p_max, 0.1);
                t_normilizer = new(t_min, t_max, 0.1);

                coefficients = MathTool.Polyfit2(
                p_pow,
                t_pow,
                p_normilizer.Normilize(calibrationt_p_ads),
                t_normilizer.Normilize(calibrationt_t_ads),
                calibrationt_ref);

                p = MathTool.Calculate(
                    coefficients,
                    p_normilizer.Normilize(p_ads),
                    t_normilizer.Normilize(t_ads));

                dp = MathTool.Difference(p, p_ref);
            }
            catch (Exception e)
            {
                Print(e, resultFileName);
                return;
            }

            WriteFile(
                resultFileName,
                "Прямой QR метод",
                coefficients,
                p_ads, t_ads,
                p_normilizer.Normilize(p_ads),
                p_normilizer,
                t_normilizer.Normilize(t_ads),
                t_normilizer,
                p_ref, p, dp,
                MathTool.PointwiseQuotient(dp, p_ref)
            );

            PressureMonotonicityReport monotonicityReport = new(coefficients);

            FitReport fitReport = new(
                p_normilizer.Normilize(calibrationt_p_ads),
                t_normilizer.Normilize(calibrationt_t_ads),
                calibrationt_ref,
                dp,
                coefficients
            );

            Print(monotonicityReport, resultFileName);

            Print(fitReport, resultFileName);

            PrintSummaryReport(monotonicityReport, fitReport, resultFileName);

            #endregion
        }

        #region Чтение

        static (int p_pow, int t_pow, int[] calibrationIndexes, double[] p_ads, double[] t_ads, double[] refer) ReadFile(string path)
        {
            if (path == null || !File.Exists(path))
                throw new ArgumentNullException("Отсутствует файл с данными");

            int lineCount = 0;
            using (var reader = new StreamReader(path))
            {
                while (reader.ReadLine() != null) lineCount++;
            }

            if (lineCount <= 2) throw new Exception(
                        "Пустой файл или нет данных");

            double[] p_ads = new double[lineCount - 2];
            double[] t_ads = new double[lineCount - 2];
            double[] @ref = new double[lineCount - 2];
            int[] indexes = new int[lineCount - 2];
            int calibratedPoints = 0;
            int p_pow = 1;
            int t_pow = 1;

            using StreamReader stream = new(path);

            string? powString = stream.ReadLine();

            if (powString == null)
                new Exception(
                        "Пустой файл или нет данных");

            string[] powStrings = powString.Split(';');

            if (!int.TryParse(powStrings[0][5..], out p_pow) || !int.TryParse(powStrings[1][5..], out t_pow))
                throw new Exception(
                        "Ошибка при чтении показателя степени полинома");

            // Пропускаю заголовки
            stream.ReadLine();

            int index = 0;
            while (index < lineCount - 2)
            {
                string? line = stream.ReadLine();
                if (line == null) break;

                string[] lines = line.Split(';');
                if (lines.Length < 3)
                    lines = lines[0].Split(" ");
                if (lines.Length < 3)
                    lines = lines[0].Split("\t");
                if (lines.Length < 3)
                    throw new Exception(
                        "Неверный формат файла: неправильный или отсутствующий " +
                        "разделитель или неправильное число столбцов");

                if (double.TryParse(lines[0].Replace(',', '.'), out double p1))
                    p_ads[index] = p1;
                else
                    break;

                if (double.TryParse(lines[1].Replace(',', '.'), out double t))
                    t_ads[index] = t;
                else
                    break;

                if (double.TryParse(lines[2].Replace(',', '.'), out double p2))
                    @ref[index] = p2;

                if (lines.Length > 3 && lines[3] == "*")
                {
                    indexes[calibratedPoints] = index;
                    calibratedPoints++;
                }

                index++;
            }

            if (index < lineCount - 2)
            {
                Array.Resize(ref p_ads, index);
                Array.Resize(ref t_ads, index);
                Array.Resize(ref @ref, index);
            }

            if (calibratedPoints == 0)
                for (calibratedPoints = 0; calibratedPoints < p_ads.Length; calibratedPoints++)
                    indexes[calibratedPoints] = calibratedPoints;

            if (calibratedPoints < p_ads.Length)
                Array.Resize(ref indexes, calibratedPoints);

            return (p_pow, t_pow, indexes, p_ads, t_ads, @ref);
        }

        #endregion

        #region Запись

        static void Print(Exception ex, string path)
        {
            using StreamWriter stream = new(path, false);
            Console.WriteLine(ex.Message);
            stream.WriteLine(ex.Message);
        }

        static void Print(PressureMonotonicityReport report, string path)
        {
            string separator = new string('-', 52);
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

            using StreamWriter stream = new(path, true);
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
        }

        static void Print(FitReport report, string path)
        {
            string separator = new string('-', 52);
            string header = $"-------------- Отчет об аппроксимации --------------";
            string cond = $"Число обусловленности:                    {report.ConditionNumber:F2}";
            string rank = $"Ранг матрицы:                             {report.Rank}";
            string points = $"Количество калибровочных точек:           {report.Points}";
            string coef = $"Количество коэффициентов:                 {report.Coefficients}";
            string deviations = $"Среднеквадратичное отклонение от эталона: {report.StandardDeviation}";

            Console.WriteLine();
            Console.WriteLine(header);
            Console.WriteLine(cond);
            Console.WriteLine(rank);
            Console.WriteLine(points);
            Console.WriteLine(coef);
            Console.WriteLine(deviations);
            Console.WriteLine(separator);
            Console.WriteLine();

            using StreamWriter stream = new(path, true);
            stream.WriteLine();
            stream.WriteLine(header);
            stream.WriteLine(cond);
            stream.WriteLine(rank);
            stream.WriteLine(points);
            stream.WriteLine(coef);
            stream.WriteLine(deviations);
            stream.WriteLine(separator);
            stream.WriteLine();
        }

        static void PrintSummaryReport(
            PressureMonotonicityReport monoReport,
            FitReport fitReport, string path)
        {
            string separator = new string('=', 64);
            string header = "================== ИТОГОВЫЙ СТАТУС КАЛИБРОВКИ ==================";

            string status = "";
            string recommendation = "";

            if (fitReport.Points < fitReport.Coefficients || fitReport.Rank < fitReport.Coefficients)
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
            else if (fitReport.StandardDeviation > 0.1)
            {
                status = "ПРЕДУПРЕЖДЕНИЕ: Нестабильная калибровка (высокая погрешность).";
                recommendation = "РЕКОМЕНДУЕТСЯ ПЕРЕКАЛИБРОВАТЬ. Точки измерения распределены " +
                    "неравномерно или содержат сильные шумы (ошибки), либо точек недостаточно." +
                    " Проверьте датчики и повторите замеры в других диапазонах.";
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

            using StreamWriter stream = new(path, true);
            foreach (var line in lines)
            {
                stream.WriteLine(line);
            }
        }

        static void WriteFile(
            string path,
            string head,
            double[,] coefs,
            double[] p_ads,
            double[] t_ads,
            double[] p_normilized,
            PointsNormilizer p_normilizer,
            double[] t_normilized,
            PointsNormilizer t_normilizer,
            double[] t_ref,
            double[] p,
            double[] dp,
            double[] ep,
            bool append = false)
        {
            using StreamWriter stream = new(path, append);

            // Заголовок
            int @char = 110 - head.Length;

            string headDecor = new string('=', @char / 2);

            string line = $"{headDecor}|{head}|{headDecor}";

            stream.WriteLine(line);
            Console.WriteLine(line);

            // Заголовки
            line = $"{"АЦП 1",10} {"АЦП 2",11} {"Норм. 1",10} {"Норм. 2",8} {"Эталон",12}" +
                $" {"Результат",16} {"Абс. Погрешность",19} {"Отн. Погрешность",16}";

            stream.WriteLine(line);
            Console.WriteLine(line);

            // Значения
            for (int i = 0; i < p.Length; i++)
            {
                line = !double.IsInfinity(ep[i])
                    ? $"{p_ads[i],11:f3} {t_ads[i],11:f3} {p_normilized[i],8:f3} {t_normilized[i],8:f3}" +
                    $" {t_ref[i],15:f3} {p[i],15:f3} {dp[i],15:f3} {ep[i],15:p2}"
                    : $"{p_ads[i],11:f3} {t_ads[i],11:f3} {p_normilized[i],8:f3} {t_normilized[i],8:f3}" +
                    $" {t_ref[i],15:f3} {p[i],15:f3} {dp[i],15:f3} {"деление на 0",15}";

                stream.WriteLine(line);
                Console.WriteLine(line);
            }
            // Диапазон
            stream.WriteLine("");
            Console.WriteLine("");

            line = $"Диапазон нормирования АЦП 1: ({p_normilizer.Min} - " +
                $"{p_normilizer.Max}) +- {p_normilizer.Tolerance:p0}";

            stream.WriteLine(line);
            Console.WriteLine(line);

            line = $"Диапазон нормирования АЦП 2: ({t_normilizer.Min} - " +
                $"{t_normilizer.Max}) +- {t_normilizer.Tolerance:p0}";

            stream.WriteLine(line);
            Console.WriteLine(line);

            // Коэффициенты 
            int n = coefs.GetLength(0) - 1;
            int m = coefs.GetLength(1) - 1;

            const int w = 16;

            stream.WriteLine();
            Console.WriteLine();

            line = $"P(u,v) = sum a[i,j] * u^i * v^j,  i=0..{n}, j=0..{m}";
            stream.WriteLine(line);
            Console.WriteLine(line);

            line = "Строки — степень по u (АЦП 1), столбцы — степень по v (АЦП 2)";
            stream.WriteLine(line);
            Console.WriteLine(line);

            stream.WriteLine();
            Console.WriteLine();

            var sb = new StringBuilder();
            sb.Append("".PadRight(4));
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
                    double c = coefs[i, j];
                    string s;

                    if (c == 0.0)
                        s = "·";
                    else if (Math.Abs(c) >= 0.001 && Math.Abs(c) < 1)
                        s = $"{c:f3}";
                    else if (Math.Abs(c) >= 1 && Math.Abs(c) < 1000000000)
                        s = $"{c:f2}";
                    else
                        s = $"{c:e}";

                    sb.Append(s.PadLeft(w));
                }

                line = sb.ToString();
                stream.WriteLine(line);
                Console.WriteLine(line);
            }

            stream.WriteLine("");
            Console.WriteLine("");

        }

        #endregion
    }
}
