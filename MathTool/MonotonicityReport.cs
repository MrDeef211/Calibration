namespace Calibration
{
    public enum MonotonicityVerdict
    {
        Ok,
        WeakMargin,
        Violated
    }

    public sealed class TemperatureMonotonicityReport
    {
        public MonotonicityVerdict Verdict { get; init; }
        public int Sign { get; init; }
        public double MinAbsG { get; init; }
        public double TAtMin { get; init; }
        public double Delta { get; init; }
        public double MeanAbsG { get; init; }
        public int Nt { get; init; }
        public int ViolationsCount { get; init; }
        public double TAtFirstViolation { get; init; }

        public TemperatureMonotonicityReport(
        double[] A,
        int nt = 100,
        int expectedSign = 0,
        double delta = double.NaN,
        double deltaFraction = 0.02)
        {
            if (A is null) throw new ArgumentNullException(nameof(A));
            if (nt < 2) throw new ArgumentOutOfRangeException(nameof(nt));

            int n = A.Length - 1;

            if (n < 1)
            {
                Verdict = MonotonicityVerdict.Violated;
                Sign = 0;
                MinAbsG = 0;
                TAtMin = 0;
                Delta = delta;
                MeanAbsG = 0;
                Nt = nt;
                ViolationsCount = nt;
                TAtFirstViolation = double.NaN;
                return;
            }

            double tStep = 2.0 / (nt - 1);

            double minAbsG = double.PositiveInfinity;
            double sumAbsG = 0.0;
            long count = 0;
            double tAtMin = 0.0;

            int sign = expectedSign;
            int violations = 0;
            double tFirstViol = double.NaN;

            for (int it = 0; it < nt; it++)
            {
                double t = -1.0 + it * tStep;
                if (it == nt - 1) t = 1.0;

                double g = n * A[n];
                for (int i = n - 1; i >= 1; i--)
                    g = g * t + i * A[i];

                double ag = Math.Abs(g);
                sumAbsG += ag;
                count++;

                if (ag < minAbsG)
                {
                    minAbsG = ag;
                    tAtMin = t;
                }

                if (sign == 0 && ag > 0)
                    sign = g > 0 ? +1 : -1;

                if (sign != 0 && (g > 0 ? +1 : -1) != sign)
                {
                    violations++;
                    if (double.IsNaN(tFirstViol))
                        tFirstViol = t;
                }
            }

            double meanAbsG = sumAbsG / count;

            double d = double.IsNaN(delta)
                ? deltaFraction * meanAbsG
                : delta;

            MonotonicityVerdict verdict;
            if (violations > 0)
                verdict = MonotonicityVerdict.Violated;
            else if (minAbsG < d)
                verdict = MonotonicityVerdict.WeakMargin;
            else
                verdict = MonotonicityVerdict.Ok;

            Verdict = verdict;
            Sign = sign;
            MinAbsG = minAbsG;
            TAtMin = tAtMin;
            Delta = d;
            MeanAbsG = meanAbsG;
            Nt = nt;
            ViolationsCount = violations;
            TAtFirstViolation = tFirstViol;
        }
    }

    public sealed class PressureMonotonicityReport
    {
        public MonotonicityVerdict Verdict { get; init; }
        public int Sign { get; init; }
        public double MinAbsG { get; init; }
        public double UAtMin { get; init; }
        public double VAtMin { get; init; }
        public double Delta { get; init; }
        public double MeanAbsG { get; init; }
        public int Nu { get; init; }
        public int Nv { get; init; }
        public int ViolationsCount { get; init; }
        public double UAtFirstViolation { get; init; }
        public double VAtFirstViolation { get; init; }

        public PressureMonotonicityReport(
            double[,] A,
            int nu = 100,
            int nv = 100,
            int expectedSign = 0,
            double delta = double.NaN,
            double deltaFraction = 0.02)
        {
            if (nu < 2) throw new ArgumentOutOfRangeException(nameof(nu));
            if (nv < 2) throw new ArgumentOutOfRangeException(nameof(nv));

            int n = A.GetLength(0) - 1;
            int m = A.GetLength(1) - 1;

            if (n < 1)
            {
                Verdict = MonotonicityVerdict.Violated;
                Sign = 0;
                MinAbsG = 0;
                Delta = delta;
                Nu = nu;
                Nv = nv;
                ViolationsCount = nu * nv;
            }

            double uStep = 2.0 / (nu - 1);
            double vStep = 2.0 / (nv - 1);

            double minAbsG = double.PositiveInfinity;
            double sumAbsG = 0.0;
            long count = 0;
            double uAtMin = 0, vAtMin = 0;

            int sign = expectedSign;
            int violations = 0;
            double uFirstViol = double.NaN, vFirstViol = double.NaN;

            for (int iv = 0; iv < nv; iv++)
            {
                double v = -1.0 + iv * vStep;
                if (iv == nv - 1) v = 1.0;

                Span<double> c = stackalloc double[n + 1];
                for (int i = 0; i <= n; i++)
                {
                    double ci = A[i, m];
                    for (int j = m - 1; j >= 0; j--)
                        ci = ci * v + A[i, j];
                    c[i] = ci;
                }

                for (int iu = 0; iu < nu; iu++)
                {
                    double u = -1.0 + iu * uStep;
                    if (iu == nu - 1) u = 1.0;

                    double g = n * c[n];
                    for (int i = n - 1; i >= 1; i--)
                        g = g * u + i * c[i];

                    double ag = Math.Abs(g);
                    sumAbsG += ag;
                    count++;

                    if (ag < minAbsG)
                    {
                        minAbsG = ag;
                        uAtMin = u;
                        vAtMin = v;
                    }

                    if (sign == 0 && ag > 0)
                        sign = g > 0 ? +1 : -1;

                    if (sign != 0 && (g > 0 ? +1 : -1) != sign)
                    {
                        violations++;
                        if (double.IsNaN(uFirstViol))
                        {
                            uFirstViol = u;
                            vFirstViol = v;
                        }
                    }
                }
            }

            double meanAbsG = sumAbsG / count;

            double d = double.IsNaN(delta) ? deltaFraction * meanAbsG : delta;

            MonotonicityVerdict verdict;
            if (violations > 0)
                verdict = MonotonicityVerdict.Violated;
            else if (minAbsG < d)
                verdict = MonotonicityVerdict.WeakMargin;
            else
                verdict = MonotonicityVerdict.Ok;

            Verdict = verdict;
            Sign = sign;
            MinAbsG = minAbsG;
            UAtMin = uAtMin;
            VAtMin = vAtMin;
            Delta = d;
            MeanAbsG = meanAbsG;
            Nu = nu;
            Nv = nv;
            ViolationsCount = violations;
            UAtFirstViolation = uFirstViol;
            VAtFirstViolation = vFirstViol;
        }
    }
}
