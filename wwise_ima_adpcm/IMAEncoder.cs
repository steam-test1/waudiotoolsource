// -----------------------------------------------------------------------
// <copyright file="IMAEncoder.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace wwise_ima_adpcm
{
    using System.IO;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class IMAEncoder
    {
        private int nextStepIndex;

        private readonly byte[] buffer;

        public IMAEncoder()
        {
            this.buffer = new byte[36];
        }

        public void WriteOut(BinaryWriter writer)
        {
            writer.Write(this.buffer);
            this.buffer.Initialize();
        }

        private int EncodeSample(ref int predictedSample, int inputSample, int stepSize)
        {
            int difference = inputSample - predictedSample;
            int encodedSample = 0;
            if (difference < 0)
            {
                encodedSample = 8;
                difference = -difference;
            }

            if (difference >= stepSize)
            {
                encodedSample |= 4;
                difference -= stepSize;
            }

            stepSize >>= 1;
            if (difference >= stepSize)
            {
                encodedSample |= 2;
                difference -= stepSize;
            }

            stepSize >>= 1;
            if (difference >= stepSize)
            {
                encodedSample |= 1;
                difference -= stepSize;
            }

            if ((encodedSample & 8) == 8)
            {
                predictedSample = inputSample + difference - (stepSize >> 1);
            }
            else
            {
                predictedSample = inputSample - difference + (stepSize >> 1);
            }

            if (predictedSample > 32767) predictedSample = 32767;
            else if (predictedSample < -32768) predictedSample = -32768;
            return encodedSample;
        }

        public void Encode(BinaryReader inputReader, int channelCount, int sampleCount = 64)
        {
            int advance = 2 * (channelCount-1);
            int predictedSample = inputReader.ReadInt16();
            --sampleCount;
            buffer[1] = (byte)((predictedSample & 0xff00) >> 8);
            buffer[0] = (byte)(predictedSample & 0xff);
            buffer[2] = (byte)this.nextStepIndex;
            buffer[3] = 0;

            for (int i = 0; sampleCount > 0; ++i)
            {
                inputReader.BaseStream.Seek(advance, SeekOrigin.Current);
                var sample1 = inputReader.ReadInt16();
                --sampleCount;
                int step = IMAConstants.StepTable[this.nextStepIndex];
                int encodedSample1 = this.EncodeSample(ref predictedSample, sample1, step);
                this.nextStepIndex = IMAConstants.NextStepIndex(encodedSample1, this.nextStepIndex);
                int encodedSample2 = 0;
                if (sampleCount > 0)
                {
                    inputReader.BaseStream.Seek(advance, SeekOrigin.Current);
                    var sample2 = inputReader.ReadInt16();
                    --sampleCount;
                    step = IMAConstants.StepTable[this.nextStepIndex];
                    encodedSample2 = this.EncodeSample(ref predictedSample, sample2, step);
                    this.nextStepIndex = IMAConstants.NextStepIndex(encodedSample2, this.nextStepIndex);
                }
                byte outputSample = (byte)(encodedSample1 | (encodedSample2 << 4));
                buffer[4 + i] = (byte)(encodedSample1 | (encodedSample2 << 4));
            }
        }
    }
}
