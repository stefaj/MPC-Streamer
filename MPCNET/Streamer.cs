using NAudio.Lame;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MPCNET
{
    public class Streamer
    {
        List<Tuple<TcpClient, NetworkStream, LameMP3FileWriter>> clients;

        NetworkStream stream;
        TcpListener listener;

        public int Port { get; private set; }
        
        public Streamer(int port = 801)
        {
            clients = new List<Tuple<TcpClient, NetworkStream, LameMP3FileWriter>>();
            this.Port = port;
        }

        public string GetInterface()
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }
            return localIP; 
        }

        public void Start()
        {
            listener = new TcpListener(Port);
            listener.Start();

            
            new Thread(() =>
            {
                while (true)
                {

                    try
                    {


                        TcpClient client = listener.AcceptTcpClient();

                        var netStream = client.GetStream();

                        var mp3Stream = new NAudio.Lame.LameMP3FileWriter(netStream, WaveFormat.CreateIeeeFloatWaveFormat(44100, 2), NAudio.Lame.LAMEPreset.ABR_320);

                        clients.Add(new Tuple<TcpClient, NetworkStream, LameMP3FileWriter>(client, netStream, mp3Stream));

                    }
                    catch
                    {

                    }
                }
            }).Start();
        }

        
        public void WriteData(byte[] buffer, int count)
        {
            foreach (var c in clients)
            {
                if (c.Item1.Connected)
                {

                    try
                    {
                        var stream = c.Item3;

                        stream.Write(buffer, 0, count);
                    }
                    catch
                    {

                    }
                }

            }

        }
    }
}
