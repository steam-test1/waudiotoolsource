using System;
using System.Collections.Generic;
using System.IO;

namespace wwise_ima_adpcm
{
	internal class Program
	{
		public Program()
		{
		}

		public static void Decode(string inFile, string outFile)
		{
			using (FileStream fileStream = new FileStream(inFile, FileMode.Open, FileAccess.Read))
			{
				int num = 0;
				BinaryReader binaryReader = new BinaryReader(fileStream);
				WAVHeader wAVHeader = WAVHeader.DecodeHeader(binaryReader);
				if (wAVHeader.Format == 2 || wAVHeader.BitsPerSample == 4 || wAVHeader.BlockAlignment == 36 * wAVHeader.ChannelCount)
				{
					using (FileStream fileStream1 = new FileStream(outFile, FileMode.Create, FileAccess.Write))
					{
						BinaryWriter binaryWriter = new BinaryWriter(fileStream1);
						WAVHeader wAVHeader1 = new WAVHeader()
						{
							Format = 1,
							ChannelCount = wAVHeader.ChannelCount,
							SampleRate = wAVHeader.SampleRate,
							BitsPerSample = 16
						};
						WAVHeader channelCount = wAVHeader1;
						channelCount.BlockAlignment = (ushort)(channelCount.ChannelCount * 2);
						channelCount.DataRate = channelCount.BlockAlignment * channelCount.SampleRate;
						WAVHeader.EncodeHeader(binaryWriter, channelCount);
						short[] numArray = new short[64 * channelCount.ChannelCount];
						for (int i = 0; i < (wAVHeader.DataLength / wAVHeader.BlockAlignment); i++)
						{
							for (int j = 0; j < wAVHeader.ChannelCount; j++)
							{
								IMADecoder.Decode(binaryReader, ref numArray, 1, j, (int)wAVHeader.ChannelCount);
							}
							short[] numArray1 = numArray;
							for (int k = 0; k < (int)numArray1.Length; k++)
							{
								binaryWriter.Write(numArray1[k]);
								num += 2;
							}
						}
						binaryWriter.Seek(4, SeekOrigin.Begin);
						binaryWriter.Write(36 + num);
						binaryWriter.Seek(40, SeekOrigin.Begin);
						binaryWriter.Write(num);
						binaryReader.Close();
						binaryWriter.Close();
					}
				}
				else
				{
					object[] format = new object[] { wAVHeader.Format, wAVHeader.BitsPerSample, wAVHeader.BlockAlignment, wAVHeader.SampleRate };
					Console.WriteLine("[ERROR] Invalid input file format. Expected Wwise IMA ADPCM. Found Format: {0}, BitsPerSample: {1}, BlockAlignment: {2}, SampleRate: {3}", format);
				}
			}
		}

		public static void Encode(string inFile, string outFile)
		{
			using (FileStream fileStream = new FileStream(inFile, FileMode.Open, FileAccess.Read))
			{
				int num = 0;
				BinaryReader binaryReader = new BinaryReader(fileStream);
				WAVHeader wAVHeader = WAVHeader.DecodeHeader(binaryReader);
				if (wAVHeader.BitsPerSample != 16 || wAVHeader.Format != 1)
				{
					Console.WriteLine("[ERROR] Input file must be a well formed signed 16bit PCM WAV file.");
				}
				else
				{
					using (FileStream fileStream1 = new FileStream(outFile, FileMode.Create, FileAccess.Write))
					{
						BinaryWriter binaryWriter = new BinaryWriter(fileStream1);
						WAVHeader wAVHeader1 = new WAVHeader()
						{
							Format = 2,
							ChannelCount = wAVHeader.ChannelCount,
							SampleRate = wAVHeader.SampleRate,
							BitsPerSample = 4
						};
						WAVHeader channelCount = wAVHeader1;
						channelCount.BlockAlignment = (ushort)(36 * channelCount.ChannelCount);
						channelCount.DataRate = channelCount.SampleRate / 64 * channelCount.BlockAlignment;
						WAVHeader.EncodeIMAHeader(binaryWriter, channelCount);
						IMAEncoder[] mAEncoder = new IMAEncoder[channelCount.ChannelCount];
						for (int i = 0; i < channelCount.ChannelCount; i++)
						{
							mAEncoder[i] = new IMAEncoder();
						}
						long position = binaryReader.BaseStream.Position;
						int blockAlignment = 64;
						int num1 = 0;
						while ((wAVHeader.DataLength - (blockAlignment - 64)) / wAVHeader.BlockAlignment > (long)64)
						{
							for (int j = 0; j < wAVHeader.ChannelCount; j++)
							{
								binaryReader.BaseStream.Seek(position + (long)(j * 2), SeekOrigin.Begin);
								mAEncoder[j].Encode(binaryReader, (int)wAVHeader.ChannelCount, 64);
								mAEncoder[j].WriteOut(binaryWriter);
								num += 36;
							}
							position += (long)(wAVHeader.BlockAlignment * 64);
							blockAlignment = blockAlignment + wAVHeader.BlockAlignment * 64;
							num1++;
						}
						long dataLength = (long)((wAVHeader.DataLength - (blockAlignment - 64)) / wAVHeader.BlockAlignment);
						for (int k = 0; dataLength > (long)0 && k < wAVHeader.ChannelCount; k++)
						{
							binaryReader.BaseStream.Seek(position + (long)(k * 2), SeekOrigin.Begin);
							mAEncoder[k].Encode(binaryReader, (int)wAVHeader.ChannelCount, (int)dataLength);
							mAEncoder[k].WriteOut(binaryWriter);
							num += 36;
						}
						binaryWriter.Seek(4, SeekOrigin.Begin);
						binaryWriter.Write((int)fileStream1.Length - 8);
						binaryWriter.Seek(60, SeekOrigin.Begin);
						binaryWriter.Write(num);
						binaryReader.Close();
						binaryWriter.Close();
					}
				}
			}
		}

		private static void Main(string[] args)
		{
			if ((int)args.Length == 0 || (int)args.Length == 2 || (int)args.Length > 3)
			{
				Console.WriteLine("Usage: wwise_ima_adpcm -d/-e <infile> <outfile>");
				Console.WriteLine("For multiple files: wwise_ima_adpcm -d_all/-e_all");
				return;
			}
			try
			{
				if (args[0] == "-d")
				{
					Program.Decode(args[1], args[2]);
				}
				else if (args[0] == "-e")
				{
					Program.Encode(args[1], args[2]);
				}
				else if (args[0] == "-e_all")
				{
					foreach (string str in Directory.EnumerateFiles(".", "*.wav"))
					{
						Console.WriteLine(string.Concat("Encoding: ", str.Replace(".\\", "")));
						Program.Encode(str, str.Replace(".wav", ".stream"));
					}
				}
				else if (args[0] != "-d_all")
				{
					Console.WriteLine("[ERROR] Must specify either -d to decode, or -e to encode.");
				}
				else
				{
					foreach (string str1 in Directory.EnumerateFiles(".", "*.stream"))
					{
						Console.WriteLine(string.Concat("Decoding: ", str1.Replace(".\\", "")));
						Program.Decode(str1, str1.Replace(".stream", ".wav"));
					}
				}
			}
			catch (Exception exception)
			{
				Console.WriteLine("[ERROR] Input File: {0} Exception occured: {1} Please report this error if it is not related to the input file not being a wave file.", args[1], exception.Message);
			}
		}
	}
}