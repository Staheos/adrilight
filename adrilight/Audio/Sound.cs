using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace adrilight.Audio
{
    class Fraction
    {
        int freq;
        double amp;
        public Fraction() 
        {

        }
    }
    class Sound
    {
        private int RATE = 44100;   // sample rate of the sound card
        public int BUFFERSIZE = 1024;  // Must be power of 2

        private int deviceNum = 0;
        private NAudio.Wave.WaveInEvent waveInEvent;
        public BufferedWaveProvider bwp;

        byte[] audioBytes;
        private bool recording = false;
        int nPoints;
        double[] data;
        Complex[] fftComplex;
        double[] hannWindow;
        public double[] fft = new double[1];

        
        public double[] fftReal = new double[1];

        Int16[] values;
        public double fraction;

        public Sound()
        {
            this.nPoints = BUFFERSIZE / 2; // whatever we measure must be a power of 2
            data = new double[this.nPoints]; // this is what we will measure
            GenerateHannWindow();
        }
        public double[]? GetData()
        {
            if (this.bwp.BufferedBytes < BUFFERSIZE)
            {
                return null;
            }

            audioBytes = new byte[BUFFERSIZE];
            this.bwp.Read(audioBytes, 0, BUFFERSIZE);

            for (int i = 0; i < BUFFERSIZE / 2; i++)
            {
                data[i] = BitConverter.ToInt16(audioBytes, i * 2);
            }

            this.fftComplex = new Complex[this.nPoints]; // the FFT function requires complex format
            for (int i = 0; i < data.Length; i++)
            {
                this.fftComplex[i] = new Complex(this.data[i], 0.0); // make it complex format
            }

            for (int i = 0; i < hannWindow.Length; i++)
            {
                fftComplex[i] *= hannWindow[i];
            }
            Accord.Math.FourierTransform.FFT(this.fftComplex, Accord.Math.FourierTransform.Direction.Forward);
            this.fft = new double[this.fftComplex.Length]; // this is where we will store the output (fft)

            for (int i = 0; i < this.fftComplex.Length; i++)
            {
                this.fft[i] = this.fftComplex[i].Magnitude; // back to double
            }
            return this.fft;
        }
        private void GenerateHannWindow()
        {
            this.hannWindow = new double[BUFFERSIZE / 2];
            var angleUnit = 2 * Math.PI / (this.hannWindow.Length - 1);
            for (int i = 0; i < this.hannWindow.Length; i++)
            {
                this.hannWindow[i] = 0.5 * (1 - Math.Cos(i * angleUnit));
            }
        }
        public void StartRecording()
        {
            this.recording = true;
            this.waveInEvent = new NAudio.Wave.WaveInEvent {
                DeviceNumber = this.deviceNum, // indicates which microphone to use
                WaveFormat = new NAudio.Wave.WaveFormat(rate: 44100, bits: 16, channels: 1),
                BufferMilliseconds = 20
            };
            this.bwp = new BufferedWaveProvider(this.waveInEvent.WaveFormat);
            this.bwp.BufferLength = BUFFERSIZE;
            this.bwp.DiscardOnBufferOverflow = true;

            this.waveInEvent.DataAvailable += WaveIn_DataAvailable;
            this.waveInEvent.StartRecording();
        }
        public void StopRecording()
        {
            this.waveInEvent.StopRecording();
            this.waveInEvent.Dispose(); 
        }
        public void WaveIn_DataAvailable(object? sender, NAudio.Wave.WaveInEventArgs e)
        {
            bwp.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }

        public void SetAudioDevice(string name)
        {
            int device_count = WaveIn.DeviceCount;
            for (int i = 0; i < device_count; i++)
            {
                var device = WaveIn.GetCapabilities(i);
                if (device.ProductName == name)
                {
                    this.deviceNum = i;
                    if (this.recording)
                    {
                        this.StopRecording();
                        this.StartRecording();
                    }
                }
            }
        }
    }
}
