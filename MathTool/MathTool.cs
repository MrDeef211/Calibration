// Ignore Spelling: xin Polyfit Heteroskedasticity Rss

using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using MathNet.Numerics.Providers.LinearAlgebra;

namespace Calibration;

public static class MathTool
{
    public static double? Round(double? value, int digits)
    {
        return value == null ? null : Math.Round(value.Value, digits);
    }
    public static void Avg(ref double avg, long val, long index)
    {
        avg = avg - avg / index + val / index;
    }

    public static void Avg(ref double avg, double val, long index)
    {
        avg = avg - avg / index + val / index;
    }
    public static string ToString(this double? d, int precision)
    {
        return d.HasValue ? Math.Round(d.Value, precision).ToString() : string.Empty;
    }
    public static string ToString(this double d, int precision)
    {
        return Math.Round(d, precision).ToString();
    }

    public static double[] Difference(double[] x, double[] y)
    {
        double[] result = new double[x.Length];
        LinearAlgebraControl.Provider.SubtractArrays(x, y, result);
        return result;
    }

    public static double[] PointWiseQuotient(double[] x, double[] y)
    {
        Vector<double> v1 = Vector<double>.Build.Dense(x);
        Vector<double> v2 = Vector<double>.Build.Dense(y);

        Vector<double> result = v1.PointwiseDivide(v2);
        return result.AsArray();
    }

    public static double[] Quotient(double[] x, double y)
    {
        Vector<double> v1 = Vector<double>.Build.Dense(x);

        Vector<double> result = v1 / y;
        return result.AsArray();
    }

    public static double Calculate(double[] coefficients, double t_adc)
    {
        return Polynomial.Evaluate(t_adc, coefficients);
    }

    public static double[] Calculate(double[] coefficients, double[] t_adc)
    {
        var poly = new Polynomial(coefficients);
        return Array.ConvertAll(t_adc, x => poly.Evaluate(x));
    }

    public static double[] Calculate(
        double[,] A,
        double[] p_adc, double[] t_adc)
    {
        if (p_adc.Length != t_adc.Length)
            throw new ArgumentException("Длины p_adc и t_adc должны совпадать");

        var a = PackA(A);
        var H = BuildDesignMatrix(A.GetLength(0) - 1, A.GetLength(1) - 1, p_adc, t_adc);

        return [.. H * a];
    }

    public static double Calculate(
        double[,] A,
        double p_adc,
        double t_adc)
    {
        int n = A.GetLength(0) - 1;
        int m = A.GetLength(1) - 1;

        Span<double> c = stackalloc double[n + 1];
        for (int i = 0; i <= n; i++)
        {
            double ci = A[i, m];
            for (int j = m - 1; j >= 0; j--)
                ci = ci * t_adc + A[i, j];
            c[i] = ci;
        }

        double P = c[n];
        for (int i = n - 1; i >= 0; i--)
            P = P * p_adc + c[i];

        return P;
    }

    public static double GetStandardDeviation(double[] deviations)
    {
        double deviation = 0;
        int deviasionCount = 0;
        foreach (var dev in deviations)
        {
            if (!double.IsFinite(dev))
                continue;
            deviation +=  dev * dev;
            deviasionCount++;
        }
        deviation /= deviasionCount;
        return Math.Sqrt(deviation);
    }

    //public static double[] Polyfit(int Size, double[] x, double[] y)
    //{
    //    var cof = new double[Size];
    //    //for(var r=0;r<Size; r++)
    //    //    cof[r] = 0d;

    //    int i = 0;
    //    int j = 0;
    //    int N = Size;
    //    double[] S = new double[N];
    //    int N1 = N - 1;
    //    double PHI;
    //    double FF;
    //    double B;

    //    for (i = 0; i < N; i++)
    //    {
    //        S[i] = 0.0;
    //    }

    //    S[N1] = -x[0];
    //    for (i = 1; i < N; i++)
    //    {
    //        for (j = N1 - i; j < N1; j++)
    //        {
    //            S[j] -= x[i] * S[j + 1];
    //        }
    //        S[N1] -= x[i];
    //    }
    //    for (i = 0; i < N; i++)
    //    {
    //        PHI = N;
    //        for (j = N1; j > 0; j--)
    //        {
    //            PHI = j * S[j] + x[i] * PHI;
    //        }
    //        FF = y[i] / PHI;
    //        B = 1.0;
    //        for (j = N1; j >= 0; j--)
    //        {
    //            cof[j] += B * FF;
    //            B = S[j] + x[i] * B;
    //        }
    //    }
    //    return cof;
    //}

    public static double[] Polyfit1(int pow, double[] xin, double[] yin)
    {
        // https://studfile.net/preview/9818132/page:10/
        /*
        Matrix<double> x = Matrix<double>.Build.DenseOfArray(new double[,]
        {
            { 1, 10 },
            { 1, 20 },
            { 1, 30 },
            { 1, 80 }
        });
        Matrix<double> y = Matrix<double>.Build.DenseOfArray(new double[,]
        {
            { 1 },
            { 3 },
            { 4 },
            { 11 },
        });
        Matrix<double> w = Matrix<double>.Build.DenseOfArray(new double[,] {
            { 1d/100d, 0, 0, 0 },
            { 0, 1d/400d, 0, 0 },
            { 0, 0, 1d/900d, 0 },
            { 0, 0, 0, 1d/6400d }  });

        var xt = x.Transpose();
        var a = xt * w * x;
        var an = a.Inverse();
        var b = xt * w * y;
        var r = an * b; // { -0.43925, 0.151713 }
        */
        /*
        Matrix<double> x = Matrix<double>.Build.DenseOfArray(new double[,]{
            { 1, 481, 481*481 },
            { 1, 571, 571*571 },
            { 1, 682, 682*682 },
        });
        Matrix<double> y = Matrix<double>.Build.DenseOfArray(new double[,]{
            { -19.3 },
            { 24.61 },
            { 79.36 },
        });
        Matrix<double> w = Matrix<double>.Build.DenseOfArray(new double[,] {
            { 1, 0, 0 },
            { 0, 1, 0 },
            { 0, 0, 1 },
        });
        var xt = x.Transpose();
        var a = xt * w * x;
        var an = a.Inverse();
        var b = xt * w * y;
        var r = an * b;
        */
        int n = xin.Length;
        var x = Matrix<double>.Build.Dense(rows: n, columns: pow + 1);
        for (int row = 0; row < n; ++row)
            for (int col = 0; col < x.ColumnCount; ++col)
                x[row, col] = Math.Pow(xin[row], col);

        var y = Matrix<double>.Build.Dense(rows: n, 1);
        for (int row = 0; row < n; ++row)
            y[row, 0] = yin[row];

        var w = Matrix<double>.Build.DiagonalIdentity(n);
        //w[0, 0] = 0.1;

        var xt = x.Transpose();
        var a = xt * w * x;
        var an = a.Inverse();
        var b = xt * w * y;
        var r = an * b;

        return [.. r.Column(0)];
    }

    public static double[] Polyfit1(int pow, double[] xin, double[] yin, double[] win)
    {
        int n = xin.Length;
        var x = Matrix<double>.Build.Dense(rows: n, columns: pow + 1);
        for (int row = 0; row < n; ++row)
            for (int col = 0; col < x.ColumnCount; ++col)
                x[row, col] = Math.Pow(xin[row], col);

        var y = Matrix<double>.Build.Dense(rows: n, 1);
        for (int row = 0; row < n; ++row)
            y[row, 0] = yin[row];

        var w = Matrix<double>.Build.DiagonalIdentity(n);

        for (int i = 0; i < n; i++)
            w[i, i] = win[i];

        var xt = x.Transpose();
        var a = xt * w * x;
        var an = a.Inverse();
        var b = xt * w * y;
        var r = an * b;

        return [.. r.Column(0)];
    }

    public static double[,] Polyfit2(
        int p_pow,
        int t_pow,
        double[] p_adc,
        double[] t_adc,
        double[] p_ref)
    {
        int K = p_ref.Length;
        int L = (p_pow + 1) * (t_pow + 1);

        var H = BuildDesignMatrix(p_pow, t_pow, p_adc, t_adc);
        var y = BuildTargetVector(p_ref);

        var svd = H.Svd(computeVectors: false);
        int rank = svd.S.Count(s => s > 1e-10 * svd.S[0]);
        ValidateForFit(K, p_pow, t_pow, rank);

        var qr = H.QR(QRMethod.Thin);
        var aVec = qr.R.Solve(qr.Q.Transpose() * y);

        var A = new double[p_pow + 1, t_pow + 1];
        for (int j = 0; j <= t_pow; j++)
            for (int i = 0; i <= p_pow; i++)
                A[i, j] = aVec[j * (p_pow + 1) + i];

        return A;
    }

    //public static double[,] Polyfit2lambda(
    //    int p_pow,
    //    int t_pow,
    //    double[] p_adc,
    //    double[] t_adc,
    //    double[] p_ref)
    //{
    //    int K = p_ref.Length;
    //    int L = (p_pow + 1) * (t_pow + 1);

    //    var H = BuildDesignMatrix(p_pow, t_pow, p_adc, t_adc);
    //    var y = BuildTargetVector(p_ref);

    //    if (K < L) throw new InvalidOperationException("K < L");

    //    var svd = H.Svd(computeVectors: false);
    //    int rank = svd.S.Count(s => s > 1e-10 * svd.S[0]);
    //    ValidateForFit(K, p_pow, t_pow, rank);

    //    var HtH = H.Transpose() * H;        
    //    var Hty = H.Transpose() * y;

    //    double lambda;
    //    double trace = 0;
    //    for (int d = 0; d < L; d++) trace += HtH[d, d];
    //    lambda = 1e-6 * trace / L;

    //    if (lambda > 0)
    //    {
    //        var I = Matrix<double>.Build.DenseIdentity(L);
    //        HtH = HtH + lambda * I;
    //    }

    //    Vector<double> aVec;
    //    try
    //    {
    //        aVec = HtH.Solve(Hty);           
    //    }
    //    catch (Exception)
    //    {
    //        aVec = HtH.PseudoInverse() * Hty;
    //    }

    //    var A = new double[p_pow + 1, t_pow + 1];
    //    for (int j = 0; j <= t_pow; j++)
    //        for (int i = 0; i <= p_pow; i++)
    //            A[i, j] = aVec[j * (p_pow + 1) + i];

    //    return A;
    //}

    public static bool HasHeteroskedasticity(
        double[] x, double[] y, int pow = 1,
        double alpha = 0.05, double dropFraction = 0.15)
    {
        int n = x.Length;
        int drop = (int)(n * dropFraction);
        int n1 = (n - drop) / 2;
        int n2 = n - drop - n1;

        if (n1 <= pow + 1 || n2 <= pow + 1)
            throw new ArgumentException(
                $"Слишком мало точек для теста Голдфелда–Квандта: " +
                $"n1={n1}, n2={n2}, нужно > {pow + 1} в каждой группе");

        var idx = Enumerable.Range(0, n).OrderBy(i => x[i]).ToArray();

        double[] x1 = new double[n1], y1 = new double[n1];
        for (int i = 0; i < n1; i++)
        {
            x1[i] = x[idx[i]];
            y1[i] = y[idx[i]];
        }

        double[] x2 = new double[n2], y2 = new double[n2];
        for (int i = 0; i < n2; i++)
        {
            x2[i] = x[idx[n - n2 + i]];
            y2[i] = y[idx[n - n2 + i]];
        }

        double rss1 = CalculateRss(x1, y1, pow);
        double rss2 = CalculateRss(x2, y2, pow);

        int df1 = n1 - (pow + 1);
        int df2 = n2 - (pow + 1);

        double mse1 = rss1 / df1;
        double mse2 = rss2 / df2;

        double F;
        double pValue;

        if (mse2 >= mse1)
        {
            F = mse2 / mse1;
            pValue = 2.0 * (1.0 - FisherSnedecor.CDF(df2, df1, F));
        }
        else
        {
            F = mse1 / mse2;
            pValue = 2.0 * (1.0 - FisherSnedecor.CDF(df1, df2, F));
        }

        pValue = Math.Min(1.0, pValue);

        return pValue < alpha;

    }

    public static double[] GetWeights(double[] x, double[] y, int pow)
    {
        int n = x.Length;

        var coeffs = Polyfit1(pow, x, y);

        double[] residuals = new double[n];
        for (int i = 0; i < n; i++)
        {
            double yPred = 0;
            for (int j = 0; j <= pow; j++)
                yPred += coeffs[j] * Math.Pow(x[i], j);
            residuals[i] = y[i] - yPred;
        }

        double[] sqRes = [.. residuals.Select(r => r * r)];

        var varCoeffs = Polyfit1(1, x, sqRes);

        double[] weights = new double[n];
        for (int i = 0; i < n; i++)
        {
            double variance = varCoeffs[0] + varCoeffs[1] * x[i];
            if (variance <= 0) variance = 1e-12;
            weights[i] = 1.0 / variance;
        }
        return weights;
    }

    public static double[] GetWeights(double[] x, double[] dx)
    {
        int n = dx.Length;

        double[] sqRes = [.. dx.Select(r => r * r)];

        var varCoeffs = Polyfit1(1, x, sqRes);

        double[] weights = new double[n];
        for (int i = 0; i < n; i++)
        {
            double variance = varCoeffs[0] + varCoeffs[1] * x[i];
            if (variance <= 0) variance = 1e-12;
            weights[i] = 1.0 / variance;
        }
        return weights;
    }

    private static double CalculateRss(double[] x, double[] y, int pow)
    {
        int n = x.Length;
        var X = Matrix<double>.Build.Dense(n, pow + 1);
        for (int i = 0; i < n; i++)
            for (int j = 0; j <= pow; j++)
                X[i, j] = Math.Pow(x[i], j);

        var Y = Vector<double>.Build.Dense(y);
        var Xt = X.Transpose();
        var beta = (Xt * X).Solve(Xt * Y);

        var residuals = X * beta - Y;
        return residuals.DotProduct(residuals);
    }

    internal static Matrix<double> BuildDesignMatrix(
        int p_pow,
        int t_pow,
        double[] p_adc,
        double[] t_adc)
    {
        int K = p_adc.Length;
        int L = (p_pow + 1) * (t_pow + 1);
        var H = Matrix<double>.Build.Dense(K, L);

        Span<double> upow = stackalloc double[p_pow + 1];
        Span<double> vpow = stackalloc double[t_pow + 1];

        for (int k = 0; k < K; k++)
        {
            double u = p_adc[k];
            double v = t_adc[k];

            upow[0] = 1.0;
            for (int i = 1; i <= p_pow; i++) upow[i] = upow[i - 1] * u;

            vpow[0] = 1.0;
            for (int j = 1; j <= t_pow; j++) vpow[j] = vpow[j - 1] * v;

            for (int j = 0; j <= t_pow; j++)
                for (int i = 0; i <= p_pow; i++)
                    H[k, j * (p_pow + 1) + i] = upow[i] * vpow[j];
        }
        return H;
    }

    private static Vector<double> BuildTargetVector(double[] p_ref)
    {
        int K = p_ref.Length;
        var y = Vector<double>.Build.Dense(K);
        for (int k = 0; k < K; k++) y[k] = p_ref[k];
        return y;
    }

    private static void ValidateForFit(int K, int n, int m, int rankH)
    {
        int L = (n + 1) * (m + 1);
        if (K < L)
            throw new InvalidOperationException(
                $"Точек {K} < коэффициентов {L}: система недоопределена. " +
                $"Уменьшите степени или добавьте точки.");
        if (rankH < L)
            throw new InvalidOperationException(
                $"rank(H)={rankH} < {L}: матрица вырождена, значения имеют линейную зависимость, либо лежат на одной прямой " +
                $"Уменьшите степени или добавьте РАЗНЫЕ точки.");
    }

    private static Vector<double> PackA(double[,] A)
    {
        int L = A.GetLength(0) * A.GetLength(1);
        var a = Vector<double>.Build.Dense(L);
        for (int j = 0; j < A.GetLength(1); j++)
            for (int i = 0; i < A.GetLength(0); i++)
                a[j * A.GetLength(0) + i] = A[i, j];
        return a;
    }
}
