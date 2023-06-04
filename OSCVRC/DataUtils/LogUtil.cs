using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSCVRC.DataUtils {
	public static class LogUtil {

		private static void WriteTextWidth(string text, int width) {
			int remaining = width - Encoding.ASCII.GetBytes(text).Length;
			Console.Write(text);
			if (remaining > 0) Console.Write(new string(' ', remaining));
		}

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

		public static void LogReceive(string name, byte[]? data, object value) {
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
			if (data != null) {
				WriteTextWidth(value.ToString()!, 24);
				Console.ForegroundColor = ConsoleColor.Gray;
				Console.Write($"0x{data[0]:X2}");
				Console.Write($"{data[1]:X2}");
				Console.Write($"{data[2]:X2}");
				Console.WriteLine($"{data[3]:X2} (BE)");
			} else {
				Console.WriteLine(value.ToString());
			}
			Console.ForegroundColor = ConsoleColor.White;
		}

	}
}
