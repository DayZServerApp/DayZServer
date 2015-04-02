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
using System.Collections;

namespace SSQLib
{
    /// <summary>
    /// Used to retreive information from a Source server
    /// </summary>
    public class SSQL
    {
		private readonly IPEndPoint _ipEnd = null;
		private uint _challenge = 0xFFFFFFFF;

        /// <summary>
        /// Generates an SSQL object with default values
        /// </summary>
		/// <param name="ip_end">The IPEndPoint object storing the IP address and port of the server</param>
		public SSQL(IPEndPoint ip_end)
        {
			_ipEnd = ip_end;
        }

		private static ushort ReadUint16(byte[] buf, ref uint i)
		{
			ushort outVal = System.BitConverter.ToUInt16(buf, (int)i);
			i += sizeof(ushort); //move over the bytes we just read
			return outVal;
		}

		private static int ReadInt32(byte[] buf, ref uint i)
		{
			int outVal = System.BitConverter.ToInt32(buf, (int)i);
			i += sizeof(int); //move over the bytes we just read
			return outVal;
		}

		private static uint ReadUint32(byte[] buf, ref uint i)
		{
			uint outVal = System.BitConverter.ToUInt32(buf, (int)i);
			i += sizeof(uint); //move over the bytes we just read
			return outVal;
		}

		private static ulong ReadUint64(byte[] buf, ref uint i)
		{
			ulong outVal = System.BitConverter.ToUInt64(buf, (int)i);
			i += sizeof(ulong); //move over the bytes we just read
			return outVal;
		}

		private static float ReadFloat(byte[] buf, ref uint i)
		{
			float outVal = System.BitConverter.ToSingle(buf, (int)i);
			i += sizeof(float); //move over the bytes we just read
			return outVal;
		}

		private static string ReadString(byte[] buf, ref uint i)
		{
			var startPos = i;
			while (buf[i] != 0x00) { i++; }
			var length = (i++) - startPos; //++ to move over the null terminator

			return Encoding.UTF8.GetString(buf, (int)startPos, (int)length);
		}

        /// <summary>
        /// Pings the specified Source server to retreive information about it such as the server name, max players, current number of players, etc.
        /// </summary>
        /// <returns>Information about the server or throws an SSQLServerException if it could not be retreived</returns>
        public ServerInfo Server()
        {
            //Create a new empty server info object
            ServerInfo info = new ServerInfo();

            //Create an empty buffer
            byte[] buf = null;

            //Create a new packet and request
            Packet requestPacket = new Packet();
            requestPacket.Data = "TSource Engine Query";

            try
            {
                //Attempt to get the server info
				buf = SocketUtils.getInfo(_ipEnd, requestPacket);
            }
            catch(SSQLServerException e)
            {
                throw e;
            }

			//Record the IP the packet came from
			info.IP = _ipEnd.Address.ToString();

            uint i = 0;

            //Make sure the first character is an I
            if (buf[i++] != 'I') return null;

            //Make sure the returned version is above 0x07
            if (buf[i++] < 0x07) return null;

            //Set the name of the server
			info.Name = ReadString(buf,ref i);

			//Set the name of the map
            info.Map = ReadString(buf, ref i);

            //Get the short name for the game
			info.Folder = ReadString(buf, ref i);

            //Get the friendly game description
			info.Game = ReadString(buf, ref i);

			//read the appId of the game
			var appID = ReadUint16(buf, ref i);

            //Store the app id
            info.AppID = appID.ToString();

            //Get the number of players
            info.PlayerCount = buf[i++].ToString();

            //Get the number of max players
            info.MaxPlayers = buf[i++].ToString();

            //Get the number of bots
            info.BotCount = buf[i++].ToString();

            //Get the dedicated server type
            if (buf[i] == 'l')
                info.Dedicated = ServerInfo.DedicatedType.LISTEN;
            else if (buf[i] == 'd')
                info.Dedicated = ServerInfo.DedicatedType.DEDICATED;
            else if (buf[i] == 'p')
                info.Dedicated = ServerInfo.DedicatedType.SOURCETV;

            //Move to the next byte
            i++;

            //Get the OS type
            if (buf[i] == 'l')
                info.OS = ServerInfo.OSType.LINUX;
            else if (buf[i] == 'w')
                info.OS = ServerInfo.OSType.WINDOWS;

            //Move to the next byte
            i++;

            //Check for password protection
            if (buf[i++] == 0x01) info.Password = true;

            //Check for VAC
            if (buf[i++] == 0x01) info.VAC = true;

			//Get the game version
			info.Version = ReadString(buf, ref i);

			//get EDF
			uint edf = buf[i++];

			if ((edf & 0x80) != 0) //has port number
			{
				ushort portNumber = ReadUint16(buf, ref i);
				info.Port = portNumber.ToString();
			}

			if ((edf & 0x10) != 0) //has server SteamId
			{
				ulong serverSteamId = ReadUint64(buf, ref i);
				info.SteamID = serverSteamId.ToString();
			}

			if ((edf & 0x40) != 0) //has spectator port number and name
			{
				//we currently arent storing these anywhere as they aren't needed

				ushort sourceTvPort = ReadUint16(buf, ref i);
				string sourceTvName = ReadString(buf, ref i);				
			}

			if ((edf & 0x20) != 0) //has keywords
			{
				info.Keywords = ReadString(buf, ref i);
			}

			if ((edf & 0x01) != 0) //has higher precision GameID
			{
				ulong gameId = ReadUint64(buf, ref i);
				ulong preciseAppId = gameId & 0xFFFFFF; //lower 24-bits contain the appId
				info.AppID = preciseAppId.ToString();
			}

			//should be at the end of the packet now

            return info;
        }

		private byte[] PerformBigRequest(byte opcode)
		{
			//Create a request packet
			byte[] rqstBytes = new byte[9];
			rqstBytes[0] = (byte)0xff;
			rqstBytes[1] = (byte)0xff;
			rqstBytes[2] = (byte)0xff;
			rqstBytes[3] = (byte)0xff;
			rqstBytes[4] = (byte)opcode;
			rqstBytes[5] = (byte)((_challenge >> 0) & 0xFF);
			rqstBytes[6] = (byte)((_challenge >> 8) & 0xFF);
			rqstBytes[7] = (byte)((_challenge >> 16) & 0xFF);
			rqstBytes[8] = (byte)((_challenge >> 24) & 0xFF);

			byte[] buf = null;
			try
			{
				//Attempt to get the response
				buf = SocketUtils.getInfo(_ipEnd, rqstBytes);
			}
			catch (SSQLServerException e)
			{
				throw e;
			}

			uint i = 0;
			if (buf[i] == 'A') //if we need to resend the request with new challenge
			{
				i++; //move over the A
				_challenge = ReadUint32(buf, ref i); //read the challenge we got

				//put the challenge into the request
				rqstBytes[5] = (byte)((_challenge >> 0) & 0xFF);
				rqstBytes[6] = (byte)((_challenge >> 8) & 0xFF);
				rqstBytes[7] = (byte)((_challenge >> 16) & 0xFF);
				rqstBytes[8] = (byte)((_challenge >> 24) & 0xFF);

				try
				{
					//Get the actual response
					buf = SocketUtils.getInfo(_ipEnd, rqstBytes);
				}
				catch (SSQLServerException e)
				{
					throw e;
				}
			}

			return buf;
		}

        /// <summary>
        /// Retreives information about the players on a Source server
        /// </summary>
        /// <returns>A List of PlayerInfo or throws an SSQLServerException if the server could not be reached</returns>
        public List<PlayerInfo> Players()
        {
            //Create a new list to store the player array
            var players = new List<PlayerInfo>();

			byte[] buf = PerformBigRequest((byte)'U');
			uint i = 0;

            //Make sure the response starts with D
            if (buf[i++] != 'D') return null;

            //Get the amount of players
            byte numPlayers = buf[i++];

            //Loop through each player and extract their stats
            for (int ii = 0; ii < numPlayers; ii++)
            {
                //Create a new player
                PlayerInfo newPlayer = new PlayerInfo();

                //Set the index of the player (Does not work in L4D2, always returns 0)
                newPlayer.Index = buf[i++];

                newPlayer.Name = ReadString(buf, ref i);

                //Get the score and store them in the player info
                newPlayer.Score = ReadInt32(buf, ref i);

                //Get the time connected as a float and store it in the player info
                newPlayer.Time = ReadFloat(buf, ref i);

                //Add the player to the list
                players.Add(newPlayer);
            }

            //Return the list of players
            return players;
        }

		/// <summary>
		/// Retreives information about the rules on a Source server
		/// </summary>
		/// <returns>A Dictionary of string,string or throws an SSQLServerException if the server could not be reached</returns>
		public Dictionary<string,string> Rules()
		{
			//Create a new dict to store the rules into
			Dictionary<string, string> rules = new Dictionary<string, string>();

			byte[] buf = PerformBigRequest((byte)'V');
			uint i = 0;

			//Make sure the response starts with E
			if (buf[i++] != 'E') return null;

			//Get the amount of rules
			ushort numRules = ReadUint16(buf, ref i);

			//Loop through each rule and add it to the dict
			for (int ii = 0; ii < numRules; ii++)
			{
				string k = ReadString(buf, ref i);
				string v = ReadString(buf, ref i);

				rules.Add(k, v);
			}

			//Return the rules
			return rules;
		}
    }
}
