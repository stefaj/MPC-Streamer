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
	using System.Collections;

	internal class Functions
	{	
		// Return a escaped version of the string, ready to be sent to the server
		public static string EscapeStr(string str) {
			return " \"" + str.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\" ";
		}
		
		// Returns last line of a string.
		public static string LastLine(string str) {
			int i = str.Length;
			while (str[i-1] == '\n')
				i--;
			if (i > 0) {
				int lastNewLine = str.Substring(0, i).LastIndexOf("\n");
				if (lastNewLine > 0)
					return str.Substring(lastNewLine + 1);
			}
			return str;
		}
	
		// Takes a string like "name: value" and returns {"name", "value"}
		public static string[] SplitKeyFromValue(string line) {
			int index = line.IndexOf(": ");
			if (index < 0)
				return new string[] {line, ""};
			else
				return new string[] {line.Substring(0, index), line.Substring(index + 2)};
		}
		
		// Gets a list from a buffer (like in Artists command)
		public static string[] GetStringListFromBuffer(string buf) {
			string[] buffer = buf.Split('\n');
			int offset = buffer[0].IndexOf(": ") + 2;
			
			if (offset == 1) // (-1 + 2) <-- when that .IndexOf is -1
				return new string[] {};
			
			int count = CountBeforeEnd(buffer);
			string[] result = new string[count];
			
			for (int i = 0; i < count; i++)
				result[i] = buffer[i].Substring(offset);
			
			return result;
		}
		
		// Gets a Hashtable from a buffer (like in Statistics)
		public static Hashtable GetHashtableFromBuffer(string buf) {
			// This is much like GetListFromBuffer but more a bit more expensive
			string[] buffer = buf.Split('\n');
			int offset;
			int count = CountBeforeEnd(buffer);
			
			Hashtable result = new Hashtable(count);
			
			for (int i = 0; i < count; i++) {
				offset = buffer[i].IndexOf(": ");
				if (offset == -1) continue;
				result[buffer[i].Substring(0, offset)] = buffer[i].Substring(offset + 2);
			}
			
			return result;
		}
		
		//
		public static ArrayList GetItemlistFromBuffer(string buf, string index, bool useindex) {
			string[] buffer = buf.Split('\n');
			int count = CountBeforeEnd(buffer);
			ArrayList result = new ArrayList(count / 5);

			Hashtable currentitem = null;
			string currentindex = "";
			string delimiter = SplitKeyFromValue(buffer[0])[0];
			
			for (int i = 0; i < count; i++) {
				string line = buffer[i];
				string[] splitted = SplitKeyFromValue(line);
				
				if (splitted[0] == delimiter) {
					if (currentindex != "") {
						if (useindex)
							currentitem["INDEX"] = currentindex;
						else {
							currentitem["INDEX"] = -1;
							currentitem[index] = currentindex;
						}
						result.Add(currentitem);
					}
					currentitem = new Hashtable(7);
					currentindex = "";
				}
				
				if (splitted[0] == index)
					currentindex = splitted[1];
				else
					currentitem[splitted[0]] = splitted[1];
			}
			
			if (currentindex.Length > 0) {
				if (useindex)
					currentitem["INDEX"] = currentindex;
				else {
					currentitem["INDEX"] = -1;
					currentitem[index] = currentindex;
				}
				result.Add(currentitem);
			}
			
			return result;
		}

		//
		private static int CountBeforeEnd(string[] buffer) {
			int count = buffer.Length - 1;
			while (count > 0)
				if (buffer[count].StartsWith("OK") || 
				    buffer[count].StartsWith("list_OK") ||
				    buffer[count].StartsWith("ACK"))
					break;
				else
					count--;
			if (count == 0 && (!(buffer[count].StartsWith("OK") || 
			                     buffer[count].StartsWith("list_OK") ||
			                     buffer[count].StartsWith("ACK"))))
				return buffer.Length;
			else
				return count;
		}
	}
}
