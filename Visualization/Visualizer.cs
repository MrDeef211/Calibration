using Calibration;
using Plotly.NET.CSharp;

namespace Visualization;

public static class Visualizer
{
    public static void DrawPoints(double[] x, double[] y, string title = "title")
    {
        var chart = Chart.Point<double, double, string>(
            x: x,
            y: y,
            Name: title
        );


        chart.Show();
    }

    public static void DrawPoints(double[] x1, double[] x2, double[] y, string title = "title")
    {
        var chart = Chart.Point3D<double, double, double, string>(
            x: x1,
            y: x2,
            z: y,
            Name: title
        );

        chart.Show();
    }

    public static void DrawChart(double[] coeffs, string title = "title")
    {
        double[] x = new double[200];
        double[] y = new double[200];
        for (int i = 0; i < 200; i ++)
        {
            x[i] = -1 + i * 0.01;
            y[i] = MathTool.Calculate(coeffs, x[i]);
        }

        var chart = Chart.Line<double, double, string>(
            x: x,
            y: y,
            Name: title
        );

        chart.Show();
    }

    public static void DrawChart(double[,] coeffs, string title = "title")
    {
        double[] x1 = new double[200];
        double[] x2 = new double[200];
        double[][] y = new double[200][];
        for (int i = 0; i < 200; i++)
        {
            x1[i] = -1 + i * 0.01;
            y[i] = new double[200];
            for (int j = 0; j < 200; j++)
            {
                x2[j] = -1 + j * 0.01;
                y[i][j] = MathTool.Calculate(coeffs, x1[i], x2[j]);
            }
        }

        var chart = Chart.Surface<double, double, double, string>(
            X: x1,
            Y: x2,
            zData: y,
            Name: title
        );

        chart.Show();
    }

    public static void DrawBoth(double[] coeffs, double[] x, double[] y, string title1 = "title", string title2 = "title")
    {
        var chart1 = Chart.Point<double, double, string>(
            x: x,
            y: y,
            Name: title2
        );

        double[] x1 = new double[200];
        double[] y1 = new double[200];
        for (int i = 0; i < 200; i++)
        {
            x1[i] = -1 + i * 0.01;
            y1[i] = MathTool.Calculate(coeffs, x1[i]);
        }

        var chart2 = Chart.Line<double, double, string>(
            x: x1,
            y: y1,
            Name: title1
        );

        var chart = Chart.Combine([chart1, chart2]);

        chart.Show();
    }

    public static void DrawBoth(double[,] coeffs, double[] x1, double[] x2, double[] y, string title1 = "title", string title2 = "title")
    {
        var chart1 = Chart.Point3D<double, double, double, string>(
            x: x1,
            y: x2,
            z: y,
            Name: title2
        );

        double[] xс1 = new double[200];
        double[] xс2 = new double[200];
        double[][] yс = new double[200][];
        for (int i = 0; i < 200; i++)
        {
            xс1[i] = -1 + i * 0.01;
            yс[i] = new double[200];
            for (int j = 0; j < 200; j++)
            {
                xс2[j] = -1 + j * 0.01;
                yс[i][j] = MathTool.Calculate(coeffs, xс2[j], xс1[i]);
            }
        }

        var chart2 = Chart.Surface<double, double, double, string>(
            X: xс1,
            Y: xс2,
            zData: yс,
            Name: title1
        );


        var chart = Chart.Combine([chart1, chart2]);

        chart.Show();

    }

}
