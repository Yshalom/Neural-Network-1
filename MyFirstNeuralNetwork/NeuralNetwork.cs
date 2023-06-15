using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using OpenTK.Compute.OpenCL;
using System.Windows.Forms;

namespace MyFirstNeuralNetwork
{
    class NeuralNetwork
    {
        private double Effective(double x)
        {
            return 1 / (1 + Math.Pow(Math.E, -x));
        }
        private double Derivative(double x)
        {
            return Effective(x) * (1 - Effective(x));
        }

        private int InputSize;
        private int[] HidenSize;
        private int OutputSize;

        private double[][] HidenBase;
        private double[] OutputBase;

        private double[][][] HidenWeight;
        private double[][] OutputWeight;

        public NeuralNetwork(int _InputSize, int[] _HidenSize, int _OutputSize)
        {
            //Copy Sizes
            InputSize = _InputSize;
            HidenSize = _HidenSize;
            OutputSize = _OutputSize;

            //Create Bases
            //hiden
            Random rand = new Random();
            List<double[]> _HidenBase = new List<double[]>();
            for (int i = 0; i < HidenSize.Length; i++)
            {
                List<double> layer = new List<double>();
                for (int j = 0; j < HidenSize[i]; j++)
                    layer.Add(rand.NextDouble() * 4 - 2);
                _HidenBase.Add(layer.ToArray());
            }
            HidenBase = _HidenBase.ToArray();
            //output
            List<double> _OutputBase = new List<double>();
            for (int i = 0; i < OutputSize; i++)
                _OutputBase.Add(rand.NextDouble() * 4 - 2);
            OutputBase = _OutputBase.ToArray();

            //Create Weight
            //hiden
            List<double[][]> _HidenWeight = new List<double[][]>();
            List<double[]> Layer = new List<double[]>();
            for (int i = 0; i < HidenSize[0]; i++)
            {
                List<double> LayerWeight = new List<double>();
                for (int j = 0; j < InputSize; j++)
                    LayerWeight.Add(rand.NextDouble() * 2 - 1);
                Layer.Add(LayerWeight.ToArray());
            }
            _HidenWeight.Add(Layer.ToArray());
            for (int i = 1; i < HidenSize.Length; i++)
            {
                Layer = new List<double[]>();
                for (int j = 0; j < HidenSize[i]; j++)
                {
                    List<double> LayerWeight = new List<double>();
                    for (int k = 0; k < HidenSize[i - 1]; k++)
                        LayerWeight.Add(rand.NextDouble() * 2 - 1);
                    Layer.Add(LayerWeight.ToArray());
                }
                _HidenWeight.Add(Layer.ToArray());
            }
            HidenWeight = _HidenWeight.ToArray();
            //output
            List<double[]> _OutputWeight = new List<double[]>();
            for (int i = 0; i < OutputSize; i++)
            {
                List<double> LayerWeight = new List<double>();
                for (int j = 0; j < HidenSize[HidenSize.Length - 1]; j++)
                    LayerWeight.Add(rand.NextDouble() * 2 - 1);
                _OutputWeight.Add(LayerWeight.ToArray());
            }
            OutputWeight = _OutputWeight.ToArray();
        }

        public NeuralNetwork(string Path)
        {
            BinaryReader file = new BinaryReader(File.OpenRead(Path));

            //read sizes
            InputSize = file.ReadInt32();

            int _HidenSizeLength = file.ReadInt32();
            List<int> _HidenSize = new List<int>();
            for (int i = 0; i < _HidenSizeLength; i++)
                _HidenSize.Add(file.ReadInt32());
            HidenSize = _HidenSize.ToArray();

            OutputSize = file.ReadInt32();

            //read bases
            int _HidenBaseLength = file.ReadInt32();
            List<double[]> _HidenBase = new List<double[]>();
            for (int i = 0; i < _HidenBaseLength; i++)
            {
                int LayerLength = file.ReadInt32();
                List<double> Layer = new List<double>();
                for (int j = 0; j < LayerLength; j++)
                    Layer.Add(file.ReadDouble());
                _HidenBase.Add(Layer.ToArray());
            }
            HidenBase = _HidenBase.ToArray();

            int _OutputBaseLength = file.ReadInt32();
            List<double> _OutputBase = new List<double>();
            for (int i = 0; i < _OutputBaseLength; i++)
                _OutputBase.Add(file.ReadDouble());
            OutputBase = _OutputBase.ToArray();

            //read wieght
            int _HidenWeightLength = file.ReadInt32();
            List<double[][]> _HidenWeight = new List<double[][]>();
            for (int i = 0; i < _HidenWeightLength; i++)
            {
                int LayerLength = file.ReadInt32();
                List<double[]> Layer = new List<double[]>();
                for (int j = 0; j < LayerLength; j++)
                {
                    int WeightLength = file.ReadInt32();
                    List<double> Weights = new List<double>();
                    for (int k = 0; k < WeightLength; k++)
                        Weights.Add(file.ReadDouble());
                    Layer.Add(Weights.ToArray());
                }
                _HidenWeight.Add(Layer.ToArray());
            }
            HidenWeight = _HidenWeight.ToArray();

            int _OutputWeightLength = file.ReadInt32();
            List<double[]> _OutputWeight = new List<double[]>();
            for (int i = 0; i < _OutputWeightLength; i++)
            {
                int LayerLength = file.ReadInt32();
                List<double> Layer = new List<double>();
                for (int j = 0; j < LayerLength; j++)
                    Layer.Add(file.ReadDouble());
                _OutputWeight.Add(Layer.ToArray());
            }
            OutputWeight = _OutputWeight.ToArray();

            file.Close();
        }

        ~NeuralNetwork()
        {
            //Release Kernel its objects
            if (IsKernelRight)
            {
                CL.ReleaseKernel(kernel);
                CL.ReleaseProgram(program);
                CL.ReleaseCommandQueue(queue);
                CL.ReleaseContext(context);
                CL.ReleaseDevice(devices[0]);

                CL.ReleaseMemoryObject(BInputSize);
                CL.ReleaseMemoryObject(BHidenSize);
                CL.ReleaseMemoryObject(BOutputSize);
                CL.ReleaseMemoryObject(BHidenSizeLength);
            }
        }

        public void Save(string Path)
        {
            BinaryWriter file = new BinaryWriter(File.OpenWrite(Path));

            //write sizes
            file.Write(InputSize);

            file.Write(HidenSize.Length);
            for (int i = 0; i < HidenSize.Length; i++)
                file.Write(HidenSize[i]);

            file.Write(OutputSize);

            //write bases
            file.Write(HidenBase.Length);
            for (int i = 0; i < HidenBase.Length; i++)
            {
                file.Write(HidenBase[i].Length);
                for (int j = 0; j < HidenBase[i].Length; j++)
                    file.Write(HidenBase[i][j]);
            }

            file.Write(OutputBase.Length);
            for (int i = 0; i < OutputBase.Length; i++)
                file.Write(OutputBase[i]);

            //write wieght
            file.Write(HidenWeight.Length);
            for (int i = 0; i < HidenWeight.Length; i++)
            {
                file.Write(HidenWeight[i].Length);
                for (int j = 0; j < HidenWeight[i].Length; j++)
                {
                    file.Write(HidenWeight[i][j].Length);
                    for (int k = 0; k < HidenWeight[i][j].Length; k++)
                        file.Write(HidenWeight[i][j][k]);
                }
            }

            file.Write(OutputWeight.Length);
            for (int i = 0; i < OutputWeight.Length; i++)
            {
                file.Write(OutputWeight[i].Length);
                for (int j = 0; j < OutputWeight[i].Length; j++)
                    file.Write(OutputWeight[i][j]);
            }

            file.Close();
        }

        public double[] Use(string Path)
        {
            return Use(GetData(Path));
        }
        public double[] Use(Bitmap input)
        {
            return Use(GetData(input));
        }
        public double[] Use(double[] input)
        {
            if (input.Length != InputSize)
                return new double[] { };

            List<List<double>> hiden = new List<List<double>>();
            hiden.Add(new List<double>());
            for (int i = 0; i < HidenSize[0]; i++)
            {
                double count = HidenBase[0][i];
                for (int j = 0; j < HidenWeight[0][i].Length; j++)
                    count += input[j] * HidenWeight[0][i][j];
                hiden[0].Add(Effective(count));
            }
            for (int i = 1; i < HidenSize.Length; i++)
            {
                hiden.Add(new List<double>());
                for (int j = 0; j < HidenSize[i]; j++)
                {
                    double count = HidenBase[i][j];
                    for (int k = 0; k < HidenWeight[i][j].Length; k++)
                        count += hiden[i - 1][k] * HidenWeight[i][j][k];
                    hiden[i].Add(Effective(count));
                }
            }
            List<double> output = new List<double>();
            for (int i = 0; i < OutputSize; i++)
            {
                double count = OutputBase[i];
                for (int j = 0; j < OutputWeight[i].Length; j++)
                    count += hiden[^1][j] * OutputWeight[i][j];
                output.Add(Effective(count));
            }

            return output.ToArray();
        }

        //This function takes the data and makes it useable.
        SortedList<string, double[]> dictionary = new SortedList<string, double[]>();
        private double[] GetData(string Path)
        {
            if (!dictionary.ContainsKey(Path))
                dictionary.Add(Path, GetData(new Bitmap(Path)));
            return dictionary[Path];
        }
        private double[] GetData(Bitmap input)
        {
            int SizeOfImage = (int)Math.Sqrt(InputSize);
            //Remember to change the number also in the other function (Form1.panel1_MouseUp).

            Bitmap res = new Bitmap(input, SizeOfImage, SizeOfImage);

            List<double> list = new List<double>();
            for (int i = 0; i < SizeOfImage; i++)
                for (int j = 0; j < SizeOfImage; j++)
                {
                    int color = res.GetPixel(i, j).R + res.GetPixel(i, j).G + res.GetPixel(i, j).B;
                    list.Add((double)color / 765);
                }
            return list.ToArray();
        }

        private struct Gradient {
            public double[][] GHidenBase;
            public double[] GOutputBase;
            
            public double[][][] GHidenWeight;
            public double[][] GOutputWeight;
        }

        private Gradient GetGradient(double[] Data, double[] Result)
        {
            //Use
            List<List<double>> hiden = new List<List<double>>();
            hiden.Add(new List<double>());
            for (int i = 0; i < HidenSize[0]; i++)
            {
                double count = HidenBase[0][i];
                for (int j = 0; j < HidenWeight[0][i].Length; j++)
                    count += Data[j] * HidenWeight[0][i][j];
                hiden[0].Add(count);
            }
            for (int i = 1; i < HidenSize.Length; i++)
            {
                hiden.Add(new List<double>());
                for (int j = 0; j < HidenSize[i]; j++)
                {
                    double count = HidenBase[i][j];
                    for (int k = 0; k < HidenWeight[i][j].Length; k++)
                        count += Effective(hiden[i - 1][k]) * HidenWeight[i][j][k];
                    hiden[i].Add(count);
                }
            }
            List<double> output = new List<double>();
            for (int i = 0; i < OutputSize; i++)
            {
                double count = OutputBase[i];
                for (int j = 0; j < OutputWeight[i].Length; j++)
                    count += Effective(hiden[^1][j]) * OutputWeight[i][j];
                output.Add(count);
            }

            //Create Gradient valuables
            Gradient g = new Gradient();
            g.GHidenBase = new double[HidenSize.Length][];
            for (int i = 0; i < g.GHidenBase.Length; i++)
                g.GHidenBase[i] = new double[HidenSize[i]];
            g.GOutputBase = new double[OutputSize];
            g.GHidenWeight = new double[HidenSize.Length][][];
            for (int i = 0; i < HidenSize.Length; i++)
            {
                g.GHidenWeight[i] = new double[HidenSize[i]][];
                for (int j = 0; j < HidenSize[i]; j++)
                    g.GHidenWeight[i][j] = new double[HidenWeight[i][j].Length];
            }
            g.GOutputWeight = new double[OutputSize][];
            for (int i = 0; i < OutputSize; i++)
                g.GOutputWeight[i] = new double[OutputWeight[i].Length];

            //Gradient calculat
            for (int i = 0; i < g.GOutputBase.Length; i++)
            {
                g.GOutputBase[i] = 2 * (Effective(output[i]) - Result[i]) * Derivative(output[i]);
                for (int j = 0; j < g.GOutputWeight[i].Length; j++)
                    g.GOutputWeight[i][j] = g.GOutputBase[i] * hiden[^1][j];
            }
            if (hiden.Count > 1)
            {
                for (int i = 0; i < g.GHidenBase[^1].Length; i++)
                {
                    g.GHidenBase[^1][i] = 0;
                    for (int j = 0; j < g.GOutputBase.Length; j++)
                        g.GHidenBase[^1][i] += g.GOutputBase[j] * OutputWeight[j][i];
                    g.GHidenBase[^1][i] *= Derivative(hiden[^1][i]);
                    for (int j = 0; j < g.GHidenWeight[^1][i].Length; j++)
                        g.GHidenWeight[^1][i][j] = g.GHidenBase[^1][i] * hiden[^2][j];
                }
                for (int L = 2; L < HidenSize.Length; L++)
                {
                    for (int i = 0; i < g.GHidenBase[^L].Length; i++)
                    {
                        g.GHidenBase[^L][i] = 0;
                        for (int j = 0; j < g.GHidenBase[^(L - 1)].Length; j++)
                            g.GHidenBase[^L][i] += g.GHidenBase[^(L - 1)][j] * HidenWeight[^(L - 1)][j][i];
                        g.GHidenBase[^L][i] *= Derivative(hiden[^L][i]);
                        for (int j = 0; j < g.GHidenWeight[^L][i].Length; j++)
                            g.GHidenWeight[^L][i][j] = g.GHidenBase[^L][i] * hiden[^(L + 1)][j];
                    }
                }

                for (int i = 0; i < g.GHidenBase[0].Length; i++)
                {
                    g.GHidenBase[0][i] = 0;
                    for (int j = 0; j < g.GHidenBase[1].Length; j++)
                        g.GHidenBase[0][i] += g.GHidenBase[1][j] * HidenWeight[1][j][i];
                    g.GHidenBase[0][i] *= Derivative(hiden[0][i]);
                    for (int j = 0; j < g.GHidenWeight[0][i].Length; j++)
                        g.GHidenWeight[0][i][j] = g.GHidenBase[0][i] * Data[j];
                }
            }
            else
            {
                for (int i = 0; i < g.GHidenBase[0].Length; i++)
                {
                    g.GHidenBase[0][i] = 0;
                    for (int j = 0; j < g.GOutputBase.Length; j++)
                        g.GHidenBase[0][i] += g.GOutputBase[j] * OutputWeight[j][i];
                    g.GHidenBase[0][i] *= Derivative(hiden[0][i]);
                    for (int j = 0; j < g.GHidenWeight[0][i].Length; j++)
                        g.GHidenWeight[0][i][j] = g.GHidenBase[0][i] * Data[j];
                }
            }

            return g;
        }

        private void programDebug(int[] InputSize, int[] HidenSize, int[] OutputSize,
            double[] HidenBase1D, double[] OutputBase,
            double[] HidenWeight1D, double[] OutputWeight1D,
            double[] DataAll1D, double[] ResultAll1D,
            int[] HidenSizeLength,
            ref double[] GHidenBaseAll1D, ref double[] GOutputBaseAll1D, ref double[] GHidenWeightAll1D, ref double[] GOutputWeightAll1D,
            ref double[] hidenAll1D, ref double[] outputAll1D, int[] GHidenWeight1DLength, int Index)
        {

            //hiden1D = hidenAll1D[Index];
            int Help = 0;
            for (int i = 0; i < HidenSizeLength[0]; i++)
                Help += HidenSize[i];
            double[] hiden1D = hidenAll1D.ToList().GetRange(Help * Index, Help).ToArray(); //hifrn1F = hidenAll1D[Index]
            double[] output1D = outputAll1D.ToList().GetRange(OutputSize[0] * Index, OutputSize[0]).ToArray(); //output1D = outputAll1D[Index]
            double[] GHidenBase1D = GHidenBaseAll1D.ToList().GetRange(Help * Index, Help).ToArray(); //GHidenBase1D = GHidenBaseAll1D[Index]
            double[] GOutputBase1D = GOutputBaseAll1D.ToList().GetRange(OutputSize[0] * Index, OutputSize[0]).ToArray(); //GOutputBase1D = GOutputBaseAll1D[Index]
            double[] GHidenWeight1D = GHidenWeightAll1D.ToList().GetRange(GHidenWeight1DLength[0] * Index, GHidenWeight1DLength[0]).ToArray(); //GHidenWeight1D = GHidenWeightAll1D[Index]
            double[] GOutputWeight1D = GOutputWeightAll1D.ToList().GetRange(HidenSize[HidenSizeLength[0] - 1] * OutputSize[0] * Index, HidenSize[HidenSizeLength[0] - 1] * OutputSize[0]).ToArray(); //GOutputWeightD = GOutputWeightAll1D[Index]
            double[] Data1D = DataAll1D.ToList().GetRange(Index * InputSize[0], InputSize[0]).ToArray();
            double[] Result1D = ResultAll1D.ToList().GetRange(Index * OutputSize[0], OutputSize[0]).ToArray();


            //Use
            int HelpHidenBase1D0 = 0;
            int HelpHidenWeight1D0 = 0;
            int Helphiden1D0 = 0;

            for (int i = 0; i < HidenSize[0]; i++)
            {
                double count = HidenBase1D[HelpHidenBase1D0++]; //double count = HidenBase[0][i];
                for (int j = 0; j < InputSize[0]; j++)
                    count += Data1D[j] * HidenWeight1D[HelpHidenWeight1D0++]; //count += Data[Index][j] * HidenWeight[0][i][j];
                hiden1D[Helphiden1D0++] = count; //hiden[0][i] = count;
            }
            int Helphiden1D1 = 0;
            for (int i = 1; i < HidenSizeLength[0]; i++)
            {
                for (int j = 0; j < HidenSize[i]; j++)
                {
                    double count = HidenBase1D[HelpHidenBase1D0++]; //double count = HidenBase[i][j];
                    for (int k = 0; k < HidenSize[i - 1]; k++)
                        count += Effective(hiden1D[Helphiden1D1 + k]) * HidenWeight1D[HelpHidenWeight1D0++]; //count += Effective(hiden[i - 1][k]) * HidenWeight[i][j][k];
                    hiden1D[Helphiden1D0++] = count; //hiden1D[i][j] = count;
                }
                Helphiden1D1 += HidenSize[i - 1];
            }

            int HelpOutputWeight1D0 = 0;
            for (int i = 0; i < OutputSize[0]; i++)
            {
                double count = OutputBase[i];
                for (int j = 0; j < HidenSize[HidenSizeLength[0] - 1]; j++)
                    count += Effective(hiden1D[Helphiden1D1 + j]) * OutputWeight1D[HelpOutputWeight1D0++]; //count += Effective(hiden[HidenSizeLength[0] - 1][j]) * OutputWeight[i][j];
                output1D[i] = count;
            }

            //Gradient calculat
            int HelpGOutputWeight1D0 = 0;
            Helphiden1D0 -= HidenSize[HidenSizeLength[0] - 1];
            for (int i = 0; i < OutputSize[0]; i++)
            {
                GOutputBase1D[i] = 2 * (Effective(output1D[i]) - Result1D[i]) * Derivative(output1D[i]); //GOutputBase[Index][i] = 2 * (Effective(output[i]) - Result[Index][i]) * Derivative(output[i]);
                for (int j = 0; j < HidenSize[HidenSizeLength[0] - 1]; j++)
                    GOutputWeight1D[HelpGOutputWeight1D0++] = GOutputBase1D[i] * hiden1D[Helphiden1D0 + j]; //GOutputWeight[Index][i][j] = GOutputBase[Index][i] * hiden[HidenSizeLength[0] - 1][j];
            }
            if (HidenSizeLength[0] > 1)
            {
                int HelGHidenBase1D0 = HelpHidenBase1D0 - HidenSize[HidenSizeLength[0] - 1];
                int HelpGHidenWeight1D0 = HelpHidenWeight1D0 - HidenSize[HidenSizeLength[0] - 1] * HidenSize[HidenSizeLength[0] - 2];
                int HelpGHidenWeight1D1 = 0;
                for (int i = 0; i < HidenSize[HidenSizeLength[0] - 1]; i++)
                {
                    GHidenBase1D[HelGHidenBase1D0 + i] = 0; //GHidenBase[Index][HidenSizeLength[0] - 1][i] = 0;
                    for (int j = 0; j < OutputSize[0]; j++)
                        GHidenBase1D[HelGHidenBase1D0 + i] += GOutputBase1D[j] * OutputWeight1D[j * HidenSize[HidenSizeLength[0] - 1] + i]; //GHidenBase[Index][HidenSizeLength[0] - 1][i] += GOutputBase[Index][j] * OutputWeight[j][i];
                    GHidenBase1D[HelGHidenBase1D0 + i] *= Derivative(hiden1D[HelGHidenBase1D0 + i]); //GHidenBase[Index][HidenSizeLength[0] - 1][i] *= Derivative(hiden[HidenSizeLength[0] - 1][i]);
                    for (int j = 0; j < HidenSize[HidenSizeLength[0] - 2]; j++)
                        GHidenWeight1D[HelpGHidenWeight1D0 + HelpGHidenWeight1D1++] = GHidenBase1D[HelGHidenBase1D0 + i] * hiden1D[HelGHidenBase1D0 - HidenSize[HidenSizeLength[0] - 2] + j]; //GHidenWeight[Index][HidenSizeLength[0] - 1][i][j] = GHidenBase[Index][HidenSizeLength[0] - 1][i] * hiden[HidenSizeLength[0] - 2][j];
                }
                int HelpHidenWeight1D1;
                for (int L = 2; L < HidenSizeLength[0]; L++)
                {
                    HelGHidenBase1D0 -= HidenSize[HidenSizeLength[0] - L];
                    HelpHidenWeight1D0 -= HidenSize[HidenSizeLength[0] - L + 1] * HidenSize[HidenSizeLength[0] - L];
                    HelpHidenWeight1D1 = HelpHidenWeight1D0 - HidenSize[HidenSizeLength[0] - L] * HidenSize[HidenSizeLength[0] - L - 1];
                    for (int i = 0; i < HidenSize[HidenSizeLength[0] - L]; i++)
                    {
                        GHidenBase1D[HelGHidenBase1D0 + i] = 0; //GHidenBase[Index][HidenSizeLength[0] - L][i] = 0;
                        for (int j = 0; j < HidenSize[HidenSizeLength[0] - L + 1]; j++)
                            GHidenBase1D[HelGHidenBase1D0 + i] += GHidenBase1D[HelGHidenBase1D0 + HidenSize[HidenSizeLength[0] - L] + j] * HidenWeight1D[HelpHidenWeight1D0 + j * HidenSize[HidenSizeLength[0] - L] + i]; //GHidenBase[Index][HidenSizeLength[0] - L][i] += GHidenBase[Index][HidenSizeLength[0] - L + 1][j] * HidenWeight[HidenSizeLength - L + 1][j][i];
                        GHidenBase1D[HelGHidenBase1D0 + i] *= Derivative(hiden1D[HelGHidenBase1D0 + i]); //GHidenBase[Index][HidenSizeLength[0] - L][i] *= Derivative(hiden[HidenSizeLength[0] - L][i]);
                        for (int j = 0; j < HidenSize[HidenSizeLength[0] - L]; j++)
                            GHidenWeight1D[HelpHidenWeight1D1++] = GHidenBase1D[HelGHidenBase1D0 + i] * hiden1D[HelGHidenBase1D0 - HidenSize[HidenSizeLength[0] - L - 1] + j]; //GHidenWeight[Index][HidenSizeLength[0] - L][i][j] = GHidenBase[Index][HidenSizeLength[0] - L][i] * hiden[HidenSizeLength - L - 1][j];
                    }
                }
                HelpHidenWeight1D0 -= HidenSize[1] * HidenSize[0];
                HelpHidenWeight1D1 = 0;
                for (int i = 0; i < HidenSize[0]; i++)
                {
                    GHidenBase1D[i] = 0; //GHidenBase[Index][0][i] = 0;
                    for (int j = 0; j < HidenSize[1]; j++)
                        GHidenBase1D[i] += GHidenBase1D[HidenSize[0] + j] * HidenWeight1D[HelpHidenWeight1D0 + j * HidenSize[0] + i]; //GHidenBase[Index][0][i] += GHidenBase[Index][1][j] * HidenWeight[1][j][i];
                    GHidenBase1D[i] *= Derivative(hiden1D[i]); //GHidenBase[Index][0][i] *= Derivative(hiden[0][i]);
                    for (int j = 0; j < InputSize[0]; j++)
                        GHidenWeight1D[HelpHidenWeight1D1++] = GHidenBase1D[i] * Data1D[j]; //GHidenWeight[Index][0][i][j] = GHidenBase[Index][0][i] * Data[Index][j];
                }
            }
            else
            {
                HelpHidenWeight1D0 -= HidenSize[1] * HidenSize[0];
                int HelpHidenWeight1D1 = 0;
                for (int i = 0; i < HidenSize[0]; i++)
                {
                    GHidenBase1D[i] = 0; //GHidenBase[Index][0][i] = 0;
                    for (int j = 0; j < OutputSize[0]; j++)
                        GHidenBase1D[i] += GOutputBase1D[j] * OutputWeight1D[j * HidenSize[0] + i]; //GHidenBase[Index][0][i] += GOutputBase[Index][j] * OutputWeight[j][i];
                    GHidenBase1D[i] *= Derivative(hiden1D[i]); //GHidenBase[Index][0][i] *= Derivative(hiden[0][i]);
                    for (int j = 0; j < InputSize[0]; j++)
                        GHidenWeight1D[HelpHidenWeight1D1++] = GHidenBase1D[i] * Data1D[j]; //GHidenWeight[Index][0][i][j] = GHidenBase[Index][0][i] * Data[Index][j];
                }
            }

            List<double> LGHidenBase1D = GHidenBaseAll1D.ToList();
            LGHidenBase1D.RemoveRange(Help * Index, Help);
            LGHidenBase1D.InsertRange(Help * Index, GHidenBase1D);
            GHidenBaseAll1D = LGHidenBase1D.ToArray();

            List<double> LGOutputBase1D = GOutputBaseAll1D.ToList();
            LGOutputBase1D.RemoveRange(OutputSize[0] * Index, OutputSize[0]);
            LGOutputBase1D.InsertRange(OutputSize[0] * Index, GOutputBase1D);
            GOutputBaseAll1D = LGOutputBase1D.ToArray();

            List<double> LGHidenWeight1D = GHidenWeightAll1D.ToList();
            LGHidenWeight1D.RemoveRange(GHidenWeight1DLength[0] * Index, GHidenWeight1DLength[0]);
            LGHidenWeight1D.InsertRange(GHidenWeight1DLength[0] * Index, GHidenWeight1D);
            GHidenWeightAll1D = LGHidenWeight1D.ToArray();

            List<double> LGOutputWeight1D = GOutputWeightAll1D.ToList();
            LGOutputWeight1D.RemoveRange(HidenSize[HidenSizeLength[0] - 1] * OutputSize[0] * Index, HidenSize[HidenSizeLength[0] - 1] * OutputSize[0]);
            LGOutputWeight1D.InsertRange(HidenSize[HidenSizeLength[0] - 1] * OutputSize[0] * Index, GOutputWeight1D);
            GOutputWeightAll1D = LGOutputWeight1D.ToArray();
        }

        private void ViewError(string log, string type)
        {
            MessageBox.Show(log, "שגיאה: " + type);
        }

        private static CLPlatform[] platforms;
        private static CLDevice[] devices;
        private static CLContext context;
        private static CLCommandQueue queue;
        private static CLProgram program;
        private static CLKernel kernel;
        private static bool IsKernelRight = false;
        private static CLBuffer BInputSize;
        private static CLBuffer BHidenSize;
        private static CLBuffer BOutputSize;
        private static CLBuffer BHidenSizeLength;
        private Gradient GetGradient(double[][] data, double[][] result)
        {
            // Create the Platform to GPU
            CLResultCode res;
            CLEvent clevent;

            if (!IsKernelRight)
            {
                res = CL.GetPlatformIds(out platforms);
                if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
                
                res = CL.GetDeviceIds(platforms[0], DeviceType.Gpu, out devices);
                if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

                ///////////////////////////////////
                int DeviceIndex = 0;
                for (int i = 0; i < devices.Length; i++)
                {
                    byte[] DeviceNameByte;
                    CL.GetDeviceInfo(devices[i], DeviceInfo.Name, out DeviceNameByte);
                    char[] DeviceNameChar = new char[DeviceNameByte.Length];
                    DeviceNameByte.CopyTo(DeviceNameChar, 0);
                    string DeviceNameString = new string(DeviceNameChar);

                    if (MessageBox.Show(DeviceNameString + "\n\nIf nothing will be chosen, the system will choose the first GPU!", (i + 1).ToString() + "/" + devices.Length.ToString(), MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        DeviceIndex = i;
                        break;
                    }
                }

                context = CL.CreateContext(IntPtr.Zero, 1, devices, IntPtr.Zero, IntPtr.Zero, out res);
                if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

                queue = CL.CreateCommandQueueWithProperties(context, devices[0], IntPtr.Zero, out res);
                if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

                string programSource = File.OpenText("..//..//..//program.c").ReadToEnd();
                program = CL.CreateProgramWithSource(context, programSource, out res);
                if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

                res = CL.BuildProgram(program, 1, devices, "", IntPtr.Zero, IntPtr.Zero);
                if (res != CLResultCode.Success)
                {
                    byte[] log;
                    CLResultCode res2 = CL.GetProgramBuildInfo(program, devices[0], ProgramBuildInfo.Log, out log);
                    if (res2 != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); };
                    char[] logchar = new char[log.Length];
                    for (int i = 0; i < log.Length; i++)
                        logchar[i] = (char)log[i];

                    ViewError(new string(logchar), res.ToString());
                }

                kernel = CL.CreateKernel(program, "GetGradient", out res);
                if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

                IsKernelRight = true;

                // Create the Memory

                BInputSize = CL.CreateBuffer(context, MemoryFlags.ReadOnly, (UIntPtr)sizeof(int), IntPtr.Zero, out res);
                if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
                res = CL.EnqueueWriteBuffer(queue, BInputSize, true, UIntPtr.Zero, new int[] { InputSize }, null, out clevent);
                if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

                BHidenSize = CL.CreateBuffer(context, MemoryFlags.ReadOnly, (UIntPtr)(sizeof(int) * HidenSize.Length), IntPtr.Zero, out res);
                if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
                res = CL.EnqueueWriteBuffer(queue, BHidenSize, true, UIntPtr.Zero, HidenSize, null, out clevent);
                if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

                BOutputSize = CL.CreateBuffer(context, MemoryFlags.ReadOnly, (UIntPtr)sizeof(int), IntPtr.Zero, out res);
                if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
                res = CL.EnqueueWriteBuffer(queue, BOutputSize, true, UIntPtr.Zero, new int[] { OutputSize }, null, out clevent);
                if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

                BHidenSizeLength = CL.CreateBuffer(context, MemoryFlags.ReadOnly, (UIntPtr)sizeof(int), IntPtr.Zero, out res);
                if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
                res = CL.EnqueueWriteBuffer(queue, BHidenSizeLength, true, UIntPtr.Zero, new int[] { HidenSize.Length }, null, out clevent);
                if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            }

            // Create the Memory - push network.
            List<double> HidenBase1D = new List<double>();
            for (int i = 0; i < HidenBase.Length; i++)
                HidenBase1D.AddRange(HidenBase[i]);
            CLBuffer BHidenBase = CL.CreateBuffer(context, MemoryFlags.ReadOnly, (UIntPtr)(sizeof(double) * HidenBase1D.Count), IntPtr.Zero, out res);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.EnqueueWriteBuffer(queue, BHidenBase, true, UIntPtr.Zero, HidenBase1D.ToArray(), null, out clevent);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

            CLBuffer BOutputBase = CL.CreateBuffer(context, MemoryFlags.ReadOnly, (UIntPtr)(sizeof(double) * OutputBase.Length), IntPtr.Zero, out res);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.EnqueueWriteBuffer(queue, BOutputBase, true, UIntPtr.Zero, OutputBase, null, out clevent);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

            List<double> HidenWeight1D = new List<double>();
            for (int i = 0; i < HidenWeight.Length; i++)
                for (int j = 0; j < HidenWeight[i].Length; j++)
                    HidenWeight1D.AddRange(HidenWeight[i][j]);
            CLBuffer BHidenWeight = CL.CreateBuffer(context, MemoryFlags.ReadOnly, (UIntPtr)(sizeof(double) * HidenWeight1D.Count), IntPtr.Zero, out res);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.EnqueueWriteBuffer(queue, BHidenWeight, true, UIntPtr.Zero, HidenWeight1D.ToArray(), null, out clevent);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

            List<double> OutputWeight1D = new List<double>();
            for (int i = 0; i < OutputWeight.Length; i++)
                OutputWeight1D.AddRange(OutputWeight[i]);
            CLBuffer BOutputWeight = CL.CreateBuffer(context, MemoryFlags.ReadOnly, (UIntPtr)(sizeof(double) * OutputWeight1D.Count), IntPtr.Zero, out res);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.EnqueueWriteBuffer(queue, BOutputWeight, true, UIntPtr.Zero, OutputWeight1D.ToArray(), null, out clevent);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

            // Create the Memory - push data.
            List<double> Data1D = new List<double>(data.Length);
            for (int i = 0; i < data.Length; i++)
                Data1D.AddRange(data[i]);
            CLBuffer BData = CL.CreateBuffer(context, MemoryFlags.ReadOnly, (UIntPtr)(sizeof(double) * Data1D.Count), IntPtr.Zero, out res);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.EnqueueWriteBuffer(queue, BData, true, UIntPtr.Zero, Data1D.ToArray(), null, out clevent);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

            List<double> Result1D = new List<double>();
            for (int i = 0; i < result.Length; i++)
                    Result1D.AddRange(result[i]);
            CLBuffer BResult = CL.CreateBuffer(context, MemoryFlags.ReadOnly, (UIntPtr)(sizeof(double) * Result1D.Count), IntPtr.Zero, out res);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.EnqueueWriteBuffer(queue, BResult, true, UIntPtr.Zero, Result1D.ToArray(), null, out clevent);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

            CLBuffer BGHidenBase = CL.CreateBuffer(context, MemoryFlags.ReadWrite, (UIntPtr)(sizeof(double) * HidenBase1D.Count * data.Length), IntPtr.Zero, out res);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

            CLBuffer BGOutputBase = CL.CreateBuffer(context, MemoryFlags.ReadWrite, (UIntPtr)(sizeof(double) * OutputBase.Length * data.Length), IntPtr.Zero, out res);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

            CLBuffer BGHidenWeight = CL.CreateBuffer(context, MemoryFlags.ReadWrite, (UIntPtr)(sizeof(double) * HidenWeight1D.Count * data.Length), IntPtr.Zero, out res);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

            CLBuffer BGOutputWeight = CL.CreateBuffer(context, MemoryFlags.ReadWrite, (UIntPtr)(sizeof(double) * OutputWeight1D.Count * data.Length), IntPtr.Zero, out res);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

            CLBuffer BhidenAll = CL.CreateBuffer(context, MemoryFlags.ReadWrite, (UIntPtr)(sizeof(double) * HidenBase1D.Count * data.Length), IntPtr.Zero, out res);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

            CLBuffer BoutputAll = CL.CreateBuffer(context, MemoryFlags.ReadWrite, (UIntPtr)(sizeof(double) * OutputBase.Length * data.Length), IntPtr.Zero, out res);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

            CLBuffer BGHidenWeight1DLength = CL.CreateBuffer(context, MemoryFlags.ReadOnly, (UIntPtr)sizeof(int), IntPtr.Zero, out res);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.EnqueueWriteBuffer(queue, BGHidenWeight1DLength, true, UIntPtr.Zero, new int[] { HidenWeight1D.Count }, null, out clevent);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

            //Set the memory

            res = CL.SetKernelArg(kernel, 0, BInputSize);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.SetKernelArg(kernel, 1, BHidenSize);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.SetKernelArg(kernel, 2, BOutputSize);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.SetKernelArg(kernel, 3, BHidenBase);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.SetKernelArg(kernel, 4, BOutputBase);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.SetKernelArg(kernel, 5, BHidenWeight);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.SetKernelArg(kernel, 6, BOutputWeight);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.SetKernelArg(kernel, 7, BData);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.SetKernelArg(kernel, 8, BResult);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.SetKernelArg(kernel, 9, BHidenSizeLength);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.SetKernelArg(kernel, 10, BGHidenBase);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.SetKernelArg(kernel, 11, BGOutputBase);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.SetKernelArg(kernel, 12, BGHidenWeight);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.SetKernelArg(kernel, 13, BGOutputWeight);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.SetKernelArg(kernel, 14, BhidenAll);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.SetKernelArg(kernel, 15, BoutputAll);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            res = CL.SetKernelArg(kernel, 16, BGHidenWeight1DLength);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

            //Execute the program

            UIntPtr[] globalWorkSize = new UIntPtr[] { (UIntPtr) data.Length };
            UIntPtr[] localWorkSize = new UIntPtr[] { (UIntPtr)1 };
            res = CL.EnqueueNDRangeKernel(queue, kernel, 1, new UIntPtr[1] { UIntPtr.Zero }, globalWorkSize, localWorkSize, 0, null, out clevent);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }

            //Get the memory

            double[] GHidenBase1D = new double[HidenBase1D.Count * data.Length];
            double[] GOutputBase1D = new double[OutputBase.Length * data.Length];
            double[] GHidenWeight1D = new double[HidenWeight1D.Count * data.Length];
            double[] GOutputWeight1D = new double[OutputWeight1D.Count * data.Length];
            //-----------------------------------------------------------------------
            //debuging
            //double[] hiden1D = new double[HidenBase1D.Count * data.Length];
            //double[] output1D = new double[OutputBase.Length * data.Length];
            //for (int i = 0; i < data.Length; i++)
            //    programDebug(new int[] { InputSize }, HidenSize, new int[] { OutputSize }, HidenBase1D.ToArray(), OutputBase, HidenWeight1D.ToArray(), OutputWeight1D.ToArray(), Data1D.ToArray(), Result1D.ToArray(), new int[] { HidenSize.Length }, ref GHidenBase1D, ref GOutputBase1D, ref GHidenWeight1D, ref GOutputWeight1D, ref hiden1D, ref output1D, new int[] { HidenWeight1D.Count }, i);
            //-----------------------------------------------------------------------
            
            res = CL.EnqueueReadBuffer(queue, BGHidenBase, true, UIntPtr.Zero, GHidenBase1D, null, out clevent);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            
            res = CL.EnqueueReadBuffer(queue, BGOutputBase, true, UIntPtr.Zero, GOutputBase1D, null, out clevent);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            
            res = CL.EnqueueReadBuffer(queue, BGHidenWeight, true, UIntPtr.Zero, GHidenWeight1D, null, out clevent);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            
            res = CL.EnqueueReadBuffer(queue, BGOutputWeight, true, UIntPtr.Zero, GOutputWeight1D, null, out clevent);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            
            //Close program
            
            res = CL.Finish(queue);
            if (res != CLResultCode.Success) { ViewError(res.ToString(), res.ToString()); throw new Exception("Error"); }
            
            //Release the momory
            
            CL.ReleaseMemoryObject(BData);
            CL.ReleaseMemoryObject(BResult);
            CL.ReleaseMemoryObject(BGHidenBase);
            CL.ReleaseMemoryObject(BGOutputBase);
            CL.ReleaseMemoryObject(BGHidenWeight);
            CL.ReleaseMemoryObject(BGOutputWeight);
            CL.ReleaseMemoryObject(BhidenAll);
            CL.ReleaseMemoryObject(BoutputAll);

            CL.ReleaseMemoryObject(BHidenBase);
            CL.ReleaseMemoryObject(BOutputBase);
            CL.ReleaseMemoryObject(BHidenWeight);
            CL.ReleaseMemoryObject(BOutputWeight);

            //Organize the gradients and return the average gradient.

            Gradient gradient = new Gradient();
            int GHidenBase1DIndex = 0;
            int GOutputBase1DIndex = 0;
            int GHidenWeight1DIndex = 0;
            int GOutputWeight1DIndex = 0;

            gradient.GHidenBase = new double[HidenSize.Length][];
            for (int j = 0; j < HidenSize.Length; j++)
            {
                gradient.GHidenBase[j] = new double[HidenSize[j]];
                for (int k = 0; k < HidenSize[j]; k++)
                    gradient.GHidenBase[j][k] = 0;
            }
            gradient.GOutputBase = new double[OutputSize];
            for (int j = 0; j < OutputSize; j++)
                gradient.GOutputBase[j] = 0;
            gradient.GHidenWeight = new double[HidenWeight.Length][][];
            for (int j = 0; j < HidenWeight.Length; j++)
            {
                gradient.GHidenWeight[j] = new double[HidenWeight[j].Length][];
                for (int k = 0; k < HidenWeight[j].Length; k++)
                {
                    gradient.GHidenWeight[j][k] = new double[HidenWeight[j][k].Length];
                    for (int l = 0; l < HidenWeight[j][k].Length; l++)
                        gradient.GHidenWeight[j][k][l] = 0;
                }
            }
            gradient.GOutputWeight = new double[OutputSize][];
            for (int j = 0; j < OutputSize; j++)
            {
                gradient.GOutputWeight[j] = new double[OutputWeight[j].Length];
                for (int k = 0; k < OutputWeight[j].Length; k++)
                    gradient.GOutputWeight[j][k] = 0;
            }

            //Sum all the gradients.
            for (int i = 0; i < data.Length; i++)
            {
                for (int j = 0; j < HidenSize.Length; j++)
                {
                    for (int k = 0; k < HidenSize[j]; k++)
                        gradient.GHidenBase[j][k] += GHidenBase1D[GHidenBase1DIndex++];
                }

                for (int j = 0; j < OutputSize; j++)
                    gradient.GOutputBase[j] += GOutputBase1D[GOutputBase1DIndex++];

                for (int j = 0; j < HidenWeight.Length; j++)
                {
                    for (int k = 0; k < HidenWeight[j].Length; k++)
                    {
                        for (int l = 0; l < HidenWeight[j][k].Length; l++)
                            gradient.GHidenWeight[j][k][l] += GHidenWeight1D[GHidenWeight1DIndex++];
                    }
                }

                for (int j = 0; j < OutputSize; j++)
                {
                    for (int k = 0; k < OutputWeight[j].Length; k++)
                        gradient.GOutputWeight[j][k] += GOutputWeight1D[GOutputWeight1DIndex++];
                }
            }

            //Div the main gradient into the number of the other gradients.
            {
                for (int j = 0; j < HidenSize.Length; j++)
                {
                    for (int k = 0; k < HidenSize[j]; k++)
                        gradient.GHidenBase[j][k] /= data.Length;
                }

                for (int j = 0; j < OutputSize; j++)
                    gradient.GOutputBase[j] /= data.Length;

                for (int j = 0; j < HidenWeight.Length; j++)
                {
                    for (int k = 0; k < HidenWeight[j].Length; k++)
                    {
                        for (int l = 0; l < HidenWeight[j][k].Length; l++)
                            gradient.GHidenWeight[j][k][l] /= data.Length;
                    }
                }

                for (int j = 0; j < OutputSize; j++)
                {
                    for (int k = 0; k < OutputWeight[j].Length; k++)
                        gradient.GOutputWeight[j][k] /= data.Length;
                }
            }

            return gradient;
        }

        public void TrainingCPU(string[] data, double[][] result)
        {
            if (data.Length != result.Length && data.Length >= 1)
                return;

            double[][] data2 = new double[data.Length][];
            for (int i = 0; i < data.Length; i++)
                data2[i] = GetData(data[i]);

            Gradient[] Gradients = new Gradient[data.Length];
            Parallel.For(0, data.Length, i =>
            {
                    Gradients[i] = GetGradient(data2[i], result[i]);
            });

            Gradient g = new Gradient();
            g.GHidenBase = Gradients[0].GHidenBase;
            g.GHidenWeight = Gradients[0].GHidenWeight;
            g.GOutputBase = Gradients[0].GOutputBase;
            g.GOutputWeight = Gradients[0].GOutputWeight;

            //avarage the gradient
            for (int i = 1; i < Gradients.Length; i++)
            {
                for (int j = 0; j < g.GHidenBase.Length; j++)
                    for (int k = 0; k < g.GHidenBase[j].Length; k++)
                        g.GHidenBase[j][k] += Gradients[i].GHidenBase[j][k];

                for (int j = 0; j < g.GHidenWeight.Length; j++)
                    for (int k = 0; k < g.GHidenWeight[j].Length; k++)
                        for (int l = 0; l < g.GHidenWeight[j][k].Length; l++)
                            g.GHidenWeight[j][k][l] = Gradients[i].GHidenWeight[j][k][l];

                for (int j = 0; j < g.GOutputBase.Length; j++)
                    g.GOutputBase[j] = Gradients[i].GOutputBase[j];

                for (int j = 0; j < g.GOutputWeight.Length; j++)
                    for (int k = 0; k < g.GOutputWeight[j].Length; k++)
                        g.GOutputWeight[j][k] += Gradients[i].GOutputWeight[j][k];
            }

            //change the neural network
            for (int j = 0; j < g.GHidenBase.Length; j++)
                for (int k = 0; k < g.GHidenBase[j].Length; k++)
                    HidenBase[j][k] -= g.GHidenBase[j][k] / Gradients.Length;

            for (int j = 0; j < g.GHidenWeight.Length; j++)
                for (int k = 0; k < g.GHidenWeight[j].Length; k++)
                    for (int l = 0; l < g.GHidenWeight[j][k].Length; l++)
                        HidenWeight[j][k][l] -= g.GHidenWeight[j][k][l] / Gradients.Length;

            for (int j = 0; j < g.GOutputBase.Length; j++)
                OutputBase[j] -= g.GOutputBase[j] / Gradients.Length;

            for (int j = 0; j < g.GOutputWeight.Length; j++)
                for (int k = 0; k < g.GOutputWeight[j].Length; k++)
                    OutputWeight[j][k] -= g.GOutputWeight[j][k] / Gradients.Length;
        }

        static int Div = 1;
        public void TrainingGPU(string[] data, double[][] result)
        {
            if (data.Length != result.Length && data.Length >= 1)
                return;

            double[][] data2 = new double[data.Length][];
            for (int i = 0; i < data.Length; i++)
                data2[i] = GetData(data[i]);
            double[][][] dataN = new double[Div][][];
            double[][][] resultN = new double[Div][][];

            if (Div > 1)
            {
                for (int i = 0; i < Div - 1; i++)
                {
                    dataN[i] = data2.ToList().GetRange(data.Length / 8 * i, data.Length / 8).ToArray();
                    resultN[i] = result.ToList().GetRange(result.Length / 8 * i, result.Length / 8).ToArray();
                }
                dataN[Div - 1] = data2.ToList().GetRange(data.Length / Div * (Div - 1), data.Length - data.Length / Div * (Div - 1)).ToArray();
                resultN[Div - 1] = result.ToList().GetRange(result.Length / Div * (Div - 1), result.Length - result.Length / Div * (Div - 1)).ToArray();
            }
            else
            {
                dataN[0] = data2;
                resultN[0] = result;
            }

            Gradient[] Gradients = new Gradient[Div];

            try
            {
                Parallel.For(0, Div, i =>
                {
                    Gradients[i] = GetGradient(dataN[i], resultN[i]);
                });
            }
            catch (Exception ex)
            {
                if (MessageBox.Show("?שגיאה של גלישה, האם תרצה להריץ שוב", "שגיאה", MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
                {
                    Div *= 2;
                    TrainingGPU(data, result);
                }
                return;
            }

            Gradient g = new Gradient();
            g.GHidenBase = Gradients[0].GHidenBase;
            g.GHidenWeight = Gradients[0].GHidenWeight;
            g.GOutputBase = Gradients[0].GOutputBase;
            g.GOutputWeight = Gradients[0].GOutputWeight;

            //avarage the gradient
            for (int i = 1; i < Gradients.Length; i++)
            {
                for (int j = 0; j < g.GHidenBase.Length; j++)
                    for (int k = 0; k < g.GHidenBase[j].Length; k++)
                        g.GHidenBase[j][k] += Gradients[i].GHidenBase[j][k];

                for (int j = 0; j < g.GHidenWeight.Length; j++)
                    for (int k = 0; k < g.GHidenWeight[j].Length; k++)
                        for (int l = 0; l < g.GHidenWeight[j][k].Length; l++)
                            g.GHidenWeight[j][k][l] = Gradients[i].GHidenWeight[j][k][l];

                for (int j = 0; j < g.GOutputBase.Length; j++)
                    g.GOutputBase[j] = Gradients[i].GOutputBase[j];

                for (int j = 0; j < g.GOutputWeight.Length; j++)
                    for (int k = 0; k < g.GOutputWeight[j].Length; k++)
                        g.GOutputWeight[j][k] += Gradients[i].GOutputWeight[j][k];
            }

            //change the neural network
            for (int j = 0; j < g.GHidenBase.Length; j++)
                for (int k = 0; k < g.GHidenBase[j].Length; k++)
                    HidenBase[j][k] -= g.GHidenBase[j][k] / Gradients.Length;

            for (int j = 0; j < g.GHidenWeight.Length; j++)
                for (int k = 0; k < g.GHidenWeight[j].Length; k++)
                    for (int l = 0; l < g.GHidenWeight[j][k].Length; l++)
                        HidenWeight[j][k][l] -= g.GHidenWeight[j][k][l] / Gradients.Length;

            for (int j = 0; j < g.GOutputBase.Length; j++)
                OutputBase[j] -= g.GOutputBase[j] / Gradients.Length;

            for (int j = 0; j < g.GOutputWeight.Length; j++)
                for (int k = 0; k < g.GOutputWeight[j].Length; k++)
                    OutputWeight[j][k] -= g.GOutputWeight[j][k] / Gradients.Length;
    }

    /// <summary>
    ///This function may cause the neural network to be Imbalance, use it carefully.
    ///Remember to run this function very few times.
    /// </summary>
    public int TrainingOnMistake(string[] data, double[][] result, Action<string[], double[][]> function)
        {
            if (data.Length != result.Length)
                return -1;

            bool[] IsMistake = new bool[data.Length];
            Parallel.For(0, data.Length, i =>
            {
                int index = 0;
                double[] I_Use = Use(data[i]);
                for (int j = 0; j < 10; j++)
                    if (I_Use[j] > I_Use[index])
                        index = j;

                if (result[i][index] != 1)
                    IsMistake[i] = true;
                else
                    IsMistake[i] = false;

            });

            List<string> NewData = new List<string>();
            List<double[]> NewResult = new List<double[]>();
            for (int i = 0; i < data.Length; i++)
            {
                if (IsMistake[i])
                {
                    NewData.Add(data[i]);
                    NewResult.Add(result[i]);
                }
            }

            function(NewData.ToArray(), NewResult.ToArray());

            return NewData.Count;
        }
    }
}