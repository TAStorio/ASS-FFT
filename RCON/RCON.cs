using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

//Class used for connecting to Factorio RCON
namespace ASS_FFT.RCON {
	public class RCON : IDisposable {

		//Settings
		private int timeout = 100;
		private int heartbeatInterval = 500;
		private IPEndPoint endPoint;
		private string password;

		//Variables
		public bool Connected {get {
			if (this.socket == null) return false;
			return this.socket.Connected;
		}}
		private Random IDGenerator = new Random();
		private Socket socket;

		//Packet Handling
		private Dictionary<int, TaskCompletionSource<RCONPacket>> pendingPackets = new Dictionary<int, TaskCompletionSource<RCONPacket>>();
		private TaskCompletionSource<bool> authenticationSuccessful;

		public RCON(string IP_Adress, int port, string password) {
			IPAddress address = null;
			if (!IPAddress.TryParse(IP_Adress, out address)) {
				throw new ArgumentException("Invalid address");
			}
			this.endPoint = new IPEndPoint(address, port);
			this.password = password;
		}

		public async Task StartClient() {
			if (Connected) {
				return;
			}
			this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			this.socket.ReceiveTimeout = timeout;
			this.socket.SendTimeout = timeout;
			this.socket.NoDelay = true;
			await this.socket.ConnectAsync(this.endPoint);

			Start();

			// Wait for successful authentication
			bool authResult = await Authentificate(password);
			if (!authResult) {
				this.socket.Shutdown(SocketShutdown.Both);
				this.socket.Dispose();
				throw new UnauthorizedAccessException("Authentification was not successful");
			}
		}

		private async Task<bool> Authentificate(string password) {
			authenticationSuccessful = new TaskCompletionSource<bool>();
			await this.socket.SendAsync(new RCONPacket(0, RCONPacketType.Auth, password).ToBytes(), SocketFlags.None);
			return await authenticationSuccessful.Task;
		}

		private void Start() {
			if (!Connected) {
				throw new InvalidOperationException("Not connected to a RCON server");
			}

			Task.Run(() => Recieve());

			/*if (heartbeatInterval != 0) {
				Task.Run(() =>
					WatchForDisconnection(heartbeatInterval).ConfigureAwait(false)
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
					if (packet.Type == RCONPacketType.AuthResponse) {
						authenticationSuccessful.SetResult(packet.Id == 0);
					} else {
						RecievedPacket(packet);
					}
				}
			}
		}

		private void RecievedPacket(RCONPacket packet) {
			if (pendingPackets.ContainsKey(packet.Id)) {
				pendingPackets[packet.Id].SetResult(packet);
				pendingPackets.Remove(packet.Id);
			}
		}

		private async Task WatchForDisconnection(int interval) {
			while (Connected) {

			}
		}

		public void Dispose() {
			this.socket.Shutdown(SocketShutdown.Both);
			this.socket.Dispose();
		}

		/// <summary>
		/// Sends a command to the RCON server
		/// </summary>
		/// <returns>A task with the response packet</returns>
		public async Task<RCONPacket> SendCommandAsync(string command, RCONPacketType type = RCONPacketType.ExecCommand) {
			RCONPacket packet = new RCONPacket(GenerateID(), type, command);
			var task = new TaskCompletionSource<RCONPacket>();
			pendingPackets.Add(packet.Id, task);
			await this.socket.SendAsync(packet.ToBytes(), SocketFlags.None);
			return await task.Task;
		}

		/// <summary>
		/// Generates a random id that is not currently used in a pending request
		/// </summary>
		/// <returns>Integer ID</returns>
		private int GenerateID() {
			int id = 0;
			do {
				id = IDGenerator.Next();
			} while (id != 0 && pendingPackets.ContainsKey(id));
			return id;
		}
	}
}
