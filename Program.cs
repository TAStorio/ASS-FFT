using System;

namespace ASS_FFT {
	class Program {
		static void Main(string[] args) {
			var client = new ASS_FFT.RCON.RCON("127.0.0.1", 2050, "RCONPASS");
			Console.WriteLine("Connecting and authentificating");
			client.ConnectAsync().Wait();
			client.ServerOutput += MessageGet;
			client.Start();
			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();
			client.SendCommandAsync("/fooping").Wait();
			client.SendCommandAsync("/h").Wait();
			client.SendCommandAsync("/fooping").Wait();
			Console.ReadKey();
		}

		public static void MessageGet(string message) {
			Console.WriteLine("Message get:"+message);
		}
	}
}
