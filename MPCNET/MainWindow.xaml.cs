using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Libmpc;
using System.Net;
using MPD;
using System.Reflection.Emit;
using MahApps.Metro.Controls;
using NAudio.Wave;
using Microsoft.Win32;
using System.Threading;
using System.Collections.ObjectModel;

namespace MPCNET
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public Player player;
        Recorder recorder;
        Streamer streamer;


        public ObservableCollection<MusicInfo> Playlist = null;



        public MainWindow()
        {
            InitializeComponent();
            recorder = new Recorder();
            streamer = new Streamer();
            player = new Player();
            player.OnStatusUpdate += player_OnStatusUpdate;

        }

        void player_OnStatusUpdate(StatusInfo info)
        {
            this.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (info.Music.Song > -1)
                        {
                            if (Playlist == null || Playlist.Count == 0)
                                UpdatePlaylist();
                            currentPlayItemLbl.Content = Playlist[info.Music.Song].DisplayName;
                        }
                    }
                    catch
                    {

                    }

                    maxTimeLbl.Content = (int)info.Music.TotalTime.TotalSeconds;

                    seekBar.Maximum = info.Music.TotalTime.TotalSeconds;
                    seekBar.Value = info.Music.ElapsedTime.TotalSeconds;

                });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            
        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

      
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
           
            player.Connect("192.168.1.109", 6600);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            player.Play();
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            player.Pause();
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            recorder.Start();
            recorder.OnAudioData += streamer.WriteData;
            streamer.Start();

            Console.WriteLine("Playing on " + streamer.GetInterface());

            player.Connect("192.168.1.109", 6600);
            player.Clear();
            player.Add(string.Format("http://{0}:{1}", streamer.GetInterface(), streamer.Port));
            player.Play();
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            UpdateStatus();           
        }


        public void UpdatePlaylist()
        {
            try
            {
                playlistBox.ItemsSource = Playlist = new ObservableCollection<MusicInfo>(player.PlayList);
                playlistBox.InvalidateVisual();
            }
            catch
            {

            }
        }

        public void UpdateStatus()
        {
            UpdatePlaylist();

            //Files
            Search();
        }

        private void fileBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void playlistBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedFile = playlistBox.SelectedItem as MusicInfo;
            if (selectedFile == null)
                return;
            player.Play(selectedFile.ID);
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            player.Clear();
            UpdateStatus();
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if(openFileDialog.ShowDialog() == true)
            {
                 AudioFileReader reader = new AudioFileReader(openFileDialog.FileName);

                 streamer.Start();

                 player.Connect("192.168.1.109", 6600);
                 player.Clear();
                 player.Add(string.Format("http://{0}:{1}", streamer.GetInterface(), streamer.Port));
                 player.Play();

                 new Thread(() =>
                     {

                         int bytesRead = 0;
                         do
                         {
                             byte[] buffer = new byte[65536];
                             bytesRead = reader.Read(buffer, 0, buffer.Length);
                             streamer.WriteData(buffer, bytesRead);
                         }
                         while (bytesRead > 0);
                     }).Start();
            }
                               

           
        }

        private bool isOpen()
        {
            if (player == null)
                return false;
            return player.IsConnected;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isOpen())
            {
                int value = (int)((sender as Slider).Value);

                player.SetVolume(value);
            }
        }

        private void fileBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedFile = fileBox.SelectedItem as MusicInfo;
            if (selectedFile == null)
                return;

            player.Add(selectedFile.Filename);

            UpdateStatus();
        }

        private void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Search();
        }

        void Search()
        {
            string type = "Any";
            if (searchTypeBox.Text != "")
                type = searchTypeBox.Text;
            if (searchBox.Text.Length < 1)
            {
                fileBox.ItemsSource = player.Files;
                fileBox.InvalidateVisual();
            }
            else
            {
                fileBox.ItemsSource = player.Search(searchBox.Text, type);
                fileBox.InvalidateVisual();
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Search();
        }

        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            if (isOpen())
            {
                try
                {
                    Console.WriteLine("Trying to get raw uri from yt");

                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo("youtube-dl.exe",
                        string.Format("--prefer-insecure -g -f140 {0}", youtubeIdBox.Text));
                    startInfo.RedirectStandardOutput = true;
                    startInfo.UseShellExecute = false;

                    var proc = System.Diagnostics.Process.Start(startInfo);
                    var reader = proc.StandardOutput;
                    string rawUri = reader.ReadToEnd();

                    Console.WriteLine(rawUri);

                    player.Connect("192.168.1.109", 6600);
                      player.Clear();
                    player.Add(rawUri);
                      player.Play();

                }
                catch (Exception d)
                {
                    Console.WriteLine(d.Message);
                }
            }
        }



  
    }
}
