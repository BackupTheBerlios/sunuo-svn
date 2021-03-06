/***************************************************************************
 *                                EventLog.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: EventLog.cs,v 1.3 2005/01/22 04:25:04 krrios Exp $
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
using System.Diagnostics;
using DiagELog = System.Diagnostics.EventLog;

namespace Server
{
	public class EventLog
	{
		static EventLog()
		{
			if ( !DiagELog.SourceExists( "SunUO" ) )
			{
				DiagELog.CreateEventSource( "SunUO", "Application" );
			}
		}

		public static void Error( int eventID, string text )
		{
			DiagELog.WriteEntry( "SunUO", text, EventLogEntryType.Error, eventID );
		}

		public static void Error( int eventID, string format, params object[] args )
		{
			Error( eventID, String.Format( format, args ) );
		}

		public static void Warning( int eventID, string text )
		{
			DiagELog.WriteEntry( "SunUO", text, EventLogEntryType.Warning, eventID );
		}

		public static void Warning( int eventID, string format, params object[] args )
		{
			Warning( eventID, String.Format( format, args ) );
		}

		public static void Inform( int eventID, string text )
		{
			DiagELog.WriteEntry( "SunUO", text, EventLogEntryType.Information, eventID );
		}

		public static void Inform( int eventID, string format, params object[] args )
		{
			Inform( eventID, String.Format( format, args ) );
		}
	}
}
