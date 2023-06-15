double Effective(double x)
{
    return 1 / (1 + pow(2.71828182845904, -x));
}
double Derivative(double x)
{
    return Effective(x) * (1 - Effective(x));
}

__kernel void GetGradient(__constant int* InputSize, __constant int* HidenSize, __constant int* OutputSize,
	__constant double* HidenBase1D, __constant double* OutputBase,
	__constant double* HidenWeight1D, __constant double* OutputWeight1D,
	__constant double* DataAll1D, __constant double* ResultAll1D,
	__constant int* HidenSizeLength,
	__global double* GHidenBaseAll1D, __global double* GOutputBaseAll1D, __global double* GHidenWeightAll1D, __global double* GOutputWeightAll1D,
	__global double* hidenAll1D, __global double* outputAll1D, __constant int* GHidenWeight1DLength)
{
	int Index = get_global_id(0);


	//hiden1D = hidenAll1D[Index];
	int Help = 0;
	for (int i = 0; i < HidenSizeLength[0]; i++)
		Help += HidenSize[i];
	__global double* hiden1D = hidenAll1D + Help * Index; //hifrn1F = hidenAll1D[Index]
	__global double* output1D = outputAll1D + OutputSize[0] * Index; //output1D = outputAll1D[Index]
	__global double* GHidenBase1D = GHidenBaseAll1D + Help * Index; //GHidenBase1D = GHidenBaseAll1D[Index]
	__global double* GOutputBase1D = GOutputBaseAll1D + OutputSize[0] * Index; //GOutputBase1D = GOutputBaseAll1D[Index]
	__global double* GHidenWeight1D = GHidenWeightAll1D + GHidenWeight1DLength[0] * Index; //GHidenWeight1D = GHidenWeightAll1D[Index]
	__global double* GOutputWeight1D = GOutputWeightAll1D + HidenSize[HidenSizeLength[0] - 1] * OutputSize[0] * Index; //GOutputWeightD = GOutputWeightAll1D[Index]
	__constant double* Data1D = DataAll1D + Index * InputSize[0];
	__constant double* Result1D = ResultAll1D + Index * OutputSize[0];

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
}