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

	public class Format
	{
		// Default value
		public string MusicInfoFormat = 
			"[%name%: &[%artist% - ]%title%]|%name%|[%artist% - ]%title%|%file%";
		
		public Format() {
			// Nothing...
		}
		public Format(string music_info_format) {
			MusicInfoFormat = music_info_format;
		}
		
		public static string TimeInSeconds(int time) {
			// Used for negative values
			string signal = "";
			if (time < 0) {
				time = -time;
				signal = "-";
			}
			
			string min = Convert.ToString(time / 60);
			string sec = Convert.ToString(time % 60);
			if (sec.Length == 1)
				sec = "0" + sec;
			return signal + min + ":" + sec;
		}
		
		public string MusicInfo(MPD.MusicInfo music) {
			// Just a little wrapper
			int curindex = 0;
			return this.MusicInfo(music, ref curindex);
		}
		
		protected string MusicInfo(MPD.MusicInfo music, ref int curindex) {
			bool found = false; // Used for required arguments
			System.Text.StringBuilder ret = new System.Text.StringBuilder();    // The return string
			string temp;        // A temporary string
			
			while (curindex < MusicInfoFormat.Length) {
				// Our current character
				char curchar = MusicInfoFormat[curindex];
			
				
				if (curchar == '|') {
					curindex++;
					// If we didn't find one argument
					if (!found)
						// Let's try this now
						ret.Remove(0, ret.Length);
					else
						// Else just skip it
						MusicInfoSkipFormatting(ref curindex);
					continue;
				}
				
				if (curchar == '&') {
					curindex++;
					// If we didn't find one argument
					if (!found)
						// Skip the following format instructions
						MusicInfoSkipFormatting(ref curindex);
					else
						// Else set "found" as false
						found = false;
					continue;
				}
				
				if (curchar == '[') {
					curindex++;
					// Try to parse whats inside the square brackets
					temp = this.MusicInfo(music, ref curindex);
					if (temp != null) {
						// and then add it to the current result
						ret.Append(temp);
						found = true;
					}
					continue;
				}
				
				if (curchar == ']') {
					// Our time has come, return the current result
					if (!found && ret.Length > 0)
						// but only if found everything needed
						ret = null;
					break;
				}
				
				if (curchar != '#' && curchar != '%') {
					// Adds to the buffer any character other character
					curindex++;
					ret.Append(curchar);
					continue;
				}
				
				if (curchar == '#' && curindex+1 < MusicInfoFormat.Length) {
					// Writes only the character after "#"
					curindex += 2;
					ret.Append(MusicInfoFormat[curindex+1]);
					continue;
				}
				
				// If we're here, it's because there's a %something% sequence 
				// curindex is the first "%", try to find the other
				temp = null;
				int finalindex = MusicInfoFormat.IndexOf('%', curindex+1);
				
				// If we found it
				if (finalindex >= 0) {
					// Then see what it is (the "+1" and the "-1" correspond to the "%" chars)
					string option = MusicInfoFormat.Substring(curindex+1, finalindex-curindex-1);
					
					// See if have length constrains
					int i = option.IndexOf(':');
					int limit = -1;
					if (i > 0) {
						limit = Convert.ToInt32(option.Substring(i+1));
						option = option.Substring(0, i);
					}
					
					switch (option) {
						case "file":
							temp = music.Filename;
							break;
						case "artist":
							if (music.Artist != "")
								temp = music.Artist;
							break;
						case "title":
							if (music.Title != "")
								temp = music.Title;
							break;
						case "album":
							if (music.Album != "")
								temp = music.Album;
							break;
						case "track":
							if (music.Track != "")
								temp = music.Track;
							break;
						case "name":
							if (music.Name != "")
								temp = music.Name;
							break;
						case "time":
							if (music.Time != -1)
								temp = Format.TimeInSeconds(music.Time);
							break;
					}
					
					// Could we format correctly?
					if (temp != null) {
						// Check size limits
						if (limit > -1 && temp.Length > limit)
							temp = temp.Substring(0, limit) + "...";
						
						// Add to the result and set "found" to true
						ret.Append(temp);
						found = true;
					} else {
						// Or else just add what we've got
						ret.AppendFormat("%{0}%", option);
					}
					
					// Move to the index of the last char plus one
					curindex = finalindex+1;
				} else {
					// But if we didn't find the corresponding "%", just print it
					ret.Append("%");
					curindex++;
				}
			}
			
			// Increment the curindex as maybe we're getting called by ourselves
			curindex++;
			if (ret != null)
				return ret.ToString();
			else
				return null;
		}
		
		// This function will skip anything inside a block until its end
		protected void MusicInfoSkipFormatting(ref int curindex) {
			int stack = 0;
			
			while (curindex < MusicInfoFormat.Length) {
				char curchar = MusicInfoFormat[curindex];
				
				if (curchar == '[')
					stack++;
				else if (curchar == '#' && curindex+1 < MusicInfoFormat.Length)
					curindex++;
				else if (stack > 0 && curchar == ']')
					stack--;
				else if (curchar == '&' || curchar == '|' || curchar == ']')
					return;
					
				curindex++;
			}
		}
	}
}
