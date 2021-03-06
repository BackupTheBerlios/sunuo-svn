/***************************************************************************
 *                               MessagePump.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: MessagePump.cs,v 1.4 2005/01/22 04:25:04 krrios Exp $
 *   $Author: krrios $
 *   $Date: 2005/01/22 04:25:04 $
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
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Server;
using Server.Network;

namespace Server.Network
{
	public class MessagePump
	{
		private Listener[] m_Listeners;
		private Queue m_Queue;
		private Queue m_Throttled;
		private byte[] m_Peek;

		public MessagePump( Listener l )
		{
			m_Listeners = new Listener[]{ l };
			m_Queue = new Queue();
			m_Throttled = new Queue();
			m_Peek = new byte[4];
		}

		public Listener[] Listeners
		{
			get{ return m_Listeners; }
			set{ m_Listeners = value; }
		}

		public void AddListener( Listener l )
		{
			Listener[] old = m_Listeners;

			m_Listeners = new Listener[old.Length + 1];

			for ( int i = 0; i < old.Length; ++i )
				m_Listeners[i] = old[i];

			m_Listeners[old.Length] = l;
		}

		private void CheckListener()
		{
			for ( int j = 0; j < m_Listeners.Length; ++j )
			{
				Socket[] accepted = m_Listeners[j].Slice();

				for ( int i = 0; i < accepted.Length; ++i )
				{
					NetState ns = new NetState( accepted[i], this );
					ns.Start();

					if ( ns.Running )
						Console.WriteLine( "Client: {0}: Connected. [{1} Online]", ns, NetState.Instances.Count );
				}
			}
		}

		public void OnReceive( NetState ns )
		{
			lock ( m_Queue.SyncRoot )
			{
				m_Queue.Enqueue( ns );
			}
		}

		public void Slice()
		{
			CheckListener();

			lock ( m_Queue.SyncRoot )
			{
				while ( m_Queue.Count > 0 )
				{
					NetState ns = (NetState)m_Queue.Dequeue();

					if ( ns.Running )
					{
						if ( HandleReceive( ns ) && ns.Running )
							ns.Continue();
					}
				}

				while ( m_Throttled.Count > 0 )
					m_Queue.Enqueue( m_Throttled.Dequeue() );
			}
		}

		private const int BufferSize = 1024;
		private BufferPool m_Buffers = new BufferPool( 4, BufferSize );

		public bool HandleReceive( NetState ns )
		{
			lock ( ns )
			{
				ByteQueue buffer = ns.Buffer;

				if ( buffer == null )
					return true;

				int length = buffer.Length;

				if ( !ns.Seeded )
				{
					if ( length >= 4 )
					{
						buffer.Dequeue( m_Peek, 0, 4 );

						int seed = (m_Peek[0] << 24) | (m_Peek[1] << 16) | (m_Peek[2] << 8) | m_Peek[3];

						//Console.WriteLine( "Login: {0}: Seed is 0x{1:X8}", ns, seed );

						if ( seed == 0 )
						{
							Console.WriteLine( "Login: {0}: Invalid client detected, disconnecting", ns );
							ns.Dispose();
							return false;
						}

						ns.m_Seed = seed;
						ns.Seeded = true;
					}

					return true;
				}

				//Console.WriteLine( "{" );

				while ( length > 0 && ns.Running )
				{
					int packetID = buffer.GetPacketID();

					if ( !ns.SentFirstPacket && packetID != 0xF1 && packetID != 0xCF && packetID != 0x80 && packetID != 0x91 && packetID != 0xA4 )
					{
						Console.WriteLine( "Client: {0}: Encrypted client detected, disconnecting", ns );
						ns.Dispose();
						break;
					}

					PacketHandler handler = PacketHandlers.GetHandler( packetID );

					if ( handler == null )
					{
						byte[] data = new byte[length];
						length = buffer.Dequeue( data, 0, length );

						new PacketReader( data, length, false ).Trace( ns );

						break;
					}

					int packetLength = handler.Length;

					if ( packetLength <= 0 )
					{
						if ( length >= 3 )
						{
							packetLength = buffer.GetPacketLength();

							if ( packetLength < 3 )
							{
								ns.Dispose();
								break;
							}
						}
						else
						{
							break;
						}
					}

					if ( length >= packetLength )
					{
						if ( handler.Ingame && ns.Mobile == null )
						{
							Console.WriteLine( "Client: {0}: Sent ingame packet (0x{1:X2}) before having been attached to a mobile", ns, packetID );
							ns.Dispose();
							break;
						}
						else if ( handler.Ingame && ns.Mobile.Deleted )
						{
							ns.Dispose();
							break;
						}
						else
						{
							ThrottlePacketCallback throttler = handler.ThrottleCallback;

							if ( throttler != null && !throttler( ns ) )
							{
								m_Throttled.Enqueue( ns );
								//Console.WriteLine( "}" );
								return false;
							}

							//Console.WriteLine( handler.OnReceive.Method.Name );

							PacketProfile profile = PacketProfile.GetIncomingProfile( packetID );
							DateTime start = ( profile == null ? DateTime.MinValue : DateTime.Now );

							byte[] packetBuffer;

							if ( BufferSize >= packetLength )
								packetBuffer = m_Buffers.AquireBuffer();
							else
								packetBuffer = new byte[packetLength];

							packetLength = buffer.Dequeue( packetBuffer, 0, packetLength );

							PacketReader r =  new PacketReader( packetBuffer, packetLength, handler.Length != 0 );

							handler.OnReceive( ns, r );
							length = buffer.Length;

							if ( BufferSize >= packetLength )
								m_Buffers.ReleaseBuffer( packetBuffer );

							if ( profile != null )
								profile.Record( packetLength, DateTime.Now - start );

							//Console.WriteLine( "Client: {0}: Unhandled packet 0x{1:X2}", ns, packetID );
							//Utility.FormatBuffer( Console.Out, new System.IO.MemoryStream( r.Buffer ), r.Buffer.Length );
						}
					}
					else
					{
						break;
					}
				}

				//Console.WriteLine( "}" );
			}

			return true;
		}
	}
}