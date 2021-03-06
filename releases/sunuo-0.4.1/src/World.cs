/***************************************************************************
 *                                 World.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *                          (C) 2005 Max Kellermann <max@duempel.org>
 *   email                : max@duempel.org
 *
 *   $Id$
 *   $Author$
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
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;
using Server;
using Server.Mobiles;
using Server.Accounting;
using Server.Network;
using Server.Guilds;

namespace Server
{
	public class World
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		protected static readonly bool ManualGC = Core.Config.Features["manual-gc"];

		public enum SaveOption
		{
			Normal,
			Threaded
		}

		public static SaveOption SaveType = SaveOption.Normal;

		private static Hashtable m_Mobiles;
		private static Hashtable m_Items;

		private static int m_LoadErrors;
		private static bool m_Loading;
		private static bool m_Loaded;
		private static bool m_Saving;
		private static ArrayList m_DeleteList;

		public static bool Saving{ get { return m_Saving; } }
		public static int LoadErrors {
			get { return m_LoadErrors; }
		}
		public static bool Loaded{ get { return m_Loaded; } }
		public static bool Loading{ get { return m_Loading; } }

		//static World()
		//{
		//	Load();
		//}

		public static Hashtable Mobiles
		{
			get
			{
				return m_Mobiles;
			}
		}

		public static Hashtable Items
		{
			get
			{
				return m_Items;
			}
		}

		public static bool OnDelete( object o )
		{
			if ( !m_Loading )
				return true;

			m_DeleteList.Add( o );

			return false;
		}

		public static void Broadcast( int hue, bool ascii, string text )
		{
			Packet p;

			if ( ascii )
				p = new AsciiMessage( Serial.MinusOne, -1, MessageType.Regular, hue, 3, "System", text );
			else
				p = new UnicodeMessage( Serial.MinusOne, -1, MessageType.Regular, hue, 3, "ENU", "System", text );

			ArrayList list = NetState.Instances;

			for ( int i = 0; i < list.Count; ++i )
			{
				if ( ((NetState)list[i]).Mobile != null )
					((NetState)list[i]).Send( p );
			}

			NetState.FlushAll();
		}

		public static void Broadcast( int hue, bool ascii, string format, params object[] args )
		{
			Broadcast( hue, ascii, String.Format( format, args ) );
		}

		private struct EntityType {
			private string name;
			private ConstructorInfo ctor;

			public EntityType(string name, ConstructorInfo ctor) {
				this.name = name;
				this.ctor = ctor;
			}

			public string Name {
				get { return name; }
			}

			public ConstructorInfo Constructor {
				get { return ctor; }
			}
		}

		private interface IEntityEntry
		{
			Serial Serial{ get; }
			int TypeID{ get; }
			long Position{ get; }
			int Length{ get; }
			object Object{ get; }
		}

		private struct RegionEntry : IEntityEntry
		{
			private Region m_Region;
			private long m_Position;
			private int m_Length;

			public object Object
			{
				get
				{	
					return m_Region;
				}
			}

			public Serial Serial
			{
				get
				{
					return m_Region == null ? 0 : m_Region.UId;
				}
			}

			public int TypeID
			{
				get
				{
					return 0;
				}
			}

			public long Position
			{
				get
				{
					return m_Position;
				}
			}

			public int Length
			{
				get
				{
					return m_Length;
				}
			}

			public RegionEntry( Region r, long pos, int length )
			{
				m_Region = r;
				m_Position = pos;
				m_Length = length;
			}

			public void Clear() {
				m_Region = null;
			}
		}

		private struct GuildEntry : IEntityEntry
		{
			private BaseGuild m_Guild;
			private long m_Position;
			private int m_Length;

			public object Object
			{
				get
				{	
					return m_Guild;
				}
			}

			public Serial Serial
			{
				get
				{
					return m_Guild == null ? 0 : m_Guild.Id;
				}
			}

			public int TypeID
			{
				get
				{
					return 0;
				}
			}

			public long Position
			{
				get
				{
					return m_Position;
				}
			}

			public int Length
			{
				get
				{
					return m_Length;
				}
			}

			public GuildEntry( BaseGuild g, long pos, int length )
			{
				m_Guild = g;
				m_Position = pos;
				m_Length = length;
			}

			public void Clear() {
				m_Guild = null;
			}
		}

		private struct ItemEntry : IEntityEntry
		{
			private Item m_Item;
			private int m_TypeID;
			private string m_TypeName;
			private long m_Position;
			private int m_Length;

			public object Object
			{
				get
				{	
					return m_Item;
				}
			}

			public Serial Serial
			{
				get
				{
					return m_Item == null ? Serial.MinusOne : m_Item.Serial;
				}
			}

			public int TypeID
			{
				get
				{
					return m_TypeID;
				}
			}

			public string TypeName
			{
				get
				{       
					return m_TypeName;
				}
			}

			public long Position
			{
				get
				{
					return m_Position;
				}
			}

			public int Length
			{
				get
				{
					return m_Length;
				}
			}

			public ItemEntry( Item item, int typeID, string typeName, long pos, int length )
			{
				m_Item = item;
				m_TypeID = typeID;
				m_TypeName = typeName;
				m_Position = pos;
				m_Length = length;
			}

			public void Clear() {
				m_Item = null;
				m_TypeName = null;
			}
		}

		private struct MobileEntry : IEntityEntry
		{
			private Mobile m_Mobile;
			private int m_TypeID;
			private string m_TypeName;
			private long m_Position;
			private int m_Length;

			public object Object
			{
				get
				{	
					return m_Mobile;
				}
			}

			public Serial Serial
			{
				get
				{
					return m_Mobile == null ? Serial.MinusOne : m_Mobile.Serial;
				}
			}

			public int TypeID
			{
				get
				{
					return m_TypeID;
				}
			}

			public string TypeName
			{
				get
				{       
					return m_TypeName;
				}
			}

			public long Position
			{
				get
				{
					return m_Position;
				}
			}

			public int Length
			{
				get
				{
					return m_Length;
				}
			}

			public MobileEntry( Mobile mobile, int typeID, string typeName, long pos, int length )
			{
				m_Mobile = mobile;
				m_TypeID = typeID;
				m_TypeName = typeName;
				m_Position = pos;
				m_Length = length;
			}

			public void Clear() {
				m_Mobile = null;
				m_TypeName = null;
			}
		}

		private static EntityType[] LoadTypes(BinaryReader tdbReader) {
			int count = tdbReader.ReadInt32();

			Type[] ctorTypes = new Type[1]{ typeof( Serial ) };

			EntityType[] types = new EntityType[count];

			for (int i = 0; i < count; ++i) {
				string typeName = tdbReader.ReadString();
				if (typeName == null || typeName == "")
					continue;

				Type t = ScriptCompiler.FindTypeByFullName(typeName);

				if (t == null) {
					log.ErrorFormat("Type '{0}' was not found. All entities referring to it will be deleted.",
									typeName);
					continue;
				}

				ConstructorInfo ctor = t.GetConstructor( ctorTypes );

				if (ctor == null) {
					log.ErrorFormat("Type '{0}' does not have a serialization constructor. All entities referring to it will be deleted.",
									typeName);
					continue;
				}

				types[i] = new EntityType(typeName, ctor);
			}

			return types;
		}

		private static EntityType[] LoadTypes(string path) {
			using (FileStream tdb = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				using (BinaryReader tdbReader = new BinaryReader(tdb)) {
					return LoadTypes(tdbReader);
				}
			}
		}

		private static MobileEntry[] LoadMobileIndex(BinaryReader idxReader,
													 EntityType[] types) {
			int count = idxReader.ReadInt32(), skipped = 0;

			object[] ctorArgs = new object[1];

			m_Mobiles = new Hashtable((count * 11) / 10);
			MobileEntry[] entries = new MobileEntry[count];

			for (int i = 0; i < count; ++i) {
				int typeID = idxReader.ReadInt32();
				Serial serial = (Serial)idxReader.ReadInt32();
				long pos = idxReader.ReadInt64();
				int length = idxReader.ReadInt32();

				if (serial == Serial.MinusOne ||
					typeID < 0 || typeID >= types.Length) {
					++skipped;
					continue;
				}

				EntityType type = types[typeID];
				if (type.Constructor == null) {
					++skipped;
					continue;
				}

				Mobile m = null;

				try {
					ctorArgs[0] = serial;
					m = (Mobile)type.Constructor.Invoke(ctorArgs);
				} catch (Exception e) {
					log.Error(String.Format("Error while creating mobile {0} of type {1}",
											serial, type.Name),
							  e);
				}

				entries[i] = new MobileEntry(m, typeID, type.Name, pos, length);
				AddMobile(m);
			}

			if (skipped > 0) {
				log.WarnFormat("{0} mobiles were skipped", skipped);
				m_LoadErrors += skipped;
			}

			return entries;
		}

		private static MobileEntry[] LoadMobileIndex(string path,
													 EntityType[] ctors) {
			using (FileStream idx = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				using (BinaryReader idxReader = new BinaryReader(idx)) {
					return LoadMobileIndex(idxReader, ctors);
				}
			}
		}

		private static void LoadMobiles(BinaryFileReader reader,
										MobileEntry[] entries) {
			for (int i = 0; i < entries.Length; ++i) {
				MobileEntry entry = entries[i];
				Mobile m = (Mobile)entry.Object;
				if (m == null)
					continue;

				reader.Seek(entry.Position, SeekOrigin.Begin);

				try {
					m_LoadingType = entry.TypeName;
					m.Deserialize(reader);
				} catch (Exception e) {
					log.Error(String.Format("failed to load mobile {0}", m),
							  e);
					m.Delete();
					entries[i].Clear();
					++m_LoadErrors;
					continue;
				}

				if (reader.Position != entry.Position + entry.Length) {
					log.ErrorFormat("Bad deserialize on mobile {0}, type {1}: position={2}, should be {3}",
									entry.Serial, entry.TypeName,
									reader.Position, entry.Position + entry.Length);
					m.Delete();
					entries[i].Clear();
					++m_LoadErrors;
				}
			}
		}

		private static void LoadMobiles(string path,
										MobileEntry[] entries) {
			using (FileStream bin = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				using (BinaryReader binReader = new BinaryReader(bin)) {
					BinaryFileReader reader = new BinaryFileReader(binReader);
					try {
						LoadMobiles(reader, entries);
					} finally {
						reader.Close();
					}
				}
			}
		}

		private static void LoadItems(BinaryFileReader reader,
									  ItemEntry[] entries) {
			for (int i = 0; i < entries.Length; ++i) {
				ItemEntry entry = entries[i];
				Item item = (Item)entry.Object;
				if (item == null)
					continue;

				reader.Seek(entry.Position, SeekOrigin.Begin);

				try {
					m_LoadingType = entry.TypeName;
					item.Deserialize(reader);
				} catch (Exception e) {
					log.Error(String.Format("failed to load item {0}", item),
							  e);
					item.Delete();
					entries[i].Clear();
					++m_LoadErrors;
					continue;
				}

				if (reader.Position != entry.Position + entry.Length) {
					log.ErrorFormat("Bad deserialize on item {0}, type {1}: position={2}, should be {3}",
									entry.Serial, entry.TypeName,
									reader.Position, entry.Position + entry.Length);
					item.Delete();
					entries[i].Clear();
					++m_LoadErrors;
				}
			}
		}

		private static void LoadItems(string path,
									  ItemEntry[] entries) {
			using (FileStream bin = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				using (BinaryReader binReader = new BinaryReader(bin)) {
					BinaryFileReader reader = new BinaryFileReader(binReader);
					try {
						LoadItems(reader, entries);
					} finally {
						reader.Close();
					}
				}
			}
		}

		private static void LoadGuilds(BinaryFileReader reader,
									   GuildEntry[] entries) {
			for (int i = 0; i < entries.Length; ++i) {
				GuildEntry entry = entries[i];
				BaseGuild guild = (BaseGuild)entry.Object;
				if (guild == null)
					continue;

				reader.Seek(entry.Position, SeekOrigin.Begin);

				try {
					guild.Deserialize(reader);
				} catch (Exception e) {
					log.Error(String.Format("failed to load guild", guild),
							  e);
					BaseGuild.List.Remove(guild.Id);
					entries[i].Clear();
					++m_LoadErrors;
					continue;
				}

				if (reader.Position != entry.Position + entry.Length) {
					log.ErrorFormat("Bad deserialize on guild {0}, type {1}: position={2}, should be {3}",
									guild.Id, guild.GetType(),
									reader.Position, entry.Position + entry.Length);
					BaseGuild.List.Remove(guild.Id);
					entries[i].Clear();
					++m_LoadErrors;
				}
			}
		}

		private static void LoadGuilds(string path,
									   GuildEntry[] entries) {
			using (FileStream bin = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				using (BinaryReader binReader = new BinaryReader(bin)) {
					BinaryFileReader reader = new BinaryFileReader(binReader);
					try {
						LoadGuilds(reader, entries);
					} finally {
						reader.Close();
					}
				}
			}
		}

		private static void LoadRegions(BinaryFileReader reader,
										RegionEntry[] entries) {
			for (int i = 0; i < entries.Length; ++i) {
				RegionEntry entry = entries[i];
				Region region = (Region)entry.Object;
				if (region == null)
					continue;

				reader.Seek(entry.Position, SeekOrigin.Begin);

				try {
					region.Deserialize(reader);
				} catch (Exception e) {
					log.Error(String.Format("failed to load region", region),
							  e);
					++m_LoadErrors;
					continue;
				}

				if (reader.Position != entry.Position + entry.Length) {
					log.ErrorFormat("Bad deserialize on region {0}, type {1}: position={2}, should be {3}",
									region.UId, region.GetType(),
									reader.Position, entry.Position + entry.Length);
					++m_LoadErrors;
				}
			}
		}

		private static void LoadRegions(string path,
										RegionEntry[] entries) {
			using (FileStream bin = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				using (BinaryReader binReader = new BinaryReader(bin)) {
					BinaryFileReader reader = new BinaryFileReader(binReader);
					try {
						LoadRegions(reader, entries);
					} finally {
						reader.Close();
					}
				}
			}
		}

		private static ItemEntry[] LoadItemIndex(BinaryReader idxReader,
												 EntityType[] types) {
			int count = idxReader.ReadInt32(), skipped = 0;

			object[] ctorArgs = new object[1];

			m_Items = new Hashtable((count * 11) / 10);
			ItemEntry[] entries = new ItemEntry[count];

			for (int i = 0; i < count; ++i) {
				int typeID = idxReader.ReadInt32();
				Serial serial = (Serial)idxReader.ReadInt32();
				long pos = idxReader.ReadInt64();
				int length = idxReader.ReadInt32();

				if (serial == Serial.MinusOne ||
					typeID < 0 || typeID >= types.Length) {
					++skipped;
					continue;
				}

				EntityType type = types[typeID];
				if (type.Constructor == null) {
					++skipped;
					continue;
				}

				Item item = null;

				try {
					ctorArgs[0] = serial;
					item = (Item)type.Constructor.Invoke(ctorArgs);
				} catch (Exception e) {
					log.Error(String.Format("Error while creating item {0} of type {1}",
											serial, type.Name),
							  e);
					++skipped;
					continue;
				}

				entries[i] = new ItemEntry(item, typeID, type.Name, pos, length);
				AddItem(item);
			}

			if (skipped > 0) {
				log.WarnFormat("{0} items were skipped", skipped);
				m_LoadErrors += skipped;
			}

			return entries;
		}

		private static ItemEntry[] LoadItemIndex(string path,
												 EntityType[] ctors) {
			using (FileStream idx = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				using (BinaryReader idxReader = new BinaryReader(idx)) {
					return LoadItemIndex(idxReader, ctors);
				}
			}
		}

		private static GuildEntry[] LoadGuildIndex(BinaryReader idxReader) {
			int count = idxReader.ReadInt32(), skipped = 0;

			GuildEntry[] entries = new GuildEntry[count];

			CreateGuildEventArgs createEventArgs = new CreateGuildEventArgs(-1);
			for (int i = 0; i < count; ++i) {
				idxReader.ReadInt32();//no typeid for guilds
				int id = idxReader.ReadInt32();
				long pos = idxReader.ReadInt64();
				int length = idxReader.ReadInt32();

				createEventArgs.Id = id;
				BaseGuild guild = EventSink.InvokeCreateGuild(createEventArgs);
				if (guild == null) {
					++skipped;
					continue;
				}

				entries[i] = new GuildEntry(guild, pos, length);
			}

			if (skipped > 0) {
				log.WarnFormat("{0} guilds were skipped", skipped);
				m_LoadErrors += skipped;
			}

			return entries;
		}

		private static GuildEntry[] LoadGuildIndex(string path) {
			using (FileStream idx = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				using (BinaryReader idxReader = new BinaryReader(idx)) {
					return LoadGuildIndex(idxReader);
				}
			}
		}

		private static RegionEntry[] LoadRegionIndex(BinaryReader idxReader) {
			int count = idxReader.ReadInt32(), skipped = 0;

			RegionEntry[] entries = new RegionEntry[count];

			for (int i = 0; i < count; ++i) {
				idxReader.ReadInt32();//no typeid for regions
				int serial = idxReader.ReadInt32();
				long pos = idxReader.ReadInt64();
				int length = idxReader.ReadInt32();

				if (serial == Serial.MinusOne) {
					++skipped;
					continue;
				}

				Region region = Region.FindByUId(serial);
				if (region == null) {
					++skipped;
					continue;
				}

				entries[i] = new RegionEntry(region, pos, length);
				Region.AddRegion(region);
			}

			if (skipped > 0) {
				log.WarnFormat("{0} regions were skipped", skipped);
				m_LoadErrors += skipped;
			}

			return entries;
		}

		private static RegionEntry[] LoadRegionIndex(string path) {
			using (FileStream idx = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				using (BinaryReader idxReader = new BinaryReader(idx)) {
					return LoadRegionIndex(idxReader);
				}
			}
		}

		private static string m_LoadingType;

		public static string LoadingType
		{
			get{ return m_LoadingType; }
		}

		private static void LoadEntities(string saveDirectory) {
			ItemEntry[] itemEntries = null;
			MobileEntry[] mobileEntries = null;
			GuildEntry[] guildEntries = null;
			RegionEntry[] regionEntries = null;

			string mobileBase = Path.Combine(saveDirectory, "Mobiles");
			string mobIdxPath = Path.Combine(mobileBase, "Mobiles.idx");
			string mobTdbPath = Path.Combine(mobileBase, "Mobiles.tdb");
			string mobBinPath = Path.Combine(mobileBase, "Mobiles.bin");

			if ( File.Exists( mobIdxPath ) && File.Exists( mobTdbPath ) )
			{
				log.Debug("loading mobile index");
				EntityType[] types = LoadTypes(mobTdbPath);
				mobileEntries = LoadMobileIndex(mobIdxPath, types);
			}
			else
			{
				m_Mobiles = new Hashtable();
			}

			string itemBase = Path.Combine(saveDirectory, "Items");
			string itemIdxPath = Path.Combine(itemBase, "Items.idx");
			string itemTdbPath = Path.Combine(itemBase, "Items.tdb");
			string itemBinPath = Path.Combine(itemBase, "Items.bin");

			if ( File.Exists( itemIdxPath ) && File.Exists( itemTdbPath ) )
			{
				log.Debug("loading item index");
				EntityType[] types = LoadTypes(itemTdbPath);
				itemEntries = LoadItemIndex(itemIdxPath, types);
			}
			else
			{
				m_Items = new Hashtable();
			}

			string guildBase = Path.Combine(saveDirectory, "Guilds");
			string guildIdxPath = Path.Combine(guildBase, "Guilds.idx");
			string guildBinPath = Path.Combine(guildBase, "Guilds.bin");

			if ( File.Exists( guildIdxPath ) )
			{
				log.Debug("loading guild index");

				guildEntries = LoadGuildIndex(guildIdxPath);
			}

			string regionBase = Path.Combine(saveDirectory, "Regions");
			string regionIdxPath = Path.Combine(regionBase, "Regions.idx");
			string regionBinPath = Path.Combine(regionBase, "Regions.bin");

			if ( File.Exists( regionIdxPath ) )
			{
				log.Debug("loading region index");

				regionEntries = LoadRegionIndex(regionIdxPath);
			}

			if ( File.Exists( mobBinPath ) )
			{
				log.Debug("loading mobiles");

				LoadMobiles(mobBinPath, mobileEntries);
				mobileEntries = null;
			}

			if ( File.Exists( itemBinPath ) )
			{
				log.Debug("loading items");

				LoadItems(itemBinPath, itemEntries);
				itemEntries = null;
			}

			if ( File.Exists( guildBinPath ) )
			{
				log.Debug("loading guilds");

				LoadGuilds(guildBinPath, guildEntries);
				guildEntries = null;
			}

			if ( File.Exists( regionBinPath ) )
			{
				log.Debug("loading regions");

				LoadRegions(regionBinPath, regionEntries);
				regionEntries = null;
			}
		}

		public static void Load()
		{
			if ( m_Loaded )
				return;

			m_Loaded = true;

			log.Info("Loading world");

			DateTime start = DateTime.Now;

			m_LoadErrors = 0;
			m_Loading = true;
			m_DeleteList = new ArrayList();

			LoadEntities(Core.Config.SaveDirectory);

			EventSink.InvokeWorldLoad();

			m_Loading = false;

			for ( int i = 0; i < m_DeleteList.Count; ++i )
			{
				object o = m_DeleteList[i];

				if ( o is Item )
					((Item)o).Delete();
				else if ( o is Mobile )
					((Mobile)o).Delete();
			}

			m_DeleteList.Clear();
			m_DeleteList = null;

			foreach ( Item item in m_Items.Values )
			{
				if ( item.Parent == null )
					item.UpdateTotals();

				item.ClearProperties();
			}

			ArrayList list = new ArrayList( m_Mobiles.Values );

			foreach ( Mobile m in list )
			{
				m.ForceRegionReEnter( true );
				m.UpdateTotals();

				m.ClearProperties();
			}

			if (ManualGC) {
				log.Debug("Manual garbage collection");
				System.GC.Collect();
			}

			log.InfoFormat("World loaded: {1} items, {2} mobiles ({0:F1} seconds)",
						   (DateTime.Now-start).TotalSeconds,
						   m_Items.Count, m_Mobiles.Count);
		}

		public static void Save()
		{
			Save( true );
		}

		private class SaveItemsStart
		{
			private string itemBase;

			public SaveItemsStart(string _itemBase)
			{
				itemBase = _itemBase;
			}

			public void SaveItems() {
				World.SaveItems(itemBase);
			}
		}

		public static void Save(string saveDirectory, bool message)
		{
			if ( m_Saving || AsyncWriter.ThreadCount > 0 ) 
				return;

			NetState.FlushAll();
			
			m_Saving = true;

			if ( message )
				Broadcast( 0x35, true, "The world is saving, please wait." );

			log.Info( "Saving world" );

			DateTime startTime = DateTime.Now;

			string mobileBase = Path.Combine(saveDirectory, "Mobiles");
			string itemBase = Path.Combine(saveDirectory, "Items");
			string guildBase = Path.Combine(saveDirectory, "Guilds");
			string regionBase = Path.Combine(saveDirectory, "Regions");

			if (Core.Config.Features["multi-threading"])
			{
				Thread saveThread = new Thread(new ThreadStart(new SaveItemsStart(itemBase).SaveItems));

				saveThread.Name = "Item Save Subset";
				saveThread.Start();

				SaveMobiles(mobileBase);
				SaveGuilds(guildBase);
				SaveRegions(regionBase);

				saveThread.Join();
			}
			else
			{
				SaveMobiles(mobileBase);
				SaveItems(itemBase);
				SaveGuilds(guildBase);
				SaveRegions(regionBase);
			}

			log.InfoFormat("Entities saved in {0:F1} seconds.",
						   (DateTime.Now - startTime).TotalSeconds);

			//Accounts.Save();

			try
			{
				EventSink.InvokeWorldSave(new WorldSaveEventArgs(saveDirectory, message));
			}
			catch ( Exception e )
			{
				throw new Exception( "World Save event threw an exception.  Save failed!", e );
			}

			if (ManualGC) {
				log.Debug("Manual garbage collection");
				System.GC.Collect();
			}

			DateTime endTime = DateTime.Now;
			log.InfoFormat("World saved in {0:F1} seconds.",
						   (endTime - startTime).TotalSeconds);

			if ( message )
				Broadcast( 0x35, true, "World save complete. The entire process took {0:F1} seconds.", (endTime - startTime).TotalSeconds );

			m_Saving = false;
		}

		private static void MoveDirectoryContents(string src, string dst) {
			foreach (string name in Directory.GetFileSystemEntries(src)) {
				string baseName = Path.GetFileName(name);
				if (baseName != "tmp")
					Directory.Move(name, Path.Combine(dst, baseName));
			}
		}

		public static void Save( bool message )
		{
			/* create "./Saves/tmp/old" */

			string saveDirectory = Core.Config.SaveDirectory;
			if (!Directory.Exists(saveDirectory))
				Directory.CreateDirectory(saveDirectory);

			string tmpDirectory = Path.Combine(saveDirectory, "tmp");
			if (Directory.Exists(tmpDirectory))
				Directory.Delete(tmpDirectory, true);
			else if (File.Exists(tmpDirectory))
				File.Delete(tmpDirectory);
			Directory.CreateDirectory(tmpDirectory);

			string oldDirectory = Path.Combine(tmpDirectory, "old");
			Directory.CreateDirectory(oldDirectory);

			/* move current save to "./Saves/tmp/old/" */

			MoveDirectoryContents(saveDirectory, oldDirectory);

			try {
				/* save to "./Saves/" */

				Save(saveDirectory, message);

			} catch {
				/* rollback */

				string newDirectory = Path.Combine(tmpDirectory, "new");
				Directory.CreateDirectory(newDirectory);

				MoveDirectoryContents(saveDirectory, newDirectory);
				MoveDirectoryContents(oldDirectory, saveDirectory);

				throw;
			} finally {
				Directory.Delete(tmpDirectory, true);
			}
		}

		private static void SaveMobiles(string mobileBase)
		{
			ArrayList restock = new ArrayList();

			GenericWriter idx;
			GenericWriter tdb;
			GenericWriter bin;

			if (!Directory.Exists(mobileBase))
				Directory.CreateDirectory(mobileBase);

			string mobIdxPath = Path.Combine(mobileBase, "Mobiles.idx");
			string mobTdbPath = Path.Combine(mobileBase, "Mobiles.tdb");
			string mobBinPath = Path.Combine(mobileBase, "Mobiles.bin");

			if ( SaveType == SaveOption.Normal )
			{
				idx = new BinaryFileWriter( mobIdxPath, false );
				tdb = new BinaryFileWriter( mobTdbPath, false );
				bin = new BinaryFileWriter( mobBinPath, true );
			} 
			else
			{
				idx = new AsyncWriter( mobIdxPath, false );
				tdb = new AsyncWriter( mobTdbPath, false );
				bin = new AsyncWriter( mobBinPath, true );
			}

			idx.Write( (int) m_Mobiles.Count );
			foreach ( Mobile m in m_Mobiles.Values )
			{
				long start = bin.Position;

				idx.Write( (int) m.m_TypeRef );
				idx.Write( (int) m.Serial );
				idx.Write( (long) start );

				m.Serialize( bin );

				idx.Write( (int) (bin.Position - start) );

				if ( m is IVendor )
				{
					if ( ((IVendor)m).LastRestock + ((IVendor)m).RestockDelay < DateTime.Now )
						restock.Add( m );
				}

				m.FreeCache();
			}

			tdb.Write( (int) m_MobileTypes.Count );
			for ( int i = 0; i < m_MobileTypes.Count; ++i )
				tdb.Write( ((Type)m_MobileTypes[i]).FullName );

			for (int i=0;i<restock.Count;i++)
			{
				IVendor vend = (IVendor)restock[i];
				vend.Restock();
				vend.LastRestock = DateTime.Now;
			}

			idx.Close();
			tdb.Close();
			bin.Close();
		}

		internal static ArrayList m_ItemTypes = new ArrayList();
		internal static ArrayList m_MobileTypes = new ArrayList();

		private static void SaveItems(string itemBase)
		{
			string itemIdxPath = Path.Combine( itemBase, "Items.idx" );
			string itemTdbPath = Path.Combine( itemBase, "Items.tdb" );
			string itemBinPath = Path.Combine( itemBase, "Items.bin" );

			ArrayList decaying = new ArrayList();

			GenericWriter idx;
			GenericWriter tdb;
			GenericWriter bin;

			if (!Directory.Exists(itemBase))
				Directory.CreateDirectory(itemBase);

			if ( SaveType == SaveOption.Normal )
			{
				idx = new BinaryFileWriter( itemIdxPath, false );
				tdb = new BinaryFileWriter( itemTdbPath, false );
				bin = new BinaryFileWriter( itemBinPath, true );
			} 
			else
			{
				idx = new AsyncWriter( itemIdxPath, false );
				tdb = new AsyncWriter( itemTdbPath, false );
				bin = new AsyncWriter( itemBinPath, true );
			}

			idx.Write( (int) m_Items.Count );
			foreach ( Item item in m_Items.Values )
			{
				if ( item.Decays && item.Parent == null && item.Map != Map.Internal && (item.LastMoved + item.DecayTime) <= DateTime.Now )
					decaying.Add( item );

				long start = bin.Position;

				idx.Write( (int) item.m_TypeRef );
				idx.Write( (int) item.Serial );
				idx.Write( (long) start );

				item.Serialize( bin );

				idx.Write( (int) (bin.Position - start) );

				item.FreeCache();
			}

			tdb.Write( (int) m_ItemTypes.Count );
			for ( int i = 0; i < m_ItemTypes.Count; ++i )
				tdb.Write( ((Type)m_ItemTypes[i]).FullName );

			idx.Close();
			tdb.Close();
			bin.Close();

			for ( int i = 0; i < decaying.Count; ++i )
			{
				Item item = (Item)decaying[i];

				if ( item.OnDecay() )
					item.Delete();
			}
		}

		private static void SaveGuilds(string guildBase)
		{
			string guildIdxPath = Path.Combine(guildBase, "Guilds.idx");
			string guildBinPath = Path.Combine(guildBase, "Guilds.bin");

			GenericWriter idx;
			GenericWriter bin;

			if (!Directory.Exists(guildBase))
				Directory.CreateDirectory(guildBase);

			if ( SaveType == SaveOption.Normal )
			{
				idx = new BinaryFileWriter( guildIdxPath, false );
				bin = new BinaryFileWriter( guildBinPath, true );
			} 
			else
			{
				idx = new AsyncWriter( guildIdxPath, false );
				bin = new AsyncWriter( guildBinPath, true );
			}

			idx.Write( (int) BaseGuild.List.Count );
			foreach ( BaseGuild guild in BaseGuild.List.Values )
			{
				long start = bin.Position;

				idx.Write( (int)0 );//guilds have no typeid
				idx.Write( (int)guild.Id );
				idx.Write( (long)start );

				guild.Serialize( bin );

				idx.Write( (int) (bin.Position - start) );
			}

			idx.Close();
			bin.Close();
		}

		private static void SaveRegions(string regionBase)
		{
			string regionIdxPath = Path.Combine( regionBase, "Regions.idx" );
			string regionBinPath = Path.Combine( regionBase, "Regions.bin" );

			int count = 0;

			GenericWriter bin;

			if (!Directory.Exists(regionBase))
				Directory.CreateDirectory(regionBase);

			if ( SaveType == SaveOption.Normal )
				bin = new BinaryFileWriter( regionBinPath, true );
			else
				bin = new AsyncWriter( regionBinPath, true );

			MemoryStream mem = new MemoryStream( 4 + (20*Region.Regions.Count) );
			BinaryWriter memIdx = new BinaryWriter( mem );

			memIdx.Write( (int)0 );

			for ( int i = 0; i < Region.Regions.Count; ++i )
			{
				Region region = (Region)Region.Regions[i];

				if ( region.Saves )
				{
					++count;
					long start = bin.Position;

					memIdx.Write( (int)0 );//typeid
					memIdx.Write( (int) region.UId );
					memIdx.Write( (long) start );

					region.Serialize( bin );

					memIdx.Write( (int) (bin.Position - start) );
				}
			}

			bin.Close();
			
			memIdx.Seek( 0, SeekOrigin.Begin );
			memIdx.Write( (int)count );

			if ( SaveType == SaveOption.Threaded )
			{
				AsyncWriter asyncIdx = new AsyncWriter( regionIdxPath, false );
				asyncIdx.MemStream = mem;
				asyncIdx.Close();
			}
			else
			{
				FileStream fs = new FileStream( regionIdxPath, FileMode.Create, FileAccess.Write, FileShare.None );
				mem.WriteTo( fs );
				fs.Close();
				mem.Close();
			}
			
			// mem is closed only in non threaded saves, as its reference is copied to asyncIdx.MemStream
			memIdx.Close();
		}

		public static IEntity FindEntity( Serial serial )
		{
			if ( serial.IsItem )
			{
				return (Item)m_Items[serial];
			}
			else if ( serial.IsMobile )
			{
				return (Mobile)m_Mobiles[serial];
			}
			else
			{
				return null;
			}
		}

		public static Mobile FindMobile( Serial serial )
		{
			return (Mobile)m_Mobiles[serial];
		}

		public static void AddMobile( Mobile m )
		{
			m_Mobiles[m.Serial] = m;
		}

		public static Item FindItem( Serial serial )
		{
			return (Item)m_Items[serial];
		}

		public static void AddItem( Item item )
		{
			m_Items[item.Serial] = item;
		}

		public static void RemoveMobile( Mobile m )
		{
			m_Mobiles.Remove( m.Serial );
		}

		public static void RemoveItem( Item item )
		{
			m_Items.Remove( item.Serial );
		}
	}
}
