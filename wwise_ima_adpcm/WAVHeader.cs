// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WAVHeader.cs" company="Zwagoth">
//   This code is released into the public domain by Zwagoth.
// </copyright>
// <summary>
//   TODO: Update summary.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace wwise_ima_adpcm
{
    using System;
    using System.Diagnostics;
    using System.IO;

    /// <summary>
    ///     TODO: Update summary.
    /// </summary>
    public struct WAVHeader
    {
        #region Fields

        /// <summary>
        /// The bits per sample.
        /// </summary>
        public ushort BitsPerSample;

        /// <summary>
        /// The block alignment.
        /// </summary>
        public ushort BlockAlignment;

        /// <summary>
        /// The channel count.
        /// </summary>
        public ushort ChannelCount;

        /// <summary>
        /// The data length.
        /// </summary>
        public uint DataLength;

        /// <summary>
        /// The data rate.
        /// </summary>
        public uint DataRate;

        /// <summary>
        /// The format.
        /// </summary>
        public ushort Format;

        /// <summary>
        /// The sample rate.
        /// </summary>
        public uint SampleRate;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The decode header.
        /// </summary>
        /// <param name="reader">
        /// The reader.
        /// </param>
        /// <returns>
        /// The <see cref="WAVHeader"/>.
        /// </returns>
        public static WAVHeader DecodeHeader(BinaryReader reader)
        {
            var header = new WAVHeader();
            if (reader.ReadInt32() != 0x46464952)
            {
                throw new Exception("Input file is not a WAVE file.");
            }
            reader.BaseStream.Seek(12, SeekOrigin.Begin);
            while (reader.ReadUInt32() != 0x20746D66U && reader.BaseStream.Position < reader.BaseStream.Length)
            {
                reader.BaseStream.Seek(reader.ReadInt32(), SeekOrigin.Current);
            }
            var nextSectionOffset = reader.ReadUInt32() + reader.BaseStream.Position;
            header.Format = reader.ReadUInt16();
            header.ChannelCount = reader.ReadUInt16();
            header.SampleRate = reader.ReadUInt32();
            header.DataRate = reader.ReadUInt32();
            header.BlockAlignment = reader.ReadUInt16();
            header.BitsPerSample = reader.ReadUInt16();
            reader.BaseStream.Seek(nextSectionOffset, SeekOrigin.Begin);
            while (reader.ReadUInt32() != 0x61746164U && reader.BaseStream.Position < reader.BaseStream.Length)
            {
                reader.BaseStream.Seek(reader.ReadInt32(), SeekOrigin.Current);
            }

            header.DataLength = reader.ReadUInt32();
            return header;
        }

        /// <summary>
        /// The encode header.
        /// </summary>
        /// <param name="writer">
        /// The writer.
        /// </param>
        /// <param name="header">
        /// The header.
        /// </param>
        public static void EncodeHeader(BinaryWriter writer, WAVHeader header)
        {
            writer.Write(0x46464952U);
            writer.Write(0);
            writer.Write(0x45564157U);
            writer.Write(0x20746D66U);
            writer.Write(16);
            writer.Write(header.Format);
            writer.Write(header.ChannelCount);
            writer.Write(header.SampleRate);
            writer.Write(header.DataRate);
            writer.Write(header.BlockAlignment);
            writer.Write(header.BitsPerSample);
            writer.Write(0x61746164U);
            writer.Write(0);
        }

        public static void EncodeIMAHeader(BinaryWriter writer, WAVHeader header)
        {
            writer.Write(0x46464952U);
            writer.Write(0);
            writer.Write(0x45564157U);
            writer.Write(0x20746D66U);
            writer.Write(24);
            writer.Write(header.Format);
            writer.Write(header.ChannelCount);
            writer.Write(header.SampleRate);
            writer.Write(header.DataRate);
            writer.Write(header.BlockAlignment);
            writer.Write(header.BitsPerSample);
            writer.Write(6);
            if(header.ChannelCount == 1)
                writer.Write(4);
            else
                writer.Write(3);
            writer.Write(0x4B4E554A);
            writer.Write(4);
            writer.Write(0);
            writer.Write(0x61746164U);
            writer.Write(0);

        }

        #endregion
    }
}