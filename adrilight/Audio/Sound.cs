using NAudio.CoreAudioApi;
using NAudio.Wave;
using NLog;
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
        private ILogger _log = LogManager.GetCurrentClassLogger();

        private int RATE = 44100;   // sample rate of the sound card
        public int BUFFERSIZE = 1024;  // Must be power of 2

        private int deviceNum = 0;
        private bool useOutputDevice = false;
        private MMDevice outputDevice;
        private IWaveIn waveInEvent;
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
        public bool IsCapturing => this.waveInEvent != null;

        public double[]? GetData()
        {
            if (this.bwp == null || this.bwp.BufferedBytes < BUFFERSIZE)
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
            this.StartCapture();
        }
        public void StopRecording()
        {
            this.recording = false;
            this.StopCapture();
        }
        private void StartCapture()
        {
            if (this.waveInEvent != null)
            {
                return;
            }

            try
            {
                if (this.useOutputDevice)
                {
                    var device = this.outputDevice;
                    if (device == null)
                    {
                        device = GetDefaultOutputDevice();
                    }
                    if (device == null)
                    {
                        _log.Info("No output audio device available, capture not started.");
                        return;
                    }
                    var loopback = new WasapiLoopbackCapture(device);
                    this.waveInEvent = loopback;
                    // loopback gives 32 bit float samples in the device mix format, they get converted to 16 bit mono in the callback
                    this.bwp = new BufferedWaveProvider(new NAudio.Wave.WaveFormat(rate: loopback.WaveFormat.SampleRate, bits: 16, channels: 1));
                }
                else
                {
                    if (WaveIn.DeviceCount == 0)
                    {
                        _log.Info("No input audio device available, capture not started.");
                        return;
                    }
                    this.waveInEvent = new NAudio.Wave.WaveInEvent {
                        DeviceNumber = this.deviceNum, // indicates which microphone to use
                        WaveFormat = new NAudio.Wave.WaveFormat(rate: 44100, bits: 16, channels: 1),
                        BufferMilliseconds = 20
                    };
                    this.bwp = new BufferedWaveProvider(this.waveInEvent.WaveFormat);
                }
                this.bwp.BufferLength = BUFFERSIZE;
                this.bwp.DiscardOnBufferOverflow = true;

                this.waveInEvent.DataAvailable += WaveIn_DataAvailable;
                this.waveInEvent.RecordingStopped += WaveIn_RecordingStopped;
                this.waveInEvent.StartRecording();
            }
            catch (Exception ex)
            {
                _log.Warn(ex, "Audio capture could not be started.");
                if (this.waveInEvent != null)
                {
                    this.waveInEvent.Dispose();
                    this.waveInEvent = null;
                }
            }
        }
        private void StopCapture()
        {
            if (this.waveInEvent == null)
            {
                return;
            }
            this.waveInEvent.DataAvailable -= WaveIn_DataAvailable;
            this.waveInEvent.RecordingStopped -= WaveIn_RecordingStopped;
            this.waveInEvent.StopRecording();
            this.waveInEvent.Dispose();
            this.waveInEvent = null;
        }
        public void WaveIn_DataAvailable(object? sender, NAudio.Wave.WaveInEventArgs e)
        {
            var capture = this.waveInEvent;
            if (capture == null)
            {
                return;
            }
            if (this.useOutputDevice)
            {
                // downmix float samples to 16 bit mono so GetData can stay the same
                int channels = capture.WaveFormat.Channels;
                int frames = e.BytesRecorded / 4 / channels;
                byte[] converted = new byte[frames * 2];
                for (int i = 0; i < frames; i++)
                {
                    float sample = 0;
                    for (int c = 0; c < channels; c++)
                    {
                        sample += BitConverter.ToSingle(e.Buffer, (i * channels + c) * 4);
                    }
                    sample /= channels;
                    if (sample > 1f)
                    {
                        sample = 1f;
                    }
                    if (sample < -1f)
                    {
                        sample = -1f;
                    }
                    Int16 pcm = (Int16)(sample * Int16.MaxValue);
                    converted[i * 2] = (byte)(pcm & 0xFF);
                    converted[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
                }
                bwp.AddSamples(converted, 0, converted.Length);
            }
            else
            {
                bwp.AddSamples(e.Buffer, 0, e.BytesRecorded);
            }
        }
        public void WaveIn_RecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception == null)
            {
                return;
            }
            _log.Warn(e.Exception, "Audio capture stopped unexpectedly, the device may have been disconnected.");
            var stopped = this.waveInEvent;
            this.waveInEvent = null;
            if (stopped != null)
            {
                stopped.DataAvailable -= WaveIn_DataAvailable;
                stopped.RecordingStopped -= WaveIn_RecordingStopped;
                stopped.Dispose();
            }
            if (this.recording)
            {
                // the selected device is gone, retry once on the current default
                this.outputDevice = null;
                this.StartCapture();
            }
        }

        public void SetAudioDevice(string name, bool useOutputDevice)
        {
            this.useOutputDevice = useOutputDevice;
            this.outputDevice = null;
            if (useOutputDevice)
            {
                var enumerator = new MMDeviceEnumerator();
                foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    if (device.FriendlyName == name)
                    {
                        this.outputDevice = device;
                    }
                }
                if (this.outputDevice == null)
                {
                    this.outputDevice = GetDefaultOutputDevice();
                }
            }
            else
            {
                int device_count = WaveIn.DeviceCount;
                for (int i = 0; i < device_count; i++)
                {
                    var device = WaveIn.GetCapabilities(i);
                    if (device.ProductName == name)
                    {
                        this.deviceNum = i;
                    }
                }
            }
            if (this.recording)
            {
                this.StopCapture();
                this.StartCapture();
            }
        }
        private MMDevice GetDefaultOutputDevice()
        {
            try
            {
                var enumerator = new MMDeviceEnumerator();
                return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
