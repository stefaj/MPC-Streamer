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
	using System;
	using System.Text.RegularExpressions;

	public enum ErrorCode {
		NotList       = 1,
		Args          = 2,
		Password      = 3,
		Permission    = 4,
		UnknownCmd    = 5,
		NoExist       = 50,
		PlaylistMax   = 51,
		System        = 52,
		PlaylistLoad  = 53,
		UpdateAlready = 54,
		PlayerSync    = 55,
		Exist         = 56,
		UnknownCode   = -1
	}
	
	public class MPDError
	{
		public readonly string[] OriginalBuffer;
		public readonly ErrorCode Code = ErrorCode.UnknownCode;
		public readonly int Where;
		public readonly string Command;
		public readonly string Message;
		public readonly string FullErrorMessage;
		
		public MPDError(string buffer) {
			int i, j, k, l, m;
			
			try {
				OriginalBuffer = buffer.Trim('\n').Split('\n');
				FullErrorMessage = OriginalBuffer[OriginalBuffer.Length-1];
				
				i = FullErrorMessage.IndexOf('[');
				j = FullErrorMessage.IndexOf('@');
				k = FullErrorMessage.IndexOf(']');
				l = FullErrorMessage.IndexOf('{');
				m = FullErrorMessage.IndexOf('}');
				if (i < 0 || j <= (i+1) || k <= (j+1) || l <= (k+1) || m <= l)
					throw new Exception();
				
				int temp = Convert.ToInt16(FullErrorMessage.Substring(i+1, j-i-1));
				if (Enum.IsDefined(typeof(ErrorCode), temp))
					Code = (ErrorCode)temp;

				Where = Convert.ToInt16(FullErrorMessage.Substring(j+1, k-j-1));
				Command = FullErrorMessage.Substring(l+1, m-l-1);
				Message = FullErrorMessage.Substring(m+1);
			} catch (Exception e) {
				// We throw this exception because we want to be sure that the
				// exception sent to the program is a MPDException.
				throw new MPDException(String.Format("Error while parsing error from server. ({0})",
							Convert.ToString(e)));
			}
		}
		
		public override string ToString() {
			return Message + " (error code \"" + Convert.ToString(Code) +
				"\" in command \"" + Command + "\")";
		}
		
		public static bool IsAnError(string Buffer) {
			string line = Functions.LastLine(Buffer);
			return line.StartsWith("ACK");
		}
		
		public static bool IsAnError(string[] Buffer) {
			string line = Buffer[Buffer.Length-1];
			return line.StartsWith("ACK");
		}
	}
	
	// Generic exception class for MPD server errors
	public class MPDException : Exception {
		public readonly MPDError Error = null;
		
		public MPDException(string message): base(message) { }
		public MPDException(MPDError error): base(Convert.ToString(error)) {
			Error = error;
		}
	}

	// When the number tries to read data from server is exceeded
	public class TriesNoExceededException : MPDException {
		public TriesNoExceededException(string message): base(message) { }
	}
}
