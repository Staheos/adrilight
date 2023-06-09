using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace adrilight.Audio
{
    class AudioRecv
    {
        Int16[] values;
        public double fraction;
        public void StartRecording()
        {
            var waveIn = new NAudio.Wave.WaveInEvent {
                DeviceNumber = 4, // indicates which microphone to use
                WaveFormat = new NAudio.Wave.WaveFormat(rate: 44100, bits: 16, channels: 1),
                BufferMilliseconds = 20
            };
            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.StartRecording();

            //Console.WriteLine("C# Audio Level Meter");
            //Console.WriteLine("(press any key to exit)");
            //Console.ReadKey();
        }

        public void WaveIn_DataAvailable(object? sender, NAudio.Wave.WaveInEventArgs e)
        {
            // copy buffer into an array of integers
            this.values = new Int16[e.Buffer.Length / 2];
            Buffer.BlockCopy(e.Buffer, 0, values, 0, e.Buffer.Length);

            // determine the highest value as a fraction of the maximum possible value
            Console.WriteLine(this.fraction);

            double mic = (float)values.Max() / 32768 * 100;

            // lower volume
            mic *= 0.2;
            mic -= mic * 0.3;

            //if (mic <= 0.8)
            //{
            //    mic = 0.8;
            //}

            // desent
            this.fraction -= this.fraction * 0.13 + 0.08;

            if (this.fraction < mic)
            {
                this.fraction = mic;
            }

            if (this.fraction <= 0.7)
            {
                this.fraction = 0.7;
            }
            //this.fraction = mic;

            // print a level meter using the console
            //string bar = new('#', (int)(fraction * 70));
            //string meter = "[" + bar.PadRight(60, '-') + "]";
            //Console.CursorLeft = 0;
            //Console.CursorVisible = false;
            //Console.Write($"{meter} {fraction * 100:00.0}%");
        }
    }
}
