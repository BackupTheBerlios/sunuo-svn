/***************************************************************************
 *                             PacketHandlers.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *                          (C) 2005 Max Kellermann <max@duempel.org>
 *   email                : max@duempel.org
 *
 *   $Id$
 *   $Author: make $
 *   $Date$
 *
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using Server.Accounting;
using CV = Server.ClientVersion;

namespace Server.Network
{
	public enum MessageType
	{
		Regular = 0x00,
		System = 0x01,
		Emote = 0x02,
		Label = 0x06,
		Focus = 0x07,
		Whisper = 0x08,
		Yell = 0x09,
		Spell = 0x0A,
		Encoded = 0xC0
	}

	public class PacketHandlers
	{
		private static PacketHandler[] m_Handlers;

		public static PacketHandler[] Handlers
		{
			get{ return m_Handlers; }
		}

		static PacketHandlers()
		{
			m_Handlers = new PacketHandler[0x100];

			Register( 0x73,   2, false, new OnPacketReceive( PingReq ) );
			Register( 0x80,  62, false, new OnPacketReceive( AccountLogin ) );
			Register( 0xA0,   3, false, new OnPacketReceive( PlayServer ) );
			Register( 0xCF,   0, false, new OnPacketReceive( AccountLogin ) );
		}

		public static void Register( int packetID, int length, bool ingame, OnPacketReceive onReceive )
		{
			m_Handlers[packetID] = new PacketHandler( packetID, length, ingame, onReceive );
		}

		public static void PingReq( NetState state, PacketReader pvSrc )
		{
			state.Send( PingAck.Instantiate( pvSrc.ReadByte() ) );
		}

		private static int GenerateAuthID()
		{
			int authID = Utility.Random( 1, int.MaxValue - 1 );

			if ( Utility.RandomBool() )
				authID |= 1<<31;

			return authID;
		}

		public static void PlayServer( NetState state, PacketReader pvSrc )
		{
			int index = pvSrc.ReadInt16();
			ServerInfo[] info = state.ServerInfo;
			IAccount a = state.Account;

			if ( info == null || a == null || index < 0 || index >= info.Length )
			{
				state.Dispose();
			}
			else
			{
				ServerInfo si = info[index];

				PlayServerAck.m_AuthID = GenerateAuthID();

				state.SentFirstPacket = false;
				state.Send( new PlayServerAck( si ) );
			}
		}

		public static void AccountLogin( NetState state, PacketReader pvSrc )
		{
			if ( state.SentFirstPacket )
			{
				state.Dispose();
				return;
			}

			state.SentFirstPacket = true;

			string username = pvSrc.ReadString( 30 );
			string password = pvSrc.ReadString( 30 );

			AccountLoginEventArgs e = new AccountLoginEventArgs( state, username, password );

			try {
				EventSink.InvokeAccountLogin(e);
			} catch (Exception ex) {
				Console.WriteLine("Exception disarmed in AccountLogin {0}: {1}",
								  username, ex);
			}

			if ( e.Accepted )
				AccountLogin_ReplyAck( state );
			else
				AccountLogin_ReplyRej( state, e.RejectReason );
		}

		public static void AccountLogin_ReplyAck( NetState state )
		{
			ServerListEventArgs e = new ServerListEventArgs( state, state.Account );

			try {
				EventSink.InvokeServerList(e);
			} catch (Exception ex) {
				Console.WriteLine("Exception disarmed in ServerList: {0}", ex);
				e.Rejected = true;
			}

			if ( e.Rejected )
			{
				state.Account = null;
				state.Send( new AccountLoginRej( ALRReason.BadComm ) );
				state.Dispose();
			}
			else
			{
				ServerInfo[] info = (ServerInfo[])e.Servers.ToArray( typeof( ServerInfo ) );

				state.ServerInfo = info;

				state.Send( new AccountLoginAck( info ) );
			}
		}

		public static void AccountLogin_ReplyRej( NetState state, ALRReason reason )
		{
			state.Send( new AccountLoginRej( reason ) );
			state.Dispose();
		}

		public static PacketHandler GetHandler( int packetID )
		{
			return m_Handlers[packetID];
		}
	}
}
