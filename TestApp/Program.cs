using OSCVRC;
using OSCVRC.DataUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp {
	internal class Program {
		private static void Main(string[] args) {
			using VRCAvatarParameterOSCInterface paramDriver = new VRCAvatarParameterOSCInterface();
			paramDriver.LogActions = true;

			(string, Variant<int, float, bool>)[] data = new (string, Variant<int, float, bool>)[255];
			for (int i = 0; i < data.Length; i++) {
				data[i] = ($"Value{i}", i);
			}
			paramDriver.SetManyParameters(data);

			while (true) Console.ReadLine();
		}
	}
}