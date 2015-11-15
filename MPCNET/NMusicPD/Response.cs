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
	using System.Net.Sockets;
	using System.Text;
	
	internal sealed class ResponseHandler
	{
		public int BufferSize = 8 * 1024; // in bytes
		private int TryNo = 0; // limit the number of failed tries to 20
		private StringBuilder bigBuffer = new StringBuilder(8 * 1024);
		private ReadResultType readType;
		private Hashtable Endings;
		
		public ResponseHandler() {
			Endings = new Hashtable();
			Endings[ReadResultType.Block] = new string[] {"list_OK", "ACK"};
			Endings[ReadResultType.Ignore] = new string[] {"OK", "ACK"};
		}
		
		// Try to fetch and parse a chunk of data. Each string contains a response from a command
		public ArrayList ReadResult(NetworkStream networkStream, ReadResultType type) {		
			// Initialize vars
			readType = type;
			ArrayList finalResult = new ArrayList();
			byte[] smallBuffer = new byte[BufferSize];
			
			// Read
			int bytesRead = networkStream.Read(smallBuffer, 0, BufferSize);

			if (bytesRead == 0) {
				// Failed try...
				TryNo++;
				if (TryNo >= 20)
					throw new TriesNoExceededException(
							"Number of tries exceeded max. of 20 tries.");
			} else
				// Successful one
				TryNo = 0;
			
			// Append to buffer
			bigBuffer.Append(Encoding.UTF8.GetString(smallBuffer, 0, bytesRead));
			
			// Try to take most commands of the buffer
			string buffer = RemoveNewlines();
			while (buffer != "") {
				string result = ParseChunk(buffer);
				if (result == null)
					break;
				else
					finalResult.Add(result);
				if (MPDError.IsAnError(result))
					throw new MPDException(new MPDError(result));
				buffer = RemoveNewlines();
			}
			
			// See if we've finished with the block of commands
			if (readType == ReadResultType.Block)
				if (TestFinalReached(buffer))
					finalResult.Add("");
					
			return finalResult;
		}
		
		// Remove any newlines that may exist in the begginning of the bigBuffer.
		// Returns the current buffer converted to a string for convinience.
		private string RemoveNewlines() {
			int length = bigBuffer.Length;
			int i = 0;
			while (i < length && bigBuffer[i] == '\n')
				i++;
			if (i > 0) 
				bigBuffer.Remove(0, i);
			return Convert.ToString(bigBuffer);
		}
		
		// Tries to find a complete response in the bigBuffer
		private string ParseChunk(string buffer) {
			string result = null;
			
			if (readType == ReadResultType.Normal) {
				string lastLine = Functions.LastLine(buffer);
				if (lastLine.StartsWith("OK") || lastLine.StartsWith("ACK"))
					result = buffer;
			} else {
				int index;
				foreach (string end in (string[])Endings[readType]) {
					index = buffer.IndexOf(end);
					if (index >= 0) {
						int final;
						if (end == "ACK") {
							int nl = buffer.IndexOf('\n', index);
							if (nl > 0)
								final = nl;
							else
								final = buffer.Length;
						} else
							final = index + end.Length;
						result = buffer.Substring(0, final);
						break;
					}
				}
			}
			
			if (result != null)
				bigBuffer.Remove(0, result.Length);
			return result;
		}
		
		// Tests if the final "OK" on block of commands has arrived
		private bool TestFinalReached(string buffer) {
			string lastLine = Functions.LastLine(buffer);
			if (lastLine.StartsWith("OK")) {
				bigBuffer.Remove(0, bigBuffer.Length);
				return true;
			} else if (MPDError.IsAnError(lastLine)) {
				bigBuffer.Remove(0, bigBuffer.Length);
				throw new MPDException(new MPDError(buffer));
			} else
				return false;
		}
	}	
}
