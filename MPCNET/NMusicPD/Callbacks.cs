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

namespace MPD
{
	// Contains all the internal callbacks used in the ClientConnection.
	internal sealed class Callbacks
	{		
		public InternalCallback MusicCollectionPerFileCallback;
		public InternalCallback MusicCollectionPerPosCallback;
		public InternalCallback MusicInfoCallback;
		public InternalCallback StartsWithPlaylistCallback;
		public InternalCallback StatsInfoCallback;
		public InternalCallback StatusInfoCallback;
		public InternalCallback StringListCallback;
		
		private object ProcessMusicCollectionPerFile(string buffer) {
			return new MusicCollection(Functions.GetItemlistFromBuffer(buffer, "file", false));
		}
		
		private object ProcessMusicCollectionPerPos(string buffer) {
			return new MusicCollection(Functions.GetItemlistFromBuffer(buffer, "Pos", true));
		}
		
		private object ProcessMusicInfo(string buffer) {
			return new MusicInfo(Functions.GetHashtableFromBuffer(buffer));
		}
		
		private object ProcessStartsWithPlaylist(string buffer) {
			System.Collections.ArrayList result = new System.Collections.ArrayList(5);
			foreach (string line in buffer.Split('\n'))
				if (line.StartsWith("playlist: "))
					result.Add(line.Substring(10)); // "playlist: ".Length
			return result.ToArray(typeof(string));
		}		
		
		private object ProcessStatsInfo(string buffer) {
			return new StatsInfo(Functions.GetHashtableFromBuffer(buffer));
		}
		
		private object ProcessStatusInfo(string buffer) {
			return new StatusInfo(Functions.GetHashtableFromBuffer(buffer));
		}
		
		private object ProcessStringList(string buffer) {
			return Functions.GetStringListFromBuffer(buffer);
		}
	
		public Callbacks() {
			MusicCollectionPerFileCallback = new 
				InternalCallback(ProcessMusicCollectionPerFile);
			MusicCollectionPerPosCallback = new 
				InternalCallback(ProcessMusicCollectionPerPos);
			StartsWithPlaylistCallback = new 
				InternalCallback(ProcessStartsWithPlaylist);
			MusicInfoCallback = new InternalCallback(ProcessMusicInfo);
			StatsInfoCallback = new InternalCallback(ProcessStatsInfo);
			StatusInfoCallback = new InternalCallback(ProcessStatusInfo);
			StringListCallback = new InternalCallback(ProcessStringList);
		}
	}
}
