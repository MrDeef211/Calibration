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
                ? args[0] : "Temperature.txt";

            string fileNameOnly = dataFileName.Substring(0, dataFileName.LastIndexOf('.'));
            string resultFileName = args.Length > 1
                && (args[1].Contains(".txt")
                || args[1].Contains(".svg"))
                ? args[1] : fileNameOnly + "Result.txt";



            (int pow, int[] t_calibrationIndexes, double[] t_ads, double[] t_ref) = ReadFile(dataFileName);

            double[] calibrationt_ads = new double[t_calibrationIndexes.Length];
            double[] calibrationt_ref = new double[t_calibrationIndexes.Length];

            for (int i = 0; i < t_calibrationIndexes.Length; i++)
            {
                calibrationt_ads[i] = t_ads[t_calibrationIndexes[i]];
                calibrationt_ref[i] = t_ref[t_calibrationIndexes[i]];
            }

            double[] t = [];
            double[] dt = [];

            #region Температура без нормализации

            double[] coeffcients = MathTool.Polyfit1(
                pow,
                calibrationt_ads,
                calibrationt_ref
            );

            try
            {
                t = MathTool.Calculate(
                    coeffcients,
                    t_ads
                );

                dt = MathTool.Difference(t, t_ref);
            }
            catch (Exception ex)
            {
                Print(ex, resultFileName);
                return;
            }


            WriteFile(
                resultFileName,
                "Без нормализации",
                coeffcients,
                t_ads, t_ref, t, dt,
                MathTool.PointwiseQuotient(dt, t_ref)
            );

            double deviation = MathTool.GetStandardDeviation(dt);

            Print(deviation, resultFileName);

            #endregion

            #region Температура с нормализацией

            double min = t_ads.Min();
            double max = t_ads.Max();

            double[] normilizeCoeffcients = [];

            PointsNormilizer normilizer = new PointsNormilizer(min, max, 0.1);

            try
            {
                normilizeCoeffcients = MathTool.Polyfit1(
                    pow,
                    normilizer.Normilize(calibrationt_ads),
                    calibrationt_ref
                );

                t = MathTool.Calculate(
                    normilizeCoeffcients,
                    normilizer.Normilize(t_ads)
                );

                dt = MathTool.Difference(t, t_ref);
            }
            catch (Exception ex)
            {
                Print(ex, resultFileName, true);
                return;
            }


            WriteFile(
                resultFileName,
                "С нормализацией",
                normilizeCoeffcients,
                t_ads,
                normilizer.Normilize(t_ads),
                normilizer,
                t_ref, t, dt,
                MathTool.PointwiseQuotient(dt, t_ref),
                true
            );

            double normilizeDeviation = MathTool.GetStandardDeviation(dt);

            Print(normilizeDeviation, resultFileName);

            TemperatureMonotonicityReport report = new TemperatureMonotonicityReport(coeffcients);

            Print(report, resultFileName);

            PrintSummaryReport(report, normilizeDeviation, resultFileName);

            #endregion
        }

        #region Чтение

        static (int pow, int[] calibrationIndexes, double[] ads, double[] refer) ReadFile(string path)
        {
            int lineCount = 0;
            using (var reader = new StreamReader(path))
            {
                while (reader.ReadLine() != null) lineCount++;
            }

            if (lineCount <= 2) return (0, [], [], []);

            double[] ads = new double[lineCount - 2];
            double[] @ref = new double[lineCount - 2];
            int[] indexes = new int[lineCount - 2];
            int calibratedPoints = 0;
            int pow = 1;

            using StreamReader stream = new(path);

            string? powString = stream.ReadLine()?.Substring(4);

            if (powString == null || !int.TryParse(powString, out pow))
                return (0, [], [], []);

            // Пропускаю заголовки
            stream.ReadLine();

            int index = 0;
            while (index < lineCount - 2)
            {
                string? line = stream.ReadLine();
                if (line == null) break;

                string[] lines = line.Split(';');

                if (double.TryParse(lines[0].Replace(',', '.'), out double t1))
                    ads[index] = t1;
                else
                    break;

                if (double.TryParse(lines[1].Replace(',', '.'), out double t2))
                    @ref[index] = t2;

                if (lines[2] == "*")
                {
                    indexes[calibratedPoints] = index;
                    calibratedPoints++;
                }

                index++;
            }

            if (index < lineCount - 2)
            {
                Array.Resize(ref ads, index);
                Array.Resize(ref @ref, index);
            }

            if (calibratedPoints == 0)
                for (calibratedPoints = 0; calibratedPoints < ads.Length; calibratedPoints++)
                    indexes[calibratedPoints] = calibratedPoints;

            if (calibratedPoints < ads.Length)
                Array.Resize(ref indexes, calibratedPoints);

            return (pow, indexes, ads, @ref);
        }

        #endregion

        #region Запись

        static void Print(double standardDeviation, string path)
        {
            string deviation = $"Среднеквадратичное отклонение = {standardDeviation}";

            Console.WriteLine(deviation);
            Console.WriteLine();

            using StreamWriter stream = new(path, true);
            stream.WriteLine(deviation);
            stream.WriteLine();
        }

        static void Print(TemperatureMonotonicityReport report, string path)
        {
            string separator = new string('-', 64);
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

            using StreamWriter stream = new(path, true);
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
        }

        static void PrintSummaryReport(
            TemperatureMonotonicityReport monoReport,
            double standardDeviation, string path)
        {
            string separator = new string('=', 64);
            string header = "================== ИТОГОВЫЙ СТАТУС КАЛИБРОВКИ ==================";

            string status = "";
            string recommendation = "";

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
            else if (standardDeviation > 0.01)
            {
                status = "ПРЕДУПРЕЖДЕНИЕ: Нестабильная калибровка (высокая погрешность).";
                recommendation = "РЕКОМЕНДУЕТСЯ ПЕРЕКАЛИБРОВАТЬ. Точки измерения распределены " +
                    "неравномерно, содержат сильные шумы (ошибки), либо точек недостаточно. " +
                    "Проверьте датчики и повторите замеры в других диапазонах.";
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

        static void Print(Exception ex, string path, bool append = false)
        {
            using StreamWriter stream = new(path, append);
            Console.WriteLine(ex.Message);
            stream.WriteLine(ex.Message);
        }

        static void WriteFile(
            string path,
            string head,
            double[] coefs,
            double[] t_ads,
            double[] t_ref,
            double[] t,
            double[] dt,
            double[] et,
            bool append = false)
        {
            using StreamWriter stream = new(path, append);

            // Заголовок
            int c = 80 - head.Length;

            string headDecor = new string('=', c / 2);

            string line = $"{headDecor}|{head}|{headDecor}";

            stream.WriteLine(line);
            Console.WriteLine(line);

            // Заголовки
            line = $"{"АЦП",8} {"Эталон",18} {"Результат",16} {"Абс. Погрешность",19} {"Отн. Погрешность",16}";

            stream.WriteLine(line);
            Console.WriteLine(line);

            // Значения
            for (int i = 0; i < t.Length; i++)
            {
                line = !double.IsInfinity(et[i])
                    ? $"{t_ads[i],11:f3} {t_ref[i],15:f3} {t[i],15:f3} {dt[i],15:f3} {et[i],15:p2}"
                    : $"{t_ads[i],11:f3} {t_ref[i],15:f3} {t[i],15:f3} {dt[i],15:f3} {"деление на 0",15}";

                stream.WriteLine(line);
                Console.WriteLine(line);
            }
            // Коэфициенты
            stream.WriteLine("");
            Console.WriteLine("");

            stream.Write("Коэфициенты = ");
            Console.Write("Коэфициенты = ");

            for (int i = coefs.Length - 1; i >= 0; i--)
            {
                if (Math.Abs(coefs[i]) >= 0.001 && Math.Abs(coefs[i]) < 1)
                    line = (i != 0)
                        ? $"{coefs[i]:f3}x^{i} + "
                        : $"{coefs[i]:f3}";
                else if (Math.Abs(coefs[i]) >= 1)
                    line = (i != 0)
                        ? $"{coefs[i]:f2}x^{i} + "
                        : $"{coefs[i]:f2}";
                else
                    line = (i != 0)
                        ? $"{coefs[i]:e2}x^{i} + "
                        : $"{coefs[i]:e2}";

                stream.Write(line);
                Console.Write(line);
            }

            stream.WriteLine("\n");
            Console.WriteLine("\n");

            stream.WriteLine("В полном виде:");
            Console.WriteLine($"В полном виде:");

            for (int i = coefs.Length - 1; i >= 0; i--)
            {
                line = $"{coefs[i]}";
                stream.WriteLine(line);
                Console.WriteLine(line);
            }

            stream.WriteLine("");
            Console.WriteLine("");
        }

        static void WriteFile(
            string path,
            string head,
            double[] coefs,
            double[] t_ads,
            double[] normilized,
            PointsNormilizer normilizer,
            double[] t_ref,
            double[] t,
            double[] dt,
            double[] et,
            bool append = false)
        {
            using StreamWriter stream = new(path, append);

            // Заголовок
            int c = 95 - head.Length;

            string headDecor = new string('=', c / 2);

            string line = $"{headDecor}|{head}|{headDecor}";

            stream.WriteLine(line);
            Console.WriteLine(line);

            // Заголовки
            line = $"{"АЦП",8} {"После Нормализации",24} {"Эталон",9} {"Результат",16} {"Абс. Погрешность",19} {"Отн. Погрешность",16}";

            stream.WriteLine(line);
            Console.WriteLine(line);

            // Значения
            for (int i = 0; i < t.Length; i++)
            {

                line = !double.IsInfinity(et[i])
                    ? $"{t_ads[i],11:f3} {normilized[i],15:f3} {t_ref[i],15:f3} {t[i],15:f3} {dt[i],15:f3} {et[i],15:p2}"
                    : $"{t_ads[i],11:f3} {normilized[i],15:f3} {t_ref[i],15:f3} {t[i],15:f3} {dt[i],15:f3} {"деление на 0",15}";

                stream.WriteLine(line);
                Console.WriteLine(line);
            }
            // Диапазон
            stream.WriteLine("");
            Console.WriteLine("");

            line = $"Диапазон нормирования: ({normilizer.Min} - {normilizer.Max}) +- {normilizer.Tolerance:p0}";

            stream.WriteLine(line);
            Console.WriteLine(line);

            // Коэфициенты
            stream.WriteLine("");
            Console.WriteLine("");

            stream.Write("Коэфициенты = ");
            Console.Write("Коэфициенты = ");

            for (int i = coefs.Length - 1; i >= 0; i--)
            {
                if (Math.Abs(coefs[i]) >= 0.001 && Math.Abs(coefs[i]) < 1)
                    line = (i != 0)
                        ? $"{coefs[i]:f3}x^{i} + "
                        : $"{coefs[i]:f3}";
                else if (Math.Abs(coefs[i]) >= 1)
                    line = (i != 0)
                        ? $"{coefs[i]:f2}x^{i} + "
                        : $"{coefs[i]:f2}";
                else
                    line = (i != 0)
                        ? $"{coefs[i]:e2}x^{i} + "
                        : $"{coefs[i]:e2}";

                stream.Write(line);
                Console.Write(line);
            }

            stream.WriteLine("\n");
            Console.WriteLine("\n");

            stream.WriteLine("В полном виде:");
            Console.WriteLine($"В полном виде:");

            for (int i = coefs.Length - 1; i >= 0; i--)
            {
                line = $"{coefs[i]}";
                stream.WriteLine(line);
                Console.WriteLine(line);
            }

            stream.WriteLine("");
            Console.WriteLine("");

        }

        #endregion
    }
}
