using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Lame;

namespace MPCNET
{
    public delegate void DataReceived(byte[] data, int length);
    class Recorder
    {
        WasapiLoopbackCapture waveIn;
        WaveFileWriter writer;

        

        public event DataReceived OnAudioData;

        public Recorder()
        {
            waveIn = new WasapiLoopbackCapture();
            waveIn.DataAvailable += InputBufferToFileCallback;

           
        }


        
        public void Start()
        {
            
            waveIn.StartRecording();

            writer = new WaveFileWriter("save.wav", WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));

  

            


        }

        public void Stop()
        {
            waveIn.StopRecording();
        }

        public void InputBufferToFileCallback(object sender, WaveInEventArgs e)
        {
            writer.Write(e.Buffer,0, e.BytesRecorded);
         //   Console.WriteLine(e.BytesRecorded);


            if (OnAudioData != null)
                OnAudioData(e.Buffer, e.BytesRecorded);


        }
    }
}
