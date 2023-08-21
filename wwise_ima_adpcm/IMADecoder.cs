using System;
using System.IO;

namespace wwise_ima_adpcm
{
	public class IMADecoder
	{
		public IMADecoder()
		{
		}

		public static void Decode(BinaryReader inputStream, ref short[] outputBuffer, int blocksToDecode, int channel, int channelCount)
		{
			int num = 0;
			byte num1 = 0;
			short num2 = inputStream.ReadInt16();
			int num3 = inputStream.ReadByte();
			inputStream.ReadByte();
			short num4 = num2;
			int num5 = num3;
			int num6 = num;
			num = num6 + 1;
			outputBuffer[channelCount * num6 + channel] = num2;
			for (int i = 1; i < 64; i++)
			{
				if ((i & 1) != 1)
				{
					num4 = IMADecoder.DecodeSample((byte)(num1 >> 4), num4, IMAConstants.StepTable[num5]);
					num5 = IMAConstants.NextStepIndex((int)(num1 >> 4), num5);
				}
				else
				{
					num1 = inputStream.ReadByte();
					num4 = IMADecoder.DecodeSample((byte)(num1 & 15), num4, IMAConstants.StepTable[num5]);
					num5 = IMAConstants.NextStepIndex((int)(num1 & 15), num5);
				}
				int num7 = num;
				num = num7 + 1;
				outputBuffer[channelCount * num7 + channel] = num4;
			}
		}

		public static short DecodeSample(byte sample, short previousSample, int step)
		{
			int num = step >> 3;
			if ((sample & 4) == 4)
			{
				num += step;
			}
			if ((sample & 2) == 2)
			{
				num = num + (step >> 1);
			}
			if ((sample & 1) == 1)
			{
				num = num + (step >> 2);
			}
			if ((sample & 8) == 8)
			{
				num = -num;
			}
			int num1 = num + previousSample;
			if (num1 > 32767)
			{
				num1 = 32767;
			}
			else if (num1 < -32768)
			{
				num1 = -32768;
			}
			return (short)num1;
		}
	}
}