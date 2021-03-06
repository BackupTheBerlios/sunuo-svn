/***************************************************************************
 *                            TileMatrixPatch.cs
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
using System.Runtime.InteropServices;

namespace Server
{
	public class TileMatrixPatch
	{
		private int m_LandBlocks, m_StaticBlocks;

		public int LandBlocks
		{
			get
			{
				return m_LandBlocks;
			}
		}

		public int StaticBlocks
		{
			get
			{
				return m_StaticBlocks;
			}
		}

		public TileMatrixPatch( TileMatrix matrix, int index )
		{
			if (Core.Config.Features["disable-tile-patch"])
				return;

			string mapDataPath = Core.FindDataFile( "mapdif{0}.mul", index );
			string mapIndexPath = Core.FindDataFile( "mapdifl{0}.mul", index );

			if ( File.Exists( mapDataPath ) && File.Exists( mapIndexPath ) )
				m_LandBlocks = PatchLand( matrix, mapDataPath, mapIndexPath );

			string staDataPath = Core.FindDataFile( "stadif{0}.mul", index );
			string staIndexPath = Core.FindDataFile( "stadifl{0}.mul", index );
			string staLookupPath = Core.FindDataFile( "stadifi{0}.mul", index );

			if ( File.Exists( staDataPath ) && File.Exists( staIndexPath ) && File.Exists( staLookupPath ) )
				m_StaticBlocks = PatchStatics( matrix, staDataPath, staIndexPath, staLookupPath );
		}

		private int PatchLand( TileMatrix matrix, string dataPath, string indexPath )
		{
			using ( FileStream fsData = new FileStream( dataPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
			{
				using ( FileStream fsIndex = new FileStream( indexPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					BinaryReader indexReader = new BinaryReader( fsIndex );

					int count = (int)(indexReader.BaseStream.Length / 4);

					for ( int i = 0; i < count; ++i )
					{
						int blockID = indexReader.ReadInt32();
						int x = blockID / matrix.BlockHeight;
						int y = blockID % matrix.BlockHeight;

						fsData.Seek( 4, SeekOrigin.Current );

						Tile[] tiles = new Tile[64];

						GCHandle handle = GCHandle.Alloc(tiles, GCHandleType.Pinned);
						try {
							if ( m_Buffer == null || 192 > m_Buffer.Length )
								m_Buffer = new byte[192];

							fsData.Read( m_Buffer, 0, 192 );

							Marshal.Copy(m_Buffer, 0, handle.AddrOfPinnedObject(), 192);
						} finally {
							handle.Free();
						}

						matrix.SetLandBlock( x, y, tiles );
					}
					
					indexReader.Close();

					return count;
				}
			}
		}

		private static byte[] m_Buffer;

		private static StaticTile[] m_TileBuffer = new StaticTile[128];

		private int PatchStatics( TileMatrix matrix, string dataPath, string indexPath, string lookupPath )
		{
			using ( FileStream fsData = new FileStream( dataPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
			{
				using ( FileStream fsIndex = new FileStream( indexPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					using ( FileStream fsLookup = new FileStream( lookupPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
					{
						BinaryReader indexReader = new BinaryReader( fsIndex );
						BinaryReader lookupReader = new BinaryReader( fsLookup );

						int count = (int)(indexReader.BaseStream.Length / 4);

						TileList[][] lists = new TileList[8][];

						for ( int x = 0; x < 8; ++x )
						{
							lists[x] = new TileList[8];

							for ( int y = 0; y < 8; ++y )
								lists[x][y] = new TileList();
						}

						for ( int i = 0; i < count; ++i )
						{
							int blockID = indexReader.ReadInt32();
							int blockX = blockID / matrix.BlockHeight;
							int blockY = blockID % matrix.BlockHeight;

							int offset = lookupReader.ReadInt32();
							int length = lookupReader.ReadInt32();
							lookupReader.ReadInt32(); // Extra

							if ( offset < 0 || length <= 0 )
							{
								matrix.SetStaticBlock( blockX, blockY, matrix.EmptyStaticBlock );
								continue;
							}

							fsData.Seek( offset, SeekOrigin.Begin );

							int tileCount = length / 7;

							if ( m_TileBuffer.Length < tileCount )
								m_TileBuffer = new StaticTile[tileCount];

							StaticTile[] staTiles = m_TileBuffer;//new StaticTile[tileCount];

							GCHandle handle = GCHandle.Alloc(staTiles, GCHandleType.Pinned);
							try {
								if ( m_Buffer == null || length > m_Buffer.Length )
									m_Buffer = new byte[length];

								fsData.Read( m_Buffer, 0, length );

								Marshal.Copy(m_Buffer, 0, handle.AddrOfPinnedObject(), length);

								for (int j = 0; j < tileCount; j++)
								{
									StaticTile cur = staTiles[j];
									lists[cur.m_X & 0x7][cur.m_Y & 0x7].Add( (short)((cur.m_ID & 0x3FFF) + 0x4000), cur.m_Z );
								}

								Tile[][][] tiles = new Tile[8][][];

								for ( int x = 0; x < 8; ++x )
								{
									tiles[x] = new Tile[8][];

									for ( int y = 0; y < 8; ++y )
										tiles[x][y] = lists[x][y].ToArray();
								}

								matrix.SetStaticBlock( blockX, blockY, tiles );
							} finally {
								handle.Free();
							}
						}

						indexReader.Close();
						lookupReader.Close();

						return count;
					}
				}
			}
		}
	}
}
