using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPD;
using System.Timers;
using System.Threading;

namespace MPCNET
{
    public delegate void StatusUpdateDel(StatusInfo info);
    public class Player
    {
        MPDClient mpd;

        CommandCallback CurrentSongCallback;
        CommandCallback ListAllCallback;
        CommandCallback PlaylistCallback;
        CommandCallback StatusCallback;
        CommandCallback ArtistsAndAlbumsCallback;
        private CommandCallback BookmarksCallback;

        StatusInfo lastStatus;
        System.Timers.Timer updateTimer;

        bool isSyncBusy = false;

        public event StatusUpdateDel OnStatusUpdate;


        private MusicInfo[] files = null;
        private MusicInfo[] playList = null;

        public bool IsConnected { get; private set; }

        public MusicInfo[] Files
        {
            get
            {
                if (!IsConnected)
                {
                    return new MusicInfo[] { };
                }

                mpd.ListAll(ListAllCallback);
                ReadConnectionResponse();
                MusicInfo[] return_ = files;
                return return_;
            }
        }
       
        public MusicInfo[] PlayList
        {
            get
            {
                if(!IsConnected)
                {
                    return new MusicInfo[] { };
                }

                mpd.Playlist(PlaylistCallback);
                ReadConnectionResponse();
                MusicInfo[] return_ = playList;
                return return_;
            }
        }

        public Player()
        {

            CurrentSongCallback = new CommandCallback(UpdateCurrentSong);
            ListAllCallback = new CommandCallback(UpdateFiles);
            PlaylistCallback = new CommandCallback(UpdatePlaylist);
            StatusCallback = new CommandCallback(UpdateStatus);
            ArtistsAndAlbumsCallback = new CommandCallback(UpdateAlbums);
            BookmarksCallback = new CommandCallback(UpdateBookmarks);
            IsConnected = false;

            updateTimer = new System.Timers.Timer(1000);
            updateTimer.Elapsed+=updateTimer_Elapsed;
            updateTimer.Start();
            
        }

        void updateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(!IsConnected)
                return;
            if (isSyncBusy)
                return;

            try
            {
            mpd.Status(StatusCallback);
            new Thread(()=>
                {
                    ReadConnectionResponse();
                }).Start();
            }
            catch
            {

            }
        }

        public MusicInfo[] Search(string match, string searchType)
        {
            if (!IsConnected)
            {
                return new MusicInfo[] { };
            }
            mpd.Search(searchType, match, false, ListAllCallback);
           
            ReadConnectionResponse();
            MusicInfo[] return_ = files;
            return return_;
        }

        private void UpdateCurrentSong(object result)
        {
            var CurrentSong = (MusicInfo)result;

            Console.WriteLine(CurrentSong.Name);
        }

        public void Seek(int seconds)
        {
            mpd.SeekTo(lastStatus.Music.SongID, seconds);
            ReadConnectionResponse();
        }

        private void UpdateFiles(object result)
        {
            var collection = (MusicCollection)result;
            
            files = (collection).Songs;
            
        }

        public void Connect(string host, int port)
        {
            try
            {


                mpd = new MPDClient(host, port);

                mpd.Connect();
                ReadConnectionResponse();

                IsConnected = true;
            }
            catch
            {
                IsConnected = false;
            }
        }


        public void Play(int id = -1)
        {
            if (id < 0)
                mpd.Play();
            else
                mpd.Play(id);
            ReadConnectionResponse();
        }

        public void Pause()
        {
            mpd.Paused(true);
            ReadConnectionResponse();
        }

        public void SetVolume(int volume)
        {
            mpd.Volume(volume);
            ReadConnectionResponse();
        }
        
        public void Clear()
        {
            mpd.Clear();
            ReadConnectionResponse();
        }

        public void Add(string uri)
        {
            mpd.Add(uri);
            ReadConnectionResponse();
        }

        public void AddRaw(string data)
        {
            mpd.AddRaw(data);
            ReadConnectionResponse();
        }


        private bool ReadConnectionResponse()
        {
            if (isSyncBusy) // wait for previous call to finish first
                return false;

            isSyncBusy = true;  
            try
            {
                if (mpd != null)
                    while (mpd.ReadResponse()) { }
            }
            catch (System.IO.IOException)
            {
                //ConfigHasChanged(true);
            }
            catch (MPD.TriesNoExceededException)
            {
                //ConfigHasChanged(true);
            }
            catch (MPD.MPDException e)
            {
                //if (!ExceptionHandler.Handle(e, this))
                //    throw e;
            }
            isSyncBusy = false;
            return true;
        }




        private void UpdatePlaylist(object result)
        {

            var musicCollection = ((MusicCollection)result);
            playList = musicCollection.Songs;
        }


        private void UpdateStatus(object result)
        {
            lastStatus = (StatusInfo)result;
            if(OnStatusUpdate != null)
                OnStatusUpdate(lastStatus);
            
        }


        private void UpdateAlbums(object result)
        {
            var albums = (System.Collections.Hashtable)result;
        }


        // Just take the bookmarks list and save it
        private void UpdateBookmarks(object result)
        {
            var bookmarks = (string[])result;
        }

    }
}
