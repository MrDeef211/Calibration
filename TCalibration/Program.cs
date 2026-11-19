using System.Text.RegularExpressions;
using Visualization;
using static Calibration.MathTool;


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

            string fileNameOnly = dataFileName[..dataFileName.LastIndexOf('.')];
            string resultFileName = args.Length > 1
                && (args[1].Contains(".txt")
                || args[1].Contains(".svg"))
                ? args[1] : fileNameOnly + "Result.txt";

            int pow;
            int[] t_calibrationIndexes;
            double[] t_adc, t_ref;

            TemperatureData temperatureData;

            try
            {
                temperatureData = ReadFile(dataFileName);
            }
            catch (Exception ex)
            {
                Print(ex, resultFileName);
                return;
            }

            pow = temperatureData.T_Pow;
            t_calibrationIndexes = temperatureData.CalibrationIndexes;
            t_adc = temperatureData.T_Adc;
            t_ref = temperatureData.T_Values;

#if VISUAL

            Visualizer.DrawPoints(t_adc, t_ref, "Входные данные");

#endif

            double[] calibrationt_adc = new double[t_calibrationIndexes.Length];
            double[] calibrationt_ref = new double[t_calibrationIndexes.Length];

            for (int i = 0; i < t_calibrationIndexes.Length; i++)
            {
                calibrationt_adc[i] = t_adc[t_calibrationIndexes[i]];
                calibrationt_ref[i] = t_ref[t_calibrationIndexes[i]];
            }

            double[] t = [];
            double[] dt = [];
            double[] et = [];

#if DEBUG

            #region Температура без нормализации

            double[] coeffcients = Polyfit1(
                pow,
                calibrationt_adc,
                calibrationt_ref
            );

            try
            {
                t = Calculate(
                    coeffcients,
                    t_adc
                );

                dt = Difference(t, t_ref);
                et = Quotient(dt, temperatureData.T_Max - temperatureData.T_Min);
            }
            catch (Exception ex)
            {
                Print(ex, resultFileName);
                return;
            }


            WriteFile(
                resultFileName,
                "Без нормализации",
                temperatureData,
                coeffcients,
                t, dt, et               
            );

            Print(dt, et, resultFileName);

            #endregion

#endif

            #region Температура с нормализацией

            double min = temperatureData.T_AdcMin;
            double max = temperatureData.T_AdcMax;

            double[] normilizeCoeffcients = [];

            PointsNormilizer normilizer;

            try
            {
                normilizer = new PointsNormilizer(min, max, 0.1);

                normilizeCoeffcients = Polyfit1(
                    pow,
                    normilizer.Normilize(calibrationt_adc),
                    calibrationt_ref
                );

                t = Calculate(
                    normilizeCoeffcients,
                    normilizer.Normilize(t_adc)
                );

                dt = Difference(t, t_ref);
                et = Quotient(dt, temperatureData.T_Max - temperatureData.T_Min);
            }
            catch (Exception ex)
            {
                Print(ex, resultFileName, true);
                return;
            }

            WriteFile(
                resultFileName,
                "С нормализацией",
                temperatureData,
                normilizeCoeffcients,
                normilizer.Normilize(t_adc),
                t, dt, et,
                true
            );

            Print(dt, et, resultFileName);

            try
            {
                TemperatureMonotonicityReport report = new(normilizeCoeffcients);

                Print(report, resultFileName);

                PrintSummaryReport(report, et, resultFileName);
            }
            catch (Exception ex) 
            {
                Print(ex, resultFileName);
            }

#if VISUAL

            Visualizer.DrawBoth(normilizeCoeffcients, normilizer.Normilize(t_adc), t_ref, "Полином", "Эталон");

#endif

            #endregion

//#if DEBUG

//            bool hasHeteroscedasticity = false;

//            try
//            {
//                hasHeteroscedasticity = HasHeteroskedasticity(calibrationt_adc, calibrationt_ref, 1);
//            }
//            catch (Exception ex)
//            {
//                Print(ex, resultFileName);
//            }

//            Print($"{(hasHeteroscedasticity ? "Данные гетероскедастичны" : "Данные гомоскедастичны")}", resultFileName);

//            // ЗАВЕРШАЕТ ПРОГРАММУ, ПОМЕНЯТЬ ЕСЛИ ПОСЛЕ ЭТОГО РЕГИОНА БУДЕТ ДРУГОЙ КОД
//            if (!hasHeteroscedasticity)
//                return;

//            #region Температура с нормализацией и матрицей весов (если данные гетероскедастичны)

//            try
//            {
//                double[] heteroscedasticityWeights = GetWeights(t, dt);

//                normilizeCoeffcients = Polyfit1(
//                    pow,
//                    normilizer.Normilize(calibrationt_adc),
//                    calibrationt_ref,
//                    heteroscedasticityWeights
//                );

//                t = Calculate(
//                    normilizeCoeffcients,
//                    normilizer.Normilize(t_adc)
//                );

//                dt = Difference(t, t_ref);
//                et = PointWiseQuotient(dt, t_ref);
//            }
//            catch (Exception ex)
//            {
//                Print(ex, resultFileName, true);
//                return;
//            }


//            WriteFile(
//                resultFileName,
//                "С нормализацией и матрицей весов",
//                normilizeCoeffcients,
//                t_calibrationIndexes,
//                t_adc,
//                normilizer.Normilize(t_adc),
//                normilizer,
//                t_ref, t, dt, et,
//                true
//            );

//            Print(dt, et, resultFileName);

//            try
//            {
//                TemperatureMonotonicityReport report = new(coeffcients);

//                Print(report, resultFileName);

//                PrintSummaryReport(report, et, resultFileName);
//            }
//            catch (Exception ex)
//            {
//                Print(ex, resultFileName);
//            }

//            #endregion
//#endif

        }

        #region Чтение

        static TemperatureData ReadFile(string path)
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

            string? powString = stream.ReadLine()?[4..];

            string diapasonsString = stream.ReadLine() ?? throw new Exception(
                "Пустой файл или нет данных");

            string[] diapasonsStrings = Regex.Replace(diapasonsString.Replace(',', '.'), @"\s+", ";").Split(';');

            if (powString == null || !int.TryParse(powString, out int pow))
                throw new Exception(
                        "Ошибка при чтении показателя степени полинома");

            if (!double.TryParse(diapasonsStrings[0][8..], out double t_AdcMin)
                || !double.TryParse(diapasonsStrings[1][8..], out double t_AdcMax)
                || !double.TryParse(diapasonsStrings[2][5..], out double t_Min)
                || !double.TryParse(diapasonsStrings[3][5..], out double t_max))
                throw new Exception(
                        "Ошибка при чтении диапазонов работы");

            // Пропускаю заголовки
            stream.ReadLine();

            int index = 0;
            while (index < lineCount - 2)
            {
                string? line = stream.ReadLine();
                if (line == null || line == "") break;

                string[] lines = Regex.Replace(line.Replace(',', '.'), @"\s+", ";").Split(';');
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

            return new TemperatureData(pow, indexes, adc, @ref, t_AdcMin, t_AdcMax, t_Min, t_max);
        }

        #endregion

        #region Запись

        static void Print(double[] dx, double[] ex, string path)
        {
            string deviation = $"Среднеквадратичная погрешность = {GetStandardDeviation(dx)} ({GetStandardDeviation(ex):p2}) (Чем меньше тем лучше)";
            string maxDeviation = $"Наибольшая погрешность = {dx.Max(d => double.IsFinite(d) ? Math.Abs(d) : 0)}" +
                $" ({ex.Max(d => double.IsFinite(d) ? Math.Abs(d) : 0):p2}) (Чем меньше тем лучше)";
            string confidenceInterval = $"Доверительный интервал (P = 99.7%) = +- {(GetStandardDeviation(dx) * 3):f5} (Максимальная погрешность с вероятностью P)";

            Console.WriteLine(deviation);
            Console.WriteLine(maxDeviation);
            Console.WriteLine(confidenceInterval);
            Console.WriteLine();

            using StreamWriter stream = new(path, true);
            stream.WriteLine(deviation);
            stream.WriteLine(maxDeviation);
            stream.WriteLine(confidenceInterval);
            stream.WriteLine();
        }

        static void Print(string text, string path)
        {
            Console.WriteLine(text);
            Console.WriteLine();

            using StreamWriter stream = new(path, true);
            stream.WriteLine(text);
            stream.WriteLine();
        }

        static void Print(TemperatureMonotonicityReport report, string path)
        {
            string separator = new('-', 64);
            string header = "------ Отчет о монотонности (температура с нормированием) ------";
            string verdict = $"Вердикт:                        {report.Verdict}";
            string sign = $"Знак производной:               {(report.Sign > 0 ? "+" : "-")}";
            string minAbsG = $"Мин. абс. G (Мин. производная): {report.MinAbsG:F6}";
            string tAtMin = $"T при мин. G:                   {report.TAtMin:F4} ({Calculate(report.Coefficients, report.TAtMin)})";
            string delta = $"Дельта G:                       {report.Delta:E4}";
            string meanAbsG = $"Средний абс. G:                 {report.MeanAbsG:F6}";
            string grid = $"Сетка:                          {report.Nt}";
            string violations = $"Кол-во нарушений:               {report.ViolationsCount}";
            string tFirstViol = $"T первого нарушения:            {report.TAtFirstViolation:F4} ({Calculate(report.Coefficients, report.TAtFirstViolation)})";

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
            double[] ex, string path)
        {
            string separator = new('=', 64);
            string header = "================== ИТОГОВЫЙ СТАТУС КАЛИБРОВКИ ==================";

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
            else if (GetStandardDeviation(ex) > 0.0005 || ex.Max(d => double.IsFinite(d) ? Math.Abs(d) : 0) > 0.0015)
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
            TemperatureData data,
            double[] coeffs,
            double[] t,
            double[] dt,
            double[] et,
            bool append = false)
        {
            using StreamWriter stream = new(path, append);

            // Заголовок
            int c = 90 - head.Length;

            string headDecor = new('=', c / 2);

            string line = $"{headDecor}|{head}|{headDecor}";

            stream.WriteLine(line);
            Console.WriteLine(line);

            // Заголовки
            line = $"{"АЦП",8} {"Эталон",18} {"Результат",16} {"Абс. Погрешность",19} {"Отн. Погрешность",16} {"Калибр.",5}";

            stream.WriteLine(line);
            Console.WriteLine(line);


            int j = 0;
            bool calibr = false;
            // Значения
            for (int i = 0; i < t.Length; i++)
            {
                calibr = data.CalibrationIndexes.Length > j ? data.CalibrationIndexes[j] == i : false;
                if (calibr)
                    j++;

                line = $"{data.T_Adc[i],11:f3} {data.T_Values[i],15:f3} {t[i],15:f3} {dt[i],15:f3} {et[i],15:p3} {(calibr ? "*" : ""),10}";

                stream.WriteLine(line);
                Console.WriteLine(line);
            }
            // Коэффициенты
            stream.WriteLine("");
            Console.WriteLine("");

            stream.Write("Коэффициенты = ");
            Console.Write("Коэффициенты = ");

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
        }

        static void WriteFile(
            string path,
            string head,
            TemperatureData data,
            double[] coeffs,
            double[] t_normalized,
            double[] t,
            double[] dt,
            double[] et,
            bool append = false)
        {
            using StreamWriter stream = new(path, append);

            // Заголовок
            int c = 105 - head.Length;

            string headDecor = new('=', c / 2);

            string line = $"{headDecor}|{head}|{headDecor}";

            stream.WriteLine(line);
            Console.WriteLine(line);

            // Заголовки
            line = $"{"АЦП",8} {"После Нормализации",24} {"Эталон",9} {"Результат",16} {"Абс. Погрешность",19} {"Отн. Погрешность",16} {"Калибр.", 5}";

            stream.WriteLine(line);
            Console.WriteLine(line);

            int j = 0;
            bool calibr = false;
            // Значения
            for (int i = 0; i < t.Length; i++)
            {
                calibr = data.CalibrationIndexes.Length > j ? data.CalibrationIndexes[j] == i : false;
                if (calibr)
                    j++;

                line = $"{data.T_Adc[i],11:f3} {t_normalized[i],15:f3} {data.T_Values[i],15:f3} {t[i],15:f3} {dt[i],15:f3} {et[i],15:p3} {(calibr ? "*" : ""),10}";

                stream.WriteLine(line);
                Console.WriteLine(line);
            }
            // Диапазон
            stream.WriteLine("");
            Console.WriteLine("");

            line = $"Диапазон нормирования: {data.T_AdcMin} - " +
                $"{data.T_AdcMax}, для температуры: ({data.T_Min} - {data.T_Max})";

            stream.WriteLine(line);
            Console.WriteLine(line);

            // Коэффициенты
            stream.WriteLine("");
            Console.WriteLine("");

            stream.Write("Коэффициенты = ");
            Console.Write("Коэффициенты = ");

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

        }

        #endregion
    }

    public class TemperatureData
    {
        public int T_Pow { get; init; }
        public int[] CalibrationIndexes { get; init; }
        public double[] T_Adc { get; init; }
        public double[] T_Values { get; init; }
        public double T_AdcMin { get; init; }
        public double T_AdcMax { get; init; }
        public double T_Min { get; init; }
        public double T_Max { get; init; }

        public TemperatureData(
            int t_pow,
            int[] calibrationIndexes,
            double[] t_adc,
            double[] refer,
            double t_adcMin,
            double t_adcMax,
            double t_min,
            double t_max)
        {
            T_Pow = t_pow;
            CalibrationIndexes = calibrationIndexes;
            T_Adc = t_adc;
            T_Values = refer;
            T_AdcMin = t_adcMin;
            T_AdcMax = t_adcMax;
            T_Min = t_min;
            T_Max = t_max;
        }
        public TemperatureData()
        {
            T_Pow = 0;
            CalibrationIndexes = [];
            T_Adc = [];
            T_Values = [];
            T_AdcMin = 0;
            T_AdcMax = 0;
            T_Min = 0;
            T_Max = 0;
        }
    }
}
