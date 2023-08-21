using System;
using System.IO;

namespace wwise_ima_adpcm
{
	public struct WAVHeader
	{
		public ushort BitsPerSample;

		public ushort BlockAlignment;

		public ushort ChannelCount;

		public uint DataLength;

		public uint DataRate;

		public ushort Format;

		public uint SampleRate;

		public static WAVHeader DecodeHeader(BinaryReader reader)
		{
			WAVHeader wAVHeader = new WAVHeader();
			if (reader.ReadInt32() != 1179011410)
			{
				throw new Exception("Input file is not a WAVE file.");
			}
			reader.BaseStream.Seek((long)12, SeekOrigin.Begin);
			while (reader.ReadUInt32() != 544501094 && reader.BaseStream.Position < reader.BaseStream.Length)
			{
				reader.BaseStream.Seek((long)reader.ReadInt32(), SeekOrigin.Current);
			}
			long num = reader.ReadUInt32() + (reader.BaseStream.Position);
			wAVHeader.Format = reader.ReadUInt16();
			wAVHeader.ChannelCount = reader.ReadUInt16();
			wAVHeader.SampleRate = reader.ReadUInt32();
			wAVHeader.DataRate = reader.ReadUInt32();
			wAVHeader.BlockAlignment = reader.ReadUInt16();
			wAVHeader.BitsPerSample = reader.ReadUInt16();
			reader.BaseStream.Seek(num, SeekOrigin.Begin);
			while (reader.ReadUInt32() != 1635017060 && reader.BaseStream.Position < reader.BaseStream.Length)
			{
				reader.BaseStream.Seek((long)reader.ReadInt32(), SeekOrigin.Current);
			}
			wAVHeader.DataLength = reader.ReadUInt32();
			return wAVHeader;
		}

		public static void EncodeHeader(BinaryWriter writer, WAVHeader header)
		{
			writer.Write((uint)1179011410);
			writer.Write(0);
			writer.Write((uint)1163280727);
			writer.Write((uint)544501094);
			writer.Write(16);
			writer.Write(header.Format);
			writer.Write(header.ChannelCount);
			writer.Write(header.SampleRate);
			writer.Write(header.DataRate);
			writer.Write(header.BlockAlignment);
			writer.Write(header.BitsPerSample);
			writer.Write((uint)1635017060);
			writer.Write(0);
		}

		public static void EncodeIMAHeader(BinaryWriter writer, WAVHeader header)
		{
			writer.Write((uint)1179011410);
			writer.Write(0);
			writer.Write((uint)1163280727);
			writer.Write((uint)544501094);
			writer.Write(24);
			writer.Write(header.Format);
			writer.Write(header.ChannelCount);
			writer.Write(header.SampleRate);
			writer.Write(header.DataRate);
			writer.Write(header.BlockAlignment);
			writer.Write(header.BitsPerSample);
			writer.Write(6);
			if (header.ChannelCount != 1)
			{
				writer.Write(3);
			}
			else
			{
				writer.Write(4);
			}
			writer.Write(1263424842);
			writer.Write(4);
			writer.Write(0);
			writer.Write((uint)1635017060);
			writer.Write(0);
		}
	}
}