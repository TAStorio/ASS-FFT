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
			this.endPoint = new IPEndPoint(IPAddress.Parse(IP_Adress), port);
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
			await SendPacketAsync(new RCONPacket(0, RCONPacketType.Auth, password));
			//_networkConsumerTask = Task.WhenAll(writing, reading);
			//await _authenticationTask.Task;
			/*if (_beaconIntervall != 0) {
				Task.Run(() =>
					 WatchForDisconnection(_beaconIntervall).ConfigureAwait(false)
				);
			}*/
		}

		public void Dispose() {
			this.socket.Shutdown(SocketShutdown.Both);
			this.socket.Dispose();
		}

		private async Task SendPacketAsync(RCONPacket packet) {
			await this.socket.SendAsync(packet.ToBytes(), SocketFlags.None);
		}
	}

	public delegate void StringOutput(string output);
}
