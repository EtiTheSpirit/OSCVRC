﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSCVRC.DataUtils {

	/// <summary>
	/// This class manages assembling and disassembling data sent and received in the OSC format.
	/// </summary>
	public static class OSCDataUtil {

		/// <summary>
		/// Buffers the provided text into a UTF-8 OSC string with the proper length.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static byte[] GetOSCString(string text) {
			byte[] textBuffer = Encoding.ASCII.GetBytes(text);
			int originalTextLength = textBuffer.Length;
			int length = originalTextLength + 1; // Add 1 because a space needs to be allocated for a terminating null.
			int extraNeededSpace = 4 - (length % 4);
			int newLength = length + extraNeededSpace;
			Array.Resize(ref textBuffer, newLength);
			for (int i = originalTextLength; i < newLength; i++) {
				textBuffer[i] = 0;
			}
			return textBuffer;
		}

		/// <summary>
		/// Buffers the provided text into a UTF-8 OSC string with the proper length.
		/// <para/>
		/// <strong>WARNING: THIS EXPECTS THE DESTINATION ARRAY TO HAVE SUFFICIENT SIZE.</strong>
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static int PutOSCString(byte[] destination, int at, string text) {
			int offset = 0;
			offset += Encoding.ASCII.GetBytes(text, 0, text.Length, destination, at);
			at += offset;
			destination[at++] = 0;
			int extraNeededSpace = 4 - (offset % 4);
			offset += extraNeededSpace;
			return offset;
		}

		/// <summary>
		/// Ensures the length of <paramref name="data"/> is a multiple of 4.
		/// </summary>
		/// <param name="data"></param>
		public static void Pad(ref byte[] data) {
			int extra = 4 - (data.Length % 4);
			if (extra == 4) extra = 0;
			Array.Resize(ref data, data.Length + extra);
		}

		/// <summary>
		/// Returns the bytes of the provided 32 bit integer in big endian form for OSC.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static byte[] GetBigEndianBytesOf(int value) => EnsureBigEndian(BitConverter.GetBytes(value));

		/// <summary>
		/// Returns the bytes of the provided 32 bit floating point value in big endian form for OSC.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static byte[] GetBigEndianBytesOf(float value) => EnsureBigEndian(BitConverter.GetBytes(value));

		/// <summary>
		/// Returns a buffer of bytes into a 32 bit little endian integer.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static int GetIntFromBigEndian(byte[] data) => BitConverter.ToInt32(EnsureBigEndian(data), 0);

		/// <summary>
		/// Returns a buffer of bytes into a 32 bit single precision floating point.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static float GetFloatFromBigEndian(byte[] data) => BitConverter.ToSingle(EnsureBigEndian(data), 0);

		/// <summary>
		/// This method inverts the endian-ness of the value if the system is little (OSC uses big). This is suitable for both outgoing data and incoming data.
		/// </summary>
		/// <remarks>
		/// This copies the input array instead of mutating it.
		/// </remarks>
		/// <param name="nativeOrderBytes"></param>
		/// <returns></returns>
		private static byte[] EnsureBigEndian(byte[] nativeOrderBytes) {
			if (BitConverter.IsLittleEndian) {
				Array.Reverse(nativeOrderBytes);
			}
			return nativeOrderBytes;
		}

		/// <summary>
		/// Converts a byte array to a string by translating each instance of <see cref="byte"/> into <see cref="char"/> to assemble the <see cref="string"/>.
		/// </summary>
		/// <param name="buf"></param>
		/// <returns></returns>
		public static string RawByteArrayToString(byte[] buf) => new string(buf.Select(b => (char)b).ToArray());

		/// <summary>
		/// Converts a string into a byte array by casting each <see cref="char"/> to <see cref="byte"/>.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static byte[] StringToRawByteArray(string str) => str.ToCharArray().Select(c => (byte)c).ToArray();

		/// <summary>
		/// A method that behaves like <see cref="string.StartsWith(string)"/>, but it also returns the substring occurring after the provided search query if it is present.<para/>
		/// <paramref name="textAfter"/> will be set to <paramref name="text"/> if <paramref name="searchFor"/> is not found!
		/// </summary>
		/// <param name="text"></param>
		/// <param name="searchFor"></param>
		/// <param name="textAfter"></param>
		/// <param name="comparison"></param>
		/// <returns></returns>
		public static bool StartsWithGetAfter(this string text, in string searchFor, out string textAfter, StringComparison comparison = StringComparison.CurrentCulture) {
			if (text.StartsWith(searchFor, comparison)) {
				if (text.Length == searchFor.Length) {
					textAfter = string.Empty;
					return true;
				} else {
					textAfter = text.Substring(searchFor.Length);
					return true;
				}
			}
			textAfter = text;
			return false;
		}

	}
}
