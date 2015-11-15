/*  Copyright (C) 2004-2005 Felipe Almeida Lessa
    
    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

namespace MPD {
	using System;
	using System.Collections;
	using System.IO;
	using System.Net.Sockets;
	using System.Threading;

	public class MPDClient
	{
		// Server socket/streams
		private TcpClient ServerSocket;
		private NetworkStream ServerNetworkStream;
		private StreamWriter ServerStreamWriter;
		
		// Server location
		public string Host;
		public int Port;
				  
		// MPD version
		private string mpdversion = "";
		public string MPDVersion { 
			get { return mpdversion; }
		}
		
		// Response stuff
		private ResponseHandler Response;
		private Callbacks callbacks = new Callbacks();
		private object data = null; // This is used by some methods for temporary stuff 
		
		// Block of commands stuff
		private ArrayList blockCommands;
		private ReadResultType nextReadResultType = ReadResultType.Ignore;
		
		// User avaiable stuff
		public bool Waiting {
			get { return (blockCommands.Count > 0 && ((Command)blockCommands[0]).Sent); }
		}
		
		// Initialize object
		public MPDClient(string host, int port) {
			Host = host;
			Port = port;
			Response = new ResponseHandler();
			blockCommands = new ArrayList();
			Connect();
		}
				
		// Connect to server
		public void Connect() {
			// Initialize connection
			ServerSocket = new TcpClient(Host, Port);
			ServerSocket.NoDelay = true;
			ServerNetworkStream = ServerSocket.GetStream();
			ServerStreamWriter = new StreamWriter(ServerNetworkStream);
			
			// Check response
			byte[] buffer = new byte[100];
			string raw_response = System.Text.Encoding.UTF8.GetString(buffer, 0, 
					ServerNetworkStream.Read(buffer, 0, 100));
			string[] response = raw_response.TrimEnd('\n').Split(' ');
			if (response[0] != "OK" || response[1] != "MPD")
				throw new MPDException("Cannot find MPD server.");
			mpdversion = response[2];		
		}
		
		// Disconnect from server
		public void Disconnect() {
			// Close everything
			ServerStreamWriter.Close();
			ServerNetworkStream.Close();
			ServerSocket.Close();
			// Clear vars
			blockCommands.Clear();
			mpdversion = "";
			data = null;
		}

		// Remove any unsent commands that may be in command cache
		// Note: we don't want to remove sent commands, do we?
		public void ClearCache() {
			if (blockCommands.Count == 0)
				return;
			int index;
			for (index = 0; index < blockCommands.Count; index++)
				if (((Command)blockCommands[index]).Sent)
					break;
			if (!((Command)blockCommands[index]).Sent)
				return;
			blockCommands.RemoveRange(index, blockCommands.Count - index);
		}

		
		// Add a file or directory to the playlist
		public void Add(string filename) {
			SendCommand("add" + Functions.EscapeStr(filename));
		}

        public void AddRaw(string data)
        {
            SendCommand("add " + data);
        }

		// Add a stream to the playist. Use as many Add() calls as necessary. This command blocks.
		public void AddStream(string location, bool clearPlaylistOnSuccess) {
			// Type can be: -1 (unrecognized), 0 (stream) or 1 (playlist)
			short type = -1;
			
			// Contact server
			Uri uri = new Uri(location, true);
			TcpClient Socket = new TcpClient(uri.Host, uri.Port);
			NetworkStream netStream = Socket.GetStream();
			StreamWriter netWriter = new StreamWriter(netStream);

			// Send headers
			netWriter.Write("GET " + uri.PathAndQuery + " HTTP/1.0\r\n" +
					"User-Agent: SharpMusic\r\n" +
					"Host: " + uri.Host + "\r\n" +
					"Accept: */*\r\n" +
					"\r\n");
			netWriter.Flush();

			// Check response
 			byte[] buffer = new byte[4096];
			int count = netStream.Read(buffer, 0, 4096);
			string raw_response = System.Text.Encoding.ASCII.GetString(buffer, 0, count);
			string[] response = raw_response.Split('\n');
			if (response[0].StartsWith("ICY"))
				type = 0;
			else {
				foreach (string str in response) {
					if (type != -1)
						break;
					else if (str.StartsWith("Content-Type: ")) {
						string content = str.Substring("Content-Type: ".Length).ToLower();
						if (content.IndexOf("ogg") > -1 || 
								content.IndexOf("mp3") > -1 || 
								content.IndexOf("flac") > -1 || 
								content.IndexOf("icy") > -1)
							type = 0;
						else if (content.IndexOf("pls") > -1 || content.IndexOf("m3u") > -1)
							type = 1;
						break;
					} else if (str.StartsWith("icy") || str.IndexOf("Ogg") > -1) {
						type = 0;
						break;
					} else if (str.IndexOf("[playlist]") > -1) {
						type = 1;
						break;
					}
				}
			}
			if (type == -1)
				// We couldn't recognize the type
				throw new MPDException("Could not recognize radio station.");

			// Clear the playlist, if needed
			if (clearPlaylistOnSuccess)
				this.Clear();
			
			if (type == 0) {
				// It's a "plain" stream
				this.Add(location);
				return;
			} else if (type == 1) {
				// It's a playlist, parse it
				StreamReader stream = new StreamReader((new System.Net.WebClient()).OpenRead(location));
				string line = stream.ReadLine();
				while (line != null) {
					// First possibility
					int i1 = line.IndexOf("File");
					int i2 = line.IndexOf("=");
					if (i1 > -1 && (i1+4) < i2) {
						this.Add(line.Substring(i2+1));
						goto next;
					}
					
					// Second one
					i1 = line.IndexOf("http://");
					if (i1 > -1) {
						this.Add(line.Substring(i1));
						goto next;
					}
	
					// Take next string
					next:
					line = stream.ReadLine();
				}
			}
		}
		
		// Return all albums (string[])
		public void Albums(CommandCallback callback) {
			SendCommand("list album", callbacks.StringListCallback, callback);
		}
		
		// Return all albums from the given artist (string[])
		public void Albums(string artist, CommandCallback callback) {
			SendCommand("list album" + Functions.EscapeStr(artist), 
				callbacks.StringListCallback, callback);
		}
			
		// Return the list of all artists
		public void Artists(CommandCallback callback) { 
			SendCommand("list artist", callbacks.StringListCallback, callback);
		}
		
		
		
		// Return the list of albums classified by artist (Hashtable)
		public void ArtistsAndAlbums(CommandCallback callback) {
			data = new AAAStorage(callback);
			Artists(new CommandCallback(ArtistsAndAlbums2));
		}
		
		// Take the list of artists and call the next method 
		private void ArtistsAndAlbums2(object result) {
			if ( ((string[])result).Length == 0)
				((AAAStorage)data).Callback( ((AAAStorage)data).Result );
			((AAAStorage)data).Artists = (string[])result;
			Albums( ((string[])result)[0], new CommandCallback(ArtistsAndAlbums3) );
		}
		
		// Loop on the list of artists and return the final value
		private void ArtistsAndAlbums3(object result) {
			if ( ((string[])result).Length > 0 ) {
				string artist = (((AAAStorage)data).Artists) [((AAAStorage)data).CurArtist];
				((AAAStorage)data).Result[artist] = result;
			}
			((AAAStorage)data).CurArtist++;
			if ( ((AAAStorage)data).CurArtist == ((AAAStorage)data).Artists.Length )
				((AAAStorage)data).Callback( ((AAAStorage)data).Result );
			else
				Albums( ((AAAStorage)data).Artists[((AAAStorage)data).CurArtist],
						new CommandCallback(ArtistsAndAlbums3) );
		}
		
		
		
		// Clear the playlist
		public void Clear()	{
			SendCommand("clear");
		}
		
		// Crossfade value in seconds
		public void Crossfade(int Value) {
			if (Value < 0 || Value > 300)
				throw new MPDException("Crossfade value out of range.");
			SendCommand("crossfade " + Convert.ToString(Value));
		}
		
		// Returns current song info (MusicInfo)
		public void CurrentSong(CommandCallback callback) {
			SendCommand("currentsong", callbacks.MusicInfoCallback, callback);
		}
		
		// Lists all files in database (MusicCollection)
		public void ListAll(CommandCallback callback) {
			SendCommand("listallinfo", 
				callbacks.MusicCollectionPerFileCallback, callback);
		}
		
		// Lists all files within a directory (MusicCollection)
		public void ListAll(string directory, CommandCallback callback) {
			SendCommand("listallinfo" + Functions.EscapeStr(directory), 
				callbacks.MusicCollectionPerFileCallback, callback);
		} 
		
		// Lists all current saved playlists (string[])
		public void ListPlaylists(CommandCallback clback) {
			SendCommand("lsinfo", callbacks.StartsWithPlaylistCallback, clback);
		}
		
		// Load a saved playlist
		public void LoadPlaylist(string name) {
			SendCommand("load" + Functions.EscapeStr(name));
		}
		
		// Moves a playlist item to another position in the playlist
		public void Move(int id, int newpos) {
			SendCommand("move " + Convert.ToString(id) + " " + Convert.ToString(newpos));
		} 
		
		// Jump to the next music
		public void Next() {
			SendCommand("next");
		}
		
		// Send the password to the server
		public void Password(string password) {
			SendCommand("password" + Functions.EscapeStr(password));
		}
		
		// Set paused state [this only pauses or not; see also Playing]
		public void Paused(bool state) {
			SendCommand("pause " + (state ? "1" : "0"));
		}
		
		// Just ping the server
		public void Ping() {
			SendCommand("ping");
		}
		
		// Play current selected music
		public void Play() {
			SendCommand("play");
		}
		
		// Play music with given ID
		public void Play(int id) {
			SendCommand("playid " + Convert.ToString(id));
		} 
				
		// Returns the current playlist (MusicCollection).
		public void Playlist(CommandCallback callback) {
			SendCommand("playlistid", callbacks.MusicCollectionPerPosCallback,
				callback);
		}
		
		// Returns the changes the playlist suffered since the specified version
		// (MusicCollection)
		public void PlaylistChanges(int oldversion, CommandCallback callback) {
			SendCommand("plchanges " + Convert.ToString(oldversion),
				callbacks.MusicCollectionPerPosCallback, callback);
		}
		
		// Returns info about a specific music in the playlist (MusicInfo)
		public void PlaylistSongInfo(int id, CommandCallback callback) { 
			SendCommand("playlistid " + Convert.ToString(id),
				callbacks.MusicInfoCallback, callback);
		}
		
		// Go back to the previous music
		public void Prev() {
			SendCommand("previous");
		}
		
		// Get/set random state
		public void Random(bool state) {
			SendCommand("random " + (state ? "1" : "0"));
		}
		
		// Remove a song from the playlist
		public void RemovePlaylist(string name) {
			SendCommand("rm" + Functions.EscapeStr(name));
		}
		
		// Remove a song from the playlist
		public void RemoveSong(int id) {
			SendCommand("deleteid " + Convert.ToString(id));
		}
		
		// Get/set repeat state
		public void Repeat(bool state) {
			SendCommand("repeat " + (state ? "1" : "0"));
		}
		
		// Save current playlist
		public void SavePlaylist(string name) {
			SendCommand("save" + Functions.EscapeStr(name));
		}
		
		// Search for something in the database (MusicCollection)
		public void Search(string type, string what, bool exactmatch, 
						   CommandCallback callback) {
			string Command = "";
			if (exactmatch) Command = "find "; else Command = "search ";
			SendCommand(Command + type + Functions.EscapeStr(what), 
				callbacks.MusicCollectionPerFileCallback, callback);
		}

        public void Search(SearchType type, string what, bool exactmatch,
                       CommandCallback callback)
        {
            Search(Enum.GetName(typeof(SearchType), type).ToLower(), what, exactmatch, callback);
        }
		
		// Seek to some position in the specified song
		public void SeekTo(int id, int time) {
			SendCommand("seekid " + Convert.ToString(id) + " " + Convert.ToString(time));
		}
		
		// Shuffle the playlist
		public void Shuffle() {
			SendCommand("shuffle");
		}
		
		// Shutdown MPD server
		public void Shutdown() {
			SendCommand("kill");
		}
		
		// Gets the current statistics (StatsInfo)
		public void Statistics(CommandCallback callback) {
			SendCommand("stats", callbacks.StatsInfoCallback, callback);
		}
		
		// Gets current status (StatusInfo)
		public void Status(CommandCallback callback) {
			SendCommand("status", callbacks.StatusInfoCallback, callback);
		}
		
		// Stop playing current music
		public void Stop() {
			SendCommand("stop");
		}
		
		// Swap music positions
		public void Swap(int id1, int id2) {
			SendCommand("swapid " + Convert.ToString(id1) + " " + Convert.ToString(id2));
		}
		
		// Updates the music database
		public void Update() {
			SendCommand("update");
		}
		
		// Get/set volume value
		public void Volume(int Value) {
			if (Value < 0 || Value > 100)
				throw new MPDException("Volume value out of range (0%-100%).");
			SendCommand("setvol " + Convert.ToString(Value));
		}
		
		// Read a chunk of data from the server and try to parse it.
		// Returns true if there's more data to receive.
		public bool ReadResponse() {
			if (!SendBufferToServer())
				return false;
			
			ReadResultType type = ((Command)blockCommands[0]).ReadType;
			string[] responses;
			
			try {
				responses = (string[]) Response.ReadResult(ServerNetworkStream, type)
					.ToArray(typeof(string));
			} catch (TriesNoExceededException e) {
				// Disconnect
				Disconnect();
				// And forward exception to let everyone know
				throw e;
			}
			
			foreach (string server_response in responses) {
				if (server_response == "" && 
						(blockCommands.Count == 0 || (!((Command)blockCommands[0]).Sent)) )
					continue;
				
				if (type == ReadResultType.Ignore) {
					// Remove all sent commands
					while (blockCommands.Count > 0 && ((Command)blockCommands[0]).Sent)
						blockCommands.RemoveAt(0);
				} else {
					// Remove only one sent command
					Command cmd = (Command)blockCommands[0];
					blockCommands.RemoveAt(0);
					if (cmd.Callback != null && cmd.LastCallback != null)
						cmd.LastCallback(cmd.Callback(server_response));
				}
			}
			
			return (blockCommands.Count > 0);
		}
		
		// Just add to the cache a command to be send	
		protected void SendCommand(string cmdline) {
			SendCommand(cmdline, null, null);
		}
		protected void SendCommand(string cmdline, InternalCallback callback,
								   CommandCallback lastCallback) {
			blockCommands.Add(new Command(cmdline, callback, lastCallback));
			if (nextReadResultType == ReadResultType.Ignore && lastCallback != null)
				nextReadResultType = ReadResultType.Block;
		}	
	
		// Send commands in-cache to the server. Returns true if there's a command
		// waiting for response
		protected bool SendBufferToServer() {
			if (blockCommands.Count == 0)
				// No commands
				return false;
			else if (((Command)blockCommands[0]).Sent)
				// If a command has already been sent, we won't send others now
				return true; // but true because there's data to read
			
			ReadResultType type;
			
			if (blockCommands.Count > 1) {
				// More than one command
				type = nextReadResultType;
			} else {
				// Only one command
				type = ReadResultType.Normal;
			}
				
			// Open a block
			if (type == ReadResultType.Block)
				ServerStreamWriter.WriteLine("command_list_ok_begin");
			else if (type == ReadResultType.Ignore)
				ServerStreamWriter.WriteLine("command_list_begin");
			
			// Send commands, marking them as sent
			foreach (Command cmd in blockCommands.ToArray(typeof(Command))) {
				ServerStreamWriter.WriteLine(cmd.Cmdline);
				cmd.Sent = true;
				cmd.ReadType = type;
			}
			
			if (type == ReadResultType.Block || type == ReadResultType.Ignore)
				// Closes the block
				ServerStreamWriter.WriteLine("command_list_end");
			
			// Force sending the data
			ServerStreamWriter.Flush();

			// Clear nextReadResultType to its default value
			nextReadResultType = ReadResultType.Ignore;
			
			// Yes, we've got things to do
			return true;
		}
	}
}
