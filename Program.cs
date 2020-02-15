using System;
using ASS_FFT.RCON;

namespace ASS_FFT {
	class Program {
		static void Main(string[] args) {
			var client = new ASS_FFT.RCON.RCON("127.0.0.1", 2050, "RCONPASS");
			Console.WriteLine("Connecting and authentificating");
			client.StartClient().Wait();
			Console.WriteLine("Send a command to the server, type exit() to quit the application");
			while (true) {
				string command = Console.ReadLine();
				if (command == "exit()") break;
				Console.WriteLine(client.SendCommandAsync(command).Result.ToString());
			}
		}
	}
}
