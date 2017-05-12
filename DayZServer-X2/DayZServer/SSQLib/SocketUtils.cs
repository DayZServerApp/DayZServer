/*
 * This file is part of SSQLib.
 *
 *   SSQLib is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU Lesser General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   SSQLib is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU Lesser General Public License for more details.
 *
 *   You should have received a copy of the GNU Lesser General Public License
 *   along with SSQLib.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace SSQLib
{
    internal class SocketUtils
    {
        private SocketUtils() { }

        internal static byte[] getInfo(IPEndPoint ipe, Packet packet)
        {
            //Create the socket
            Socket srvSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            //Save the max packet size
            int packetSize = 12288;

            //Send/Receive timeouts
            srvSocket.SendTimeout = 3000;
            srvSocket.ReceiveTimeout = 3000;

            try
            {
                //Send the request to the server
                srvSocket.SendTo(packet.outputAsBytes(), ipe);
            }
            catch (SocketException se)
            {
                throw new SSQLServerException("Could not send packet to server {" + se.Message + "}");
            }

            //Create a new receive buffer
            byte[] rcvPacketInfo = new byte[packetSize];
            EndPoint Remote = (EndPoint)ipe;

			int recvdBytes = -1;
            try
            {
                //Receive the data from the server
				recvdBytes = srvSocket.ReceiveFrom(rcvPacketInfo, ref Remote);
            }
            catch (SocketException se)
            {
                throw new SSQLServerException("Could not receive packet from server {" + se.Message + "}");
            }

			if (recvdBytes < sizeof(uint))
				return null;

			uint headerInt = BitConverter.ToUInt32(rcvPacketInfo, 0);
			if (headerInt != 0xFFFFFFFF) //we only support simple non-split packets for this query
				return null;

			int realDataSize = recvdBytes - sizeof(uint);

			//Send the packet data back
			byte[] retnData = new byte[realDataSize];
			Array.Copy(rcvPacketInfo, sizeof(uint), retnData,  0, realDataSize);
			return retnData;
        }

		internal static byte[] getInfo(IPEndPoint ipe, byte[] request)
		{
			//Create the socket
			Socket srvSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

			//Save the max packet size
			int packetSize = 12288;

			//Send/Receive timeouts
			srvSocket.SendTimeout = 3000;
			srvSocket.ReceiveTimeout = 3000;

			try
			{
				//Send the request to the server
				srvSocket.SendTo(request, ipe);
			}
			catch (SocketException se)
			{
				throw new SSQLServerException("Could not send packet to server {" + se.Message + "}");
			}

			//Create a new receive buffer
			byte[] tempPacketBuf = new byte[packetSize];
			EndPoint Remote = (EndPoint)ipe;

			int recvdBytes = -1;
			try
			{
				//Receive the data from the server
				recvdBytes = srvSocket.ReceiveFrom(tempPacketBuf, ref Remote);
			}
			catch (SocketException se)
			{
				throw new SSQLServerException("Could not receive first packet from server {" + se.Message + "}");
			}

			if (recvdBytes < sizeof(uint))
				return null;

			uint headerInt = BitConverter.ToUInt32(tempPacketBuf, 0);
			if (headerInt == 0xFFFFFFFF) //single packet response
			{
				int realDataSize = recvdBytes - sizeof(uint);

				byte[] retnData = new byte[realDataSize];
				Array.Copy(tempPacketBuf, sizeof(uint), retnData, 0, realDataSize);
				return retnData;
			}
			else if (headerInt == 0xFFFFFFFE) //multiple packet response
			{
				uint answerId = 0;
				int totalPackets = 1;
				byte[][] packets = null;

				for (int currentPacket = 0; currentPacket < totalPackets; currentPacket++)
				{
					if (currentPacket > 0) //already have the first
					{
						try { recvdBytes = srvSocket.ReceiveFrom(tempPacketBuf, ref Remote); }
						catch (SocketException se)
						{
							throw new SSQLServerException("Could not receive later packet from server {" + se.Message + "}");
						}
					}

					int offs = sizeof(uint);
					uint newAnswerId = BitConverter.ToUInt32(tempPacketBuf, offs);
					offs += sizeof(uint);

					uint newTotalPackets = tempPacketBuf[offs++];
					uint newPacketNumber = tempPacketBuf[offs++];

					ushort maxPacketSize = BitConverter.ToUInt16(tempPacketBuf, offs);
					offs += sizeof(ushort);

					if ((newAnswerId & 0x80000000) != 0) //indicates compressed
					{
						uint decomprSize = BitConverter.ToUInt32(tempPacketBuf, offs);
						offs += sizeof(uint);

						uint decomprCrc = BitConverter.ToUInt32(tempPacketBuf, offs);
						offs += sizeof(uint);

						return null; //handle compressed if necessary, not for me.
					}

					if (packets == null)
					{
						answerId = newAnswerId;
						totalPackets = (int)newTotalPackets;
						packets = new byte[totalPackets][];
					}
					else if (newAnswerId != answerId)
					{
						currentPacket--;
						continue;
					}

					int realDataSize = recvdBytes - offs;
					packets[newPacketNumber] = new byte[realDataSize];
					Array.Copy(tempPacketBuf, offs, packets[newPacketNumber], 0, realDataSize);
				}

				int totalDataSize = 0;
				foreach (byte[] packetBytes in packets)
					totalDataSize += packetBytes.Length;

				byte[] totalData = new byte[totalDataSize];
				int copiedDataSize = 0;
				foreach (byte[] packetBytes in packets)
				{
					Array.Copy(packetBytes, 0, totalData, copiedDataSize, packetBytes.Length);
					copiedDataSize += packetBytes.Length;
				}

				return totalData;
			}
			else //unknown packet splitting format
				return null;
		}
    }
}
