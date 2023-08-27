// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImaDecoder.cs" company="Zwagoth">
//   This code is released into the public domain by Zwagoth.
// </copyright>
// <summary>
//   Wwise IMA ADPCM Decoder.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace wwise_ima_adpcm
{
    using System.IO;

    /// <summary>
    ///     TODO: Update summary.
    /// </summary>
    public class IMADecoder
    {
        #region Public Methods and Operators

        /// <summary>
        /// The adjustment index.
        /// </summary>
        /// <param name="sample">
        /// The sample.
        /// </param>
        /// <param name="previousStep">
        /// The previous step.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>

        /// <summary>
        /// The decoder.
        /// </summary>
        /// <param name="inputStream">
        /// The input stream.
        /// </param>
        /// <param name="outputBuffer">
        /// The output buffer.
        /// </param>
        /// <param name="blocksToDecode">
        /// The blocks to decode.
        /// </param>
        /// <param name="channel">
        /// The channel.
        /// </param>
        public static void Decode(BinaryReader inputStream, ref short[] outputBuffer, int blocksToDecode, int channel, int channelCount)
        {
            int sampleNumber = 0;
            byte sample = 0;
            short seedSample = inputStream.ReadInt16();
            int seedStep = inputStream.ReadByte();
            inputStream.ReadByte(); // Alignment byte.
            short previousSample = seedSample;
            int previousStep = seedStep;
            outputBuffer[(channelCount * sampleNumber++) + channel] = seedSample;
            for (int i = 1; i < 64; ++i)
            {
                if ((i & 1) == 1)
                {
                    sample = inputStream.ReadByte();
                    previousSample = DecodeSample((byte)(sample & 0xF), previousSample, IMAConstants.StepTable[previousStep]);
                    previousStep = IMAConstants.NextStepIndex((byte)(sample & 0xF), previousStep);
                }
                else
                {
                    previousSample = DecodeSample((byte)(sample >> 4), previousSample, IMAConstants.StepTable[previousStep]);
                    previousStep = IMAConstants.NextStepIndex((byte)(sample >> 4), previousStep);
                }

                outputBuffer[(channelCount * sampleNumber++) + channel] = previousSample;
            }
        }

        /// <summary>
        /// The decode sample.
        /// </summary>
        /// <param name="sample">
        /// The sample.
        /// </param>
        /// <param name="previousSample">
        /// The previous sample.
        /// </param>
        /// <param name="step">
        /// The step.
        /// </param>
        /// <returns>
        /// The <see cref="short"/>.
        /// </returns>
        public static short DecodeSample(byte sample, short previousSample, int step)
        {
            int difference = step >> 3;
            if ((sample & 4) == 4)
            {
                difference += step;
            }
            if ((sample & 2) == 2)
            {
                difference += step >> 1;
            }
            if ((sample & 1) == 1)
            {
                difference += step >> 2;
            }
            if ((sample & 8) == 8)
            {
                difference = -difference;
            }

            int newSample = difference + previousSample;
            if (newSample > 32767)
            {
                newSample = 32767;
            }
            else if (newSample < -32768)
            {
                newSample = -32768;
            }

            return (short)newSample;
        }

        #endregion
    }
}