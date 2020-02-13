using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ASS_FFT.RCON {
	public class RCONPacket {
		private static int GUID = 1;

		public const int MAX_PACKET_SIZE = 4200;

		public string Body { get; private set; }
		public int Id { get; private set; }
		public RCONPacketType Type { get; private set; }

		/// <summary>
		/// Create a new packet.
		/// </summary>
		/// <param name="id">Some kind of identifier to keep track of responses from the server.</param>
		/// <param name="type">What the server is supposed to do with the body of this packet.</param>
		/// <param name="body">The actual information held within.</param>
		public RCONPacket(int id, RCONPacketType type, string body) {
			this.Id = id;
			this.Type = type;
			this.Body = body;
		}

		/// <summary>
		/// Create a new packet and generate a GUID for it.
		/// </summary>
		/// <param name="type">What the server is supposed to do with the body of this packet.</param>
		/// <param name="body">The actual information held within.</param>
		public RCONPacket(RCONPacketType type, string body) : this(GetGUID(), type, body) {}

		public override string ToString() => Body;

		/// <summary>
		/// Converts a buffer to a packet.
		/// </summary>
		/// <param name="buffer">Buffer to read.</param>
		/// <returns>Created packet.</returns>
		internal static RCONPacket FromBytes(byte[] buffer) {
			if (buffer == null) throw new NullReferenceException("Byte buffer cannot be null.");
			if (buffer.Length < 4) throw new InvalidDataException("Buffer does not contain a size field.");
			if (buffer.Length > MAX_PACKET_SIZE) throw new InvalidDataException("Buffer is too large for an RCON packet.");

			int size = BitConverter.ToInt32(buffer, 0);
			if (size > buffer.Length - 4) throw new InvalidDataException("Packet size specified was larger then buffer");

			if (size < 10) throw new InvalidDataException("Packet received was invalid.");

			int id = BitConverter.ToInt32(buffer, 4);
			RCONPacketType type = (RCONPacketType)BitConverter.ToInt32(buffer, 8);

			try {
				// Force string to \r\n line endings
				char[] rawBody = Encoding.UTF8.GetChars(buffer, 12, size - 10);
				string body = new string(rawBody, 0, size - 10).TrimEnd();
				body = Regex.Replace(body, @"\r\n|\n\r|\n|\r", "\r\n");
				return new RCONPacket(id, type, body);
			} catch (Exception ex) {
				Console.Error.WriteLine($"{DateTime.Now} - Error reading RCON packet from server: " + ex.Message);
				return new RCONPacket(id, type, "");
			}
		}

		/// <summary>
		/// Serializes a packet to a byte array for transporting over a network. Body is serialized as UTF8.
		/// </summary>
		/// <returns>Byte array with each field.</returns>
		internal byte[] ToBytes() {
			byte[] body = Encoding.ASCII.GetBytes(Body + "\0");
			int length = body.Length;

			using (var packet = new MemoryStream(12 + length)) {
				//Write the length
				packet.Write(BitConverter.GetBytes(9 + length), 0, 4);
				//Write the ID
				packet.Write(BitConverter.GetBytes(Id), 0, 4);
				//Write the Type
				packet.Write(BitConverter.GetBytes((int)Type), 0, 4);
				//Write the actual contents
				packet.Write(body, 0, length);
				//Terminate the packet with a null byte
				packet.Write(new byte[] {0}, 0, 1);

				return packet.ToArray();
			}
		}

		/// <summary>
		/// Provides a new GUID to use for a packet
		/// </summary>
		/// <returns>Created packet.</returns>
		public static int GetGUID() {
			return GUID++;
		}
	}
	
	public enum RCONPacketType {
		// SERVERDATA_RESPONSE_VALUE
		Response = 0,
		// SERVERDATA_AUTH_RESPONSE
		AuthResponse = 2,
		// SERVERDATA_EXECCOMMAND
		ExecCommand = 2,
		// SERVERDATA_AUTH
		Auth = 3
	}
}