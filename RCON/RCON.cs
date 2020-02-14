using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

//Class used for connecting to Factorio RCON
namespace ASS_FFT.RCON {
	public class RCON : IDisposable {
		private Socket socket;

		//Settings
		private int timeout = 100;
		private IPEndPoint endPoint;
		private string password;

		//Variables
		public bool Connected {get {
			if (this.socket == null) return false;
			return this.socket.Connected;
		}}

		//Events
		public event StringOutput ServerOutput;
		public event StringOutput Errors;

		public RCON(string IP_Adress, int port, string password) {
			IPAddress address = null;
			if (!IPAddress.TryParse(IP_Adress, out address)) {
				throw new ArgumentException("Invalid address");
			}
			this.endPoint = new IPEndPoint(address, port);
			this.password = password;
		}

		public async Task ConnectAsync() {
			if (Connected) {
				return;
			}
			this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			this.socket.ReceiveTimeout = timeout;
			this.socket.SendTimeout = timeout;
			this.socket.NoDelay = true;
			await this.socket.ConnectAsync(this.endPoint);

			// Wait for successful authentication
			await SendPacketAsync(new RCONPacket(RCONPacketType.Auth, password));
		}

		public void Start() {
			if (!Connected) {
				throw new InvalidOperationException("Not connected to a RCON server");
			}

			Task.Run(() => Recieve());

			/*if (_beaconIntervall != 0) {
				Task.Run(() =>
					 WatchForDisconnection(_beaconIntervall).ConfigureAwait(false)
				);
			}*/
		}

		private async Task Recieve() {
			byte[] buffer = new byte[4096];
			var builder = new RCONPacketBuilder();
			while (Connected) {
				int bytes = await this.socket.ReceiveAsync(buffer, SocketFlags.None);
				builder.FeedBytes(buffer, bytes);
				while (builder.AvailablePackets > 0) {
					RCONPacket packet = builder.GetPacket();
					Console.WriteLine(packet.Type);
					Console.WriteLine(packet.Id);
					Console.WriteLine(packet.ToString());
				}
			}
		}

		public void Dispose() {
			this.socket.Shutdown(SocketShutdown.Both);
			this.socket.Dispose();
		}

		public async Task SendCommandAsync(string command) {
			await this.SendPacketAsync(new RCONPacket(RCONPacketType.ExecCommand, command));
		}

		private async Task SendPacketAsync(RCONPacket packet) {
			await this.socket.SendAsync(packet.ToBytes(), SocketFlags.None);
		}
	}

	public delegate void StringOutput(string output);
}
