using System;
using System.Collections;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Scripts.Commands
{
	public class ConvertPlayers
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static void Initialize()
		{
			Server.Commands.Register( "ConvertPlayers", AccessLevel.Administrator, new CommandEventHandler( Convert_OnCommand ) );
		}
		
		public static void Convert_OnCommand( CommandEventArgs e )
		{
			e.Mobile.SendMessage( "Converting all players to PlayerMobile.  You will be disconnected.  Please Restart the server after the world has finished saving." );
			ArrayList mobs = new ArrayList( World.Mobiles.Values );
			int count = 0;
			
			foreach ( Mobile m in mobs )
			{
				if ( m.Player && !(m is PlayerMobile ) )
				{
					count++;
					if ( m.NetState != null )
						m.NetState.Dispose();
					
					PlayerMobile pm = new PlayerMobile( m.Serial );
					pm.DefaultMobileInit();
					
					ArrayList copy = new ArrayList( m.Items );
					for (int i=0;i<copy.Count;i++)
						pm.AddItem( (Item)copy[i] );
					
					CopyProps( pm, m );
					
					for (int i=0;i<m.Skills.Length;i++)
					{
						pm.Skills[i].Base = m.Skills[i].Base;
						pm.Skills[i].SetLockNoRelay( m.Skills[i].Lock );
					}
					
					World.Mobiles[m.Serial] = pm;
				}
			}
			
			if ( count > 0 )
			{
				NetState.ProcessDisposedQueue();
				World.Save();
			
				log.InfoFormat("{0} players have been converted to PlayerMobile.  Restarting the server.",
							   count);
				Core.Shutdown(true);
			}
			else
			{
				log.Info( "Couldn't find any Players to convert." );
			}
		}
		
		private static void CopyProps( Mobile to, Mobile from )
		{
			Type type = typeof( Mobile );
			
			PropertyInfo[] props = type.GetProperties( BindingFlags.Public | BindingFlags.Instance );
			
			for (int p=0;p<props.Length;p++)
			{
				PropertyInfo prop = props[p];
				
				if ( prop.CanRead && prop.CanWrite )
				{
					try
					{
						prop.SetValue( to, prop.GetValue( from, null ), null );
					}
					catch
					{
					}
				}
			}
		}
	}
}
