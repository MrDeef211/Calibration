using System.Text;
using static Calibration.MathTool;
using Visualization;

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

            string fileNameOnly = dataFileName[..dataFileName.LastIndexOf('.')];
            string resultFileName = args.Length > 1
                && (args[1].Contains(".txt")
                || args[1].Contains(".svg"))
                ? args[1] : fileNameOnly + "Result.txt";

            PressureData pressureData;

            try
            {
                pressureData = ReadFile(dataFileName);
            }
            catch (Exception ex)
            {
                Print(ex, resultFileName);
                return;
            }

#if VISUAL

            Visualizer.DrawPoints(pressureData.P_Adc, pressureData.T_Adc, pressureData.P_Values, "Входные данные");

#endif

            double[] calibrationt_p_adc = new double[pressureData.CalibrationIndexes.Length];
            double[] calibrationt_t_adc = new double[pressureData.CalibrationIndexes.Length];
            double[] calibrationt_ref = new double[pressureData.CalibrationIndexes.Length];

            for (int i = 0; i < pressureData.CalibrationIndexes.Length; i++)
            {
                calibrationt_p_adc[i] = pressureData.P_Adc[pressureData.CalibrationIndexes[i]];
                calibrationt_t_adc[i] = pressureData.T_Adc[pressureData.CalibrationIndexes[i]];
                calibrationt_ref[i] = pressureData.P_Values[pressureData.CalibrationIndexes[i]];
            }

            double[] p = [];
            double[] dp = [];

            #region Прямой 

            double p_adcMin = pressureData.P_AdcMin;
            double p_adcMax = pressureData.P_AdcMax;

            double t_adcMin = pressureData.T_AdcMin;
            double t_adcMax = pressureData.T_AdcMax;

            double[,] coefficients = new double[0, 0];

            PointsNormilizer p_normilizer;
            PointsNormilizer t_normilizer;

            try
            {

                p_normilizer = new(p_adcMin, p_adcMax, 0.1);
                t_normilizer = new(t_adcMin, t_adcMax, 0.1);

                coefficients = Polyfit2(
                pressureData.P_Pow,
                pressureData.T_Pow,
                p_normilizer.Normilize(calibrationt_p_adc),
                t_normilizer.Normilize(calibrationt_t_adc),
                calibrationt_ref);

                p = Calculate(
                    coefficients,
                    p_normilizer.Normilize(pressureData.P_Adc),
                    t_normilizer.Normilize(pressureData.T_Adc));

                dp = Difference(p, pressureData.P_Values);
            }
            catch (Exception e)
            {
                Print(e, resultFileName);
                return;
            }

            WriteFile(
                resultFileName,
                "Прямой QR метод",
                pressureData,
                coefficients,                
                p_normilizer.Normilize(pressureData.P_Adc),
                t_normilizer.Normilize(pressureData.T_Adc),
                p, dp,
                Quotient(dp, pressureData.P_Max - pressureData.P_Min)
            );
            

            try
            {
                PressureMonotonicityReport monotonicityReport = new(coefficients);

                FitReport fitReport = new(
                    p_normilizer.Normilize(calibrationt_p_adc),
                    t_normilizer.Normilize(calibrationt_t_adc),
                    pressureData.P_Values,
                    dp,
                    pressureData.P_Max - pressureData.P_Min,
                    coefficients
                );

                Print(monotonicityReport, resultFileName);

                Print(fitReport, resultFileName);

                PrintSummaryReport(monotonicityReport, fitReport, resultFileName);
            }
            catch (Exception ex)
            {
                Print(ex, resultFileName);
            }

#if VISUAL

            Visualizer.DrawBoth(coefficients, p_normilizer.Normilize(pressureData.P_Adc), t_normilizer.Normilize(pressureData.T_Adc), pressureData., "Полином", "Эталон");

#endif

            #endregion
        }

        #region Чтение

        static PressureData ReadFile(string path)
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
            double[] @ref = new double[lineCount - 2];
            int[] indexes = new int[lineCount - 2];
            int calibratedPoints = 0;

            using StreamReader stream = new(path);

            string powString = stream.ReadLine() ?? throw new Exception(
                        "Пустой файл или нет данных");

            string[] powStrings = powString.Split(';');

            string diapasonsString = stream.ReadLine() ?? throw new Exception(
                "Пустой файл или нет данных");

            string[] diapasonsStrings = diapasonsString.Split(';');

            if (!int.TryParse(powStrings[0][5..], out int p_pow) || !int.TryParse(powStrings[1][5..], out int t_pow))
                throw new Exception(
                        "Ошибка при чтении показателя степени полинома");

            if (!double.TryParse(diapasonsStrings[0][8..].Replace(',', '.'), out double p_AdcMin)
                || !double.TryParse(diapasonsStrings[1][8..].Replace(',', '.'), out double p_AdcMax)
                || !double.TryParse(diapasonsStrings[2][8..].Replace(',', '.'), out double t_AdcMin)
                || !double.TryParse(diapasonsStrings[3][8..].Replace(',', '.'), out double t_AdcMax)
                || !double.TryParse(diapasonsStrings[4][5..].Replace(',', '.'), out double p_Min)
                || !double.TryParse(diapasonsStrings[5][5..].Replace(',', '.'), out double p_Max)
                || !double.TryParse(diapasonsStrings[6][5..].Replace(',', '.'), out double t_Min)
                || !double.TryParse(diapasonsStrings[7][5..].Replace(',', '.'), out double t_max))
                throw new Exception(
                        "Ошибка при чтении диапазонов работы");

            // Пропускаю заголовки
            stream.ReadLine();

            int index = 0;
            while (index < lineCount - 2)
            {
                string? line = stream.ReadLine();
                if (line == null || line == "") break;

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
                    p_adc[index] = p1;
                else
                    break;

                if (double.TryParse(lines[1].Replace(',', '.'), out double t))
                    t_adc[index] = t;

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
                Array.Resize(ref p_adc, index);
                Array.Resize(ref t_adc, index);
                Array.Resize(ref @ref, index);
            }

            if (calibratedPoints == 0)
                for (calibratedPoints = 0; calibratedPoints < p_adc.Length; calibratedPoints++)
                    indexes[calibratedPoints] = calibratedPoints;

            if (calibratedPoints < indexes.Length)
                Array.Resize(ref indexes, calibratedPoints);

            return new PressureData(
                p_pow, 
                t_pow, 
                indexes, 
                p_adc, 
                t_adc, 
                @ref, 
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

        static void Print(Exception ex, string path)
        {
            using StreamWriter stream = new(path, false);
            Console.WriteLine(ex.Message);
            stream.WriteLine(ex.Message);
        }

        static void Print(PressureMonotonicityReport report, string path)
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
            string separator = new('-', 52);
            string header = $"-------------- Отчет об аппроксимации --------------";
            string cond = $"Число обусловленности:              {report.ConditionNumber:F2} (Норма от 1 до 100000)";
            string rank = $"Ранг матрицы:                       {report.Rank} (Должен быть не меньше чем число коэффициентов (pow_t+1)*(pow_p+1))";
            string points = $"Количество калибровочных точек:     {report.Points}";
            string coef = $"Количество коэффициентов:           {report.Coefficients}";
            string deviations = $"Среднеквадратичная погрешность:     {report.StandardDeviation:f5} ({report.RelativeStandardDeviation:p5}) (Чем меньше тем лучше)";
            string maxDeviations = $"Максимальная погрешность (точки):   {report.MaxDeviation:f5} ({report.MaxRelativeDeviation:p5}) (Чем меньше тем лучше)";
            string confidenceInterval = $"Доверительный интервал (P = 99.7%): +- {(report.StandardDeviation * 3):f5} (Максимальная погрешность с вероятностью P)";

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

            using StreamWriter stream = new(path, true);
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
        }

        static void PrintSummaryReport(
            PressureMonotonicityReport monoReport,
            FitReport fitReport, string path)
        {
            string separator = new('=', 64);
            string header = "================== ИТОГОВЫЙ СТАТУС КАЛИБРОВКИ ==================";

            string status;
            string recommendation;

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
            else if (fitReport.RelativeStandardDeviation > 0.0005 || fitReport.MaxRelativeDeviation > 0.0015)
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

            using StreamWriter stream = new(path, true);
            foreach (var line in lines)
            {
                stream.WriteLine(line);
            }
        }

        static void WriteFile(
            string path,
            string head,
            PressureData data,
            double[,] coeffs,
            double[] p_normalized,
            double[] t_normalized,
            double[] p,
            double[] dp,
            double[] ep,
            bool append = false)
        {
            using StreamWriter stream = new(path, append);

            // Заголовок
            int @char = 120 - head.Length;

            string headDecor = new('=', @char / 2);

            string line = $"{headDecor}|{head}|{headDecor}";

            stream.WriteLine(line);
            Console.WriteLine(line);

            // Заголовки
            line = $"{"АЦП 1",10} {"АЦП 2",11} {"Норм. 1",10} {"Норм. 2",8} {"Эталон",12}" +
                $" {"Результат",16} {"Абс. Погрешность",19} {"Прив. Погрешность",16} {"Калибр.",5}";

            stream.WriteLine(line);
            Console.WriteLine(line);


            int k = 0;
            bool calibr = false;
            // Значения
            for (int i = 0; i < p.Length; i++)
            {
                calibr = data.CalibrationIndexes.Length > k ? data.CalibrationIndexes[k] == i : false;
                if (calibr)
                    k++;

                line = $"{data.P_Adc[i],11:f3} {data.T_Adc[i],11:f3} {p_normalized[i],8:f3} {t_normalized[i],8:f3}" +
                    $" {data.P_Values[i],15:f3} {p[i],15:f3} {dp[i],15:f5} {ep[i],15:p5} {(calibr ? "*" : ""),8}";

                stream.WriteLine(line);
                Console.WriteLine(line);
            }
            // Диапазон
            stream.WriteLine("");
            Console.WriteLine("");

            line = $"Диапазон нормирования АЦП давления: {data.P_AdcMin} - " +
                $"{data.P_AdcMax}, для давления: ({data.P_Min} - {data.P_Max})";

            stream.WriteLine(line);
            Console.WriteLine(line);

            line = $"Диапазон нормирования АЦП температуры: {data.T_AdcMin} - " +
                $"{data.T_AdcMax}, для температуры: ({data.T_Min} - {data.T_Max})";

            stream.WriteLine(line);
            Console.WriteLine(line);

            // Коэффициенты 
            int n = coeffs.GetLength(0) - 1;
            int m = coeffs.GetLength(1) - 1;

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
                    double c = coeffs[i, j];
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

    internal class PressureData
    {
        public int P_Pow { get; init; }
        public int T_Pow { get; init; }
        public int[] CalibrationIndexes { get; init; }
        public double[] P_Adc { get; init; }
        public double[] T_Adc { get; init; }
        public double[] P_Values { get; init; }
        public double P_AdcMin { get; init; }
        public double P_AdcMax { get; init; }
        public double T_AdcMin { get; init; }
        public double T_AdcMax { get; init; }
        public double P_Min { get; init; }
        public double P_Max { get; init; }
        public double T_Min { get; init; }
        public double T_Max { get; init; }

        public PressureData(
            int p_pow, 
            int t_pow, 
            int[] calibrationIndexes, 
            double[] p_adc,
            double[] t_adc, 
            double[] refer,
            double p_adcMin,
            double p_adcMax,
            double t_adcMin,
            double t_adcMax,
            double p_min,
            double p_max,
            double t_min,
            double t_max)
        {
            P_Pow = p_pow;
            T_Pow = t_pow;
            CalibrationIndexes = calibrationIndexes;
            P_Adc = p_adc;
            T_Adc = t_adc;
            P_Values = refer;
            P_AdcMin = p_adcMin;
            P_AdcMax = p_adcMax;
            T_AdcMin = t_adcMin;
            T_AdcMax = t_adcMax;
            P_Min = p_min;
            P_Max = p_max;
            T_Min = t_min;
            T_Max = t_max;
        }

        public PressureData()
        {
            P_Pow = 0;
            T_Pow = 0;
            CalibrationIndexes = [];
            P_Adc = [];
            T_Adc = [];
            P_Values = [];
            P_AdcMin = 0;
            P_AdcMax = 0;
            T_AdcMin = 0;
            T_AdcMax = 0;
            P_Min = 0;
            P_Max = 0;
            T_Min = 0;
            T_Max = 0;
        }
    }
}
