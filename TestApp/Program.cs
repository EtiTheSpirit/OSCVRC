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
			while (true) Console.ReadLine();
		}
	}
}