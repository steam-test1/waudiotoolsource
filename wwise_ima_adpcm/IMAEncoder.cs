using System;
using System.IO;

namespace wwise_ima_adpcm
{
	public class IMAEncoder
	{
		private int nextStepIndex;

		private readonly byte[] buffer;

		public IMAEncoder()
		{
			this.buffer = new byte[36];
		}

		public void Encode(BinaryReader inputReader, int channelCount, int sampleCount = 64)
		{
			int num = 2 * (channelCount - 1);
			int num1 = inputReader.ReadInt16();
			sampleCount--;
			this.buffer[1] = (byte)((num1 & 65280) >> 8);
			this.buffer[0] = (byte)(num1 & 255);
			this.buffer[2] = (byte)this.nextStepIndex;
			this.buffer[3] = 0;
			int num2 = 0;
			while (sampleCount > 0)
			{
				inputReader.BaseStream.Seek((long)num, SeekOrigin.Current);
				short num3 = inputReader.ReadInt16();
				sampleCount--;
				int stepTable = IMAConstants.StepTable[this.nextStepIndex];
				int num4 = this.EncodeSample(ref num1, num3, stepTable);
				this.nextStepIndex = IMAConstants.NextStepIndex(num4, this.nextStepIndex);
				int num5 = 0;
				if (sampleCount > 0)
				{
					inputReader.BaseStream.Seek((long)num, SeekOrigin.Current);
					short num6 = inputReader.ReadInt16();
					sampleCount--;
					stepTable = IMAConstants.StepTable[this.nextStepIndex];
					num5 = this.EncodeSample(ref num1, num6, stepTable);
					this.nextStepIndex = IMAConstants.NextStepIndex(num5, this.nextStepIndex);
				}
				this.buffer[4 + num2] = (byte)(num4 | num5 << 4);
				num2++;
			}
		}

		private int EncodeSample(ref int predictedSample, int inputSample, int stepSize)
		{
			int num = inputSample - predictedSample;
			int num1 = 0;
			if (num < 0)
			{
				num1 = 8;
				num = -num;
			}
			if (num >= stepSize)
			{
				num1 |= 4;
				num -= stepSize;
			}
			stepSize >>= 1;
			if (num >= stepSize)
			{
				num1 |= 2;
				num -= stepSize;
			}
			stepSize >>= 1;
			if (num >= stepSize)
			{
				num1 |= 1;
				num -= stepSize;
			}
			if ((num1 & 8) != 8)
			{
				predictedSample = inputSample - num + (stepSize >> 1);
			}
			else
			{
				predictedSample = inputSample + num - (stepSize >> 1);
			}
			if (predictedSample > 32767)
			{
				predictedSample = 32767;
			}
			else if (predictedSample < -32768)
			{
				predictedSample = -32768;
			}
			return num1;
		}

		public void WriteOut(BinaryWriter writer)
		{
			writer.Write(this.buffer);
			this.buffer.Initialize();
		}
	}
}