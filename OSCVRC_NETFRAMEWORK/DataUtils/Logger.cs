using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSCVRC.DataUtils {

	/// <summary>
	/// Logs happenings and stuff.
	/// </summary>
	public static class Logger {

		/// <summary>
		/// Writes the text and appends spaces to the end such that the amount of characters written is equal to <paramref name="width"/>.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="width"></param>
		private static void WriteTextWidth(string text, int width) {
			int remaining = width - Encoding.ASCII.GetBytes(text).Length;
			Console.Write(text);
			if (remaining > 0) Console.Write(new string(' ', remaining));
		}

		/// <summary>
		/// Logs the act of sending a parameter to VRChat.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		public static void LogSend(string name, object value) {
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write('[');
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("SEND");
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write("] ");
			Console.ForegroundColor = ConsoleColor.Green;
			WriteTextWidth(name, 24);
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" => ");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(value);
			Console.ForegroundColor = ConsoleColor.White;
		}

		/// <summary>
		/// Logs the act of receiving a parameter from VRChat.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		public static void LogReceiveParameter(string name, object value) {
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write('[');
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.Write("RECV");
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write("] ");
			Console.ForegroundColor = ConsoleColor.Green;
			WriteTextWidth(name, 24);
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(" <= ");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(value.ToString());
			Console.ForegroundColor = ConsoleColor.White;
		}

		/// <summary>
		/// Logs the act of changing avatars.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="avatarID"></param>
		public static void LogChangeAvatar(string name, string avatarID) {
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write('[');
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.Write("RECV");
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write("] ");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write("Changed avatar to ");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(avatarID);
			Console.ForegroundColor = ConsoleColor.White;
		}

	}
}
