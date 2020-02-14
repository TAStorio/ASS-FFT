using System;
using System.Collections.Generic;

namespace ASS_FFT.RCON {
	public class RCONPacketBuilder {
		private bool constructingPacket = false;

		//Data about the currently constructed packet
		private int packetBufferIndex = 0;
		private int packetSize = 0;
		private byte[] packetBuffer;

		//Used for constructing the size argument
		private int sizeBufferIndex = 0;
		private byte[] sizeBuffer = new byte[4];

		private Queue<RCONPacket> packets = new Queue<RCONPacket>();
		
		/// <summary>
		/// Consumes recieved bytes and constructs packets from them, returns how many new packets have been constructed.
		/// </summary>
		/// <param name="buffer">Buffer to read.</param>
		/// <param name="count">How many bytes to read from the buffer</param>
		/// <param name="offset">Starting read offset (defaults to 0)</param>
		/// <returns>Created packet.</returns>
		public int FeedBytes(byte[] buffer, int count, int offset = 0) {
			for (int i = offset; i < count + offset; i++) {
				if (constructingPacket) {
					//Write the byte to memory
					packetBuffer[packetBufferIndex] = buffer[i];
					packetBufferIndex++;
					//The packet is finished
					if (packetBufferIndex >= packetSize) {
						packetBufferIndex = 0;
						packets.Enqueue(RCONPacket.FromBytes(packetBuffer));
						constructingPacket = false;
					}
				} else {
					//We need to find out the packetsize
					sizeBuffer[sizeBufferIndex] = buffer[i];
					sizeBufferIndex++;
					//We got the full 4 byte number
					if (sizeBufferIndex >= 4) {
						packetSize = 4 + BitConverter.ToInt32(sizeBuffer, 0);
						sizeBufferIndex = 0;
						packetBuffer = new byte[packetSize];
						packetBuffer[0] = sizeBuffer[0];
						packetBuffer[1] = sizeBuffer[1];
						packetBuffer[2] = sizeBuffer[2];
						packetBuffer[3] = sizeBuffer[3];
						packetBufferIndex = 4;
						constructingPacket = true;
					}
				}
			}
			return 0;
		}

		//Functions used for retrieving the packets
		public int AvailablePackets {get {return this.packets.Count;}}

		public RCONPacket GetPacket() {
			if (AvailablePackets == 0) throw new InvalidOperationException("No packets available");
			return this.packets.Dequeue();
		}

		public RCONPacket[] GetAllPackets() {
			if (AvailablePackets == 0) throw new InvalidOperationException("No packets available");
			var packetarray = this.packets.ToArray();
			this.packets.Clear();
			return packetarray;
		}
	}
}
