using System;
using System.IO;
using Microsoft.Win32;
using Server;

namespace Server.Misc
{
	public class DataPath
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/* The following is a list of files which a required for proper execution:
		 * 
		 * Multi.idx
		 * Multi.mul
		 * VerData.mul
		 * TileData.mul
		 * Map*.mul
		 * StaIdx*.mul
		 * Statics*.mul
		 * MapDif*.mul
		 * MapDifL*.mul
		 * StaDif*.mul
		 * StaDifL*.mul
		 * StaDifI*.mul
		 */

		public static void Configure()
		{
			string pathReg = GetExePath( "Ultima Online" );
			string pathTD = GetExePath( "Ultima Online Third Dawn" );

			if ( pathReg != null )
				Core.DataDirectories.Add( pathReg );

			if ( pathTD != null )
				Core.DataDirectories.Add( pathTD );

			if ( Core.DataDirectories.Count == 0 )
				log.Error("please configure a data-path in etc/sunuo.xml");
		}

		private static string GetExePath( string subName )
		{
			try
			{
				using ( RegistryKey key = Registry.LocalMachine.OpenSubKey( String.Format( @"SOFTWARE\Origin Worlds Online\{0}\1.0", subName ) ) )
				{
					if ( key == null )
						return null;

					string v = key.GetValue( "ExePath" ) as string;

					if ( v == null || v.Length <= 0 )
						return null;

					if ( !File.Exists( v ) )
						return null;

					v = Path.GetDirectoryName( v );

					if ( v == null )
						return null;

					return v;
				}
			}
			catch
			{
				return null;
			}
		}
	}
}