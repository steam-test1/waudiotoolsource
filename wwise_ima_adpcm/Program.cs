// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Zwagoth">
//   This code is released into the public domain by Zwagoth.
// </copyright>
// <summary>
//   Decodes Wwise IMA ADPCM audio into PCM audio.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace wwise_ima_adpcm
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// The program.
    /// </summary>
    internal class Program
    {
        #region Methods

        /// <summary>
        /// The main.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void Main(string[] args)
        {
            if ((args.Length == 0) || (args.Length == 2) || (args.Length > 3))
            {
                Console.WriteLine("Usage: wwise_ima_adpcm -d/-e <infile> <outfile>");
                Console.WriteLine("For multiple files: wwise_ima_adpcm -d_all/-e_all");
                return;
            }

            try
            {
                if (args[0] == "-d")
                {
                    Decode(args[1], args[2]);
                }
                else if (args[0] == "-e")
                {
                    Encode(args[1], args[2]);
                }
                else if (args[0] == "-e_all")
                {
                    foreach (string file in Directory.EnumerateFiles(".", "*.wav"))
                    {
                        Console.WriteLine("Encoding: " + file.Replace(".\\", ""));
                        Encode(file, file.Replace(".wav", ".stream"));
                    }
                }
                else if (args[0] == "-d_all")
                {
                    foreach (string file in Directory.EnumerateFiles(".", "*.stream"))
                    {
                        Console.WriteLine("Decoding: " + file.Replace( ".\\", "" ));
                        Decode(file, file.Replace(".stream", ".wav"));
                    }
                }
                else
                {
                    Console.WriteLine("[ERROR] Must specify either -d to decode, or -e to encode.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "[ERROR] Input File: {0} Exception occured: {1} Please report this error if it is not related to the input file not being a wave file.",
                    args[1],
                    e.Message);
            }
        }

        public static void Decode(string inFile, string outFile)
        {
            using (var inStream = new FileStream(inFile, FileMode.Open, FileAccess.Read))
            {
                int dataSize = 0;
                var reader = new BinaryReader(inStream);
                WAVHeader header = WAVHeader.DecodeHeader(reader);
                if (header.Format != 2 && header.BitsPerSample != 4
                    && header.BlockAlignment != (36 * header.ChannelCount))
                {
                    Console.WriteLine(
                        "[ERROR] Invalid input file format. Expected Wwise IMA ADPCM. Found Format: {0}, BitsPerSample: {1}, BlockAlignment: {2}, SampleRate: {3}",
                        header.Format,
                        header.BitsPerSample,
                        header.BlockAlignment,
                        header.SampleRate);
                    return;
                }
                using (var outStream = new FileStream(outFile, FileMode.Create, FileAccess.Write))
                {
                    var writer = new BinaryWriter(outStream);
                    var newHeader = new WAVHeader
                                        {
                                            Format = 1,
                                            ChannelCount = header.ChannelCount,
                                            SampleRate = header.SampleRate,
                                            BitsPerSample = 16
                                        };
                    newHeader.BlockAlignment = (ushort)(newHeader.ChannelCount * 2);
                    newHeader.DataRate = newHeader.BlockAlignment * newHeader.SampleRate;
                    WAVHeader.EncodeHeader(writer, newHeader);
                    var outBuffer = new short[64 * newHeader.ChannelCount];
                    for (int i = 0; i < header.DataLength / header.BlockAlignment; ++i)
                    {
                        for (int channel = 0; channel < header.ChannelCount; ++channel)
                        {
                            IMADecoder.Decode(reader, ref outBuffer, 1, channel, header.ChannelCount);
                        }

                        foreach (short sample in outBuffer)
                        {
                            writer.Write(sample);
                            dataSize += 2;
                        }
                    }

                    writer.Seek(4, SeekOrigin.Begin);
                    writer.Write(4 + 24 + 8 + dataSize);
                    writer.Seek(0x28, SeekOrigin.Begin);
                    writer.Write(dataSize);
                    reader.Close();
                    writer.Close();
                }
            }
        }

        public static void Encode(string inFile, string outFile)
        {
            using (var inStream = new FileStream(inFile, FileMode.Open, FileAccess.Read))
            {
                int dataSize = 0;
                var reader = new BinaryReader(inStream);
                WAVHeader header = WAVHeader.DecodeHeader(reader);
                if (header.BitsPerSample != 16 || header.Format != 1)
                {
                    Console.WriteLine("[ERROR] Input file must be a well formed signed 16bit PCM WAV file.");
                    return;
                }
                using (var outStream = new FileStream(outFile, FileMode.Create, FileAccess.Write))
                {
                    var writer = new BinaryWriter(outStream);
                    var newHeader = new WAVHeader
                                        {
                                            Format = 2,
                                            ChannelCount = header.ChannelCount,
                                            SampleRate = header.SampleRate,
                                            BitsPerSample = 4
                                        };
                    newHeader.BlockAlignment = (ushort)(36 * newHeader.ChannelCount);
                    newHeader.DataRate = (newHeader.SampleRate / 64) * newHeader.BlockAlignment;
                    WAVHeader.EncodeIMAHeader(writer, newHeader);
                    var encoders = new IMAEncoder[newHeader.ChannelCount];
                    for (int i = 0; i < newHeader.ChannelCount; ++i)
                    {
                        encoders[i] = new IMAEncoder();
                    }
                    long basePosition = reader.BaseStream.Position;
                    int currentPosition = 64;
                    for (int i = 0; (header.DataLength - (currentPosition - 64)) / header.BlockAlignment > 64; ++i)
                    {
                        for (int channel = 0; channel < header.ChannelCount; ++channel)
                        {
                            reader.BaseStream.Seek(basePosition + (channel * 2), SeekOrigin.Begin);
                            encoders[channel].Encode(reader, header.ChannelCount);
                            encoders[channel].WriteOut(writer);
                            dataSize += 36;
                        }
                        basePosition += header.BlockAlignment * 64;
                        currentPosition += (header.BlockAlignment * 64);
                    }
                    var remainingSamples = (header.DataLength - (currentPosition - 64)) / header.BlockAlignment;
                    for (int channel = 0; remainingSamples > 0 && channel < header.ChannelCount; ++channel)
                    {
                        reader.BaseStream.Seek(basePosition + (channel * 2), SeekOrigin.Begin);
                        encoders[channel].Encode(reader, header.ChannelCount, (int)remainingSamples);
                        encoders[channel].WriteOut(writer);
                        dataSize += 36;
                    }

                    writer.Seek(4, SeekOrigin.Begin);
                    writer.Write((int)outStream.Length - 8);
                    writer.Seek(0x3C, SeekOrigin.Begin);
                    writer.Write(dataSize);
                    reader.Close();
                    writer.Close();
                }
            }
        }

        #endregion
    }
}