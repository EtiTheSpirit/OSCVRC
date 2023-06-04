using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OSCVRC.DataUtils {

	/// <summary>
	/// This class manages assembling and disassembling data sent and received in the OSC format.
	/// </summary>
	public static class OSCDataManager {

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
		public static byte[] GetBigEndianBytesOf(int value) => InvertEndianness(BitConverter.GetBytes(value));

		/// <summary>
		/// Returns the bytes of the provided 32 bit floating point value in big endian form for OSC.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static byte[] GetBigEndianBytesOf(float value) => InvertEndianness(BitConverter.GetBytes(value));

		/// <summary>
		/// Returns a buffer of bytes into a 32 bit little endian integer.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static int GetIntFromBigEndian(byte[] data) => BitConverter.ToInt32(InvertEndianness(data));

		/// <summary>
		/// Returns a buffer of bytes into a 32 bit single precision floating point.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static float GetFloatFromBigEndian(byte[] data) => BitConverter.ToSingle(InvertEndianness(data));

		/// <summary>
		/// This method inverts the endian-ness of the value if the system is little (OSC uses big). This is suitable for both outgoing data and incoming data.
		/// </summary>
		/// <remarks>
		/// This copies the input array instead of mutating it.
		/// </remarks>
		/// <param name="nativeOrderBytes"></param>
		/// <returns></returns>
		private static byte[] InvertEndianness(byte[] nativeOrderBytes) {
			byte[] newData = new byte[4];
			nativeOrderBytes.CopyTo(newData, 0);
			if (BitConverter.IsLittleEndian) {
				Array.Reverse(newData);
			}
			return newData;
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

	}
}
