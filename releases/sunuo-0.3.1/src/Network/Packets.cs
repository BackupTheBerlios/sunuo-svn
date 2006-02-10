/***************************************************************************
 *                                Packets.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id: Packets.cs,v 1.9 2005/01/22 04:25:04 krrios Exp $
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
using Server.Accounting;
using Server.Targeting;
using Server.Items;
using Server.Mobiles;
using Server.Gumps;
using Server.Menus;
using Server.Menus.ItemLists;
using Server.Menus.Questions;
using Server.Prompts;
using Server.HuePickers;
using Server.ContextMenus;

namespace Server.Network
{
	public enum PMMessage : byte
	{
		CharNoExist = 1,
		CharExists = 2,
		CharInWorld = 5,
		LoginSyncError = 6,
		IdleWarning = 7
	}

	public enum LRReason : byte
	{
		CannotLift = 0,
		OutOfRange = 1,
		OutOfSight = 2,
		TryToSteal = 3,
		AreHolding = 4,
		Inspecific = 5
	}

	/*public enum CMEFlags
	{
		None = 0x00,
		Locked = 0x01,
		Arrow = 0x02,
		x0004 = 0x04,
		Color = 0x20,
		x0040 = 0x40,
		x0080 = 0x80
	}*/

	public sealed class DamagePacketOld : Packet
	{
		public DamagePacketOld( Mobile m, int amount ) : base( 0xBF )
		{
			EnsureCapacity( 11 );

			m_Stream.Write( (short) 0x22 );
			m_Stream.Write( (byte) 1 );
			m_Stream.Write( (int) m.Serial );

			if ( amount > 255 )
				amount = 255;
			else if ( amount < 0 )
				amount = 0;

			m_Stream.Write( (byte)amount );
		}
	}

	public sealed class DamagePacket : Packet
	{
		public static readonly ClientVersion Version = new ClientVersion( "4.0.7a" );

		public DamagePacket( Mobile m, int amount ) : base( 0x0B, 7 )
		{
			m_Stream.Write( (int) m.Serial );

			if ( amount > 0xFFFF )
				amount = 0xFFFF;
			else if ( amount < 0 )
				amount = 0;

			m_Stream.Write( (ushort) amount );
		}

		/*public DamagePacket( Mobile m, int amount ) : base( 0xBF )
		{
			EnsureCapacity( 11 );

			m_Stream.Write( (short) 0x22 );
			m_Stream.Write( (byte) 1 );
			m_Stream.Write( (int) m.Serial );

			if ( amount > 255 )
				amount = 255;
			else if ( amount < 0 )
				amount = 0;

			m_Stream.Write( (byte)amount );
		}*/
	}

	public sealed class CancelArrow : Packet
	{
		public CancelArrow() : base( 0xBA, 6 )
		{
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (short) -1 );
			m_Stream.Write( (short) -1 );
		}
	}

	public sealed class SetArrow : Packet
	{
		public SetArrow( int x, int y ) : base( 0xBA, 6 )
		{
			m_Stream.Write( (byte) 1 );
			m_Stream.Write( (short) x );
			m_Stream.Write( (short) y );
		}
	}

	public sealed class DisplaySecureTrade : Packet
	{
		public DisplaySecureTrade( Mobile them, Container first, Container second, string name ) : base( 0x6F )
		{
			if ( name == null )
				name = "";

			EnsureCapacity( 18 + name.Length );

			m_Stream.Write( (byte) 0 ); // Display
			m_Stream.Write( (int) them.Serial );
			m_Stream.Write( (int) first.Serial );
			m_Stream.Write( (int) second.Serial );
			m_Stream.Write( (bool) true );

			m_Stream.WriteAsciiFixed( name, 30 );
		}
	}

	public sealed class CloseSecureTrade : Packet
	{
		public CloseSecureTrade( Container cont ) : base( 0x6F )
		{
			EnsureCapacity( 8 );

			m_Stream.Write( (byte) 1 ); // Close
			m_Stream.Write( (int) cont.Serial );
		}
	}

	public sealed class UpdateSecureTrade : Packet
	{
		public UpdateSecureTrade( Container cont, bool first, bool second ) : base( 0x6F )
		{
			EnsureCapacity( 8 );

			m_Stream.Write( (byte) 2 ); // Update
			m_Stream.Write( (int) cont.Serial );
			m_Stream.Write( (int) (first ? 1 : 0) );
			m_Stream.Write( (int) (second ? 1 : 0) );
		}
	}

	public sealed class SecureTradeEquip : Packet
	{
		public SecureTradeEquip( Item item, Mobile m ) : base( 0x25, 20 )
		{
			m_Stream.Write( (int) item.Serial );
			m_Stream.Write( (short) item.ItemID );
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (short) item.Amount );
			m_Stream.Write( (short) item.X );
			m_Stream.Write( (short) item.Y );
			m_Stream.Write( (int) m.Serial );
			m_Stream.Write( (short) item.Hue );
		}
	}

	public sealed class MapPatches : Packet
	{
		public MapPatches() : base( 0xBF )
		{
			EnsureCapacity( 9 + (3 * 8) );

			m_Stream.Write( (short) 0x0018 );

			m_Stream.Write( (int) 4 );

			m_Stream.Write( (int) Map.Felucca.Tiles.Patch.StaticBlocks );
			m_Stream.Write( (int) Map.Felucca.Tiles.Patch.LandBlocks );

			m_Stream.Write( (int) Map.Trammel.Tiles.Patch.StaticBlocks );
			m_Stream.Write( (int) Map.Trammel.Tiles.Patch.LandBlocks );

			m_Stream.Write( (int) Map.Ilshenar.Tiles.Patch.StaticBlocks );
			m_Stream.Write( (int) Map.Ilshenar.Tiles.Patch.LandBlocks );

			m_Stream.Write( (int) Map.Malas.Tiles.Patch.StaticBlocks );
			m_Stream.Write( (int) Map.Malas.Tiles.Patch.LandBlocks );
		}
	}

	public sealed class ObjectHelpResponse : Packet
	{
		public ObjectHelpResponse( IEntity e, string text ) : base( 0xB7 )
		{
			this.EnsureCapacity( 9 + (text.Length * 2) );

			m_Stream.Write( (int) e.Serial );
			m_Stream.WriteBigUniNull( text );
		}
	}

	public sealed class VendorBuyContent : Packet
	{
		public VendorBuyContent( ArrayList list ) : base( 0x3c )
		{
			this.EnsureCapacity( list.Count*19 + 5 );

			m_Stream.Write( (short)list.Count );

			//The client sorts these by their X/Y value.
			//OSI sends these in wierd order.  X/Y highest to lowest and serial loest to highest
			//These are already sorted by serial (done by the vendor class) but we have to send them by x/y
			//(the x74 packet is sent in 'correct' order.)
			for ( int i = list.Count - 1; i >= 0; --i )
			{
				BuyItemState bis = (BuyItemState)list[i];
		
				m_Stream.Write( (int)bis.MySerial );
				m_Stream.Write( (ushort)(bis.ItemID & 0x3FFF) );
				m_Stream.Write( (byte)0 );//itemid offset
				m_Stream.Write( (ushort)bis.Amount );
				m_Stream.Write( (short)(i+1) );//x
				m_Stream.Write( (short)1 );//y
				m_Stream.Write( (int)bis.ContainerSerial );
				m_Stream.Write( (ushort)bis.Hue );
			}
		}
	}

	public sealed class DisplayBuyList : Packet
	{
		public DisplayBuyList( Mobile vendor ) : base( 0x24, 7 )
		{
			m_Stream.Write( (int)vendor.Serial );
			m_Stream.Write( (short)0x30 );//buy window id?
		}
	}

	public sealed class VendorBuyList : Packet
	{
		public VendorBuyList( Mobile vendor, ArrayList list ) : base( 0x74 )
		{
			this.EnsureCapacity( 256 );

			Container BuyPack = vendor.FindItemOnLayer( Layer.ShopBuy ) as Container;
			m_Stream.Write( (int)(BuyPack == null ? Serial.MinusOne : BuyPack.Serial) );

			m_Stream.Write( (byte)list.Count );

			for ( int i = 0; i < list.Count; ++i )
			{
				BuyItemState bis = (BuyItemState)list[i];

				m_Stream.Write( (int) bis.Price );

				string desc = bis.Description;

				if ( desc == null )
					desc = "";

				m_Stream.Write( (byte)(desc.Length + 1) );
				m_Stream.WriteAsciiNull( desc );
			}
		}
	}

	public sealed class VendorSellList : Packet
	{
		public VendorSellList( Mobile shopkeeper, Hashtable table ) : base( 0x9E )
		{
			this.EnsureCapacity( 256 );

			m_Stream.Write( (int) shopkeeper.Serial );

			m_Stream.Write( (ushort) table.Count );

			foreach ( SellItemState state in table.Values )
			{
				m_Stream.Write( (int) state.Item.Serial );
				m_Stream.Write( (ushort) (state.Item.ItemID & 0x3FFF) );
				m_Stream.Write( (ushort) state.Item.Hue );
				m_Stream.Write( (ushort) state.Item.Amount );
				m_Stream.Write( (ushort) state.Price );

				string name = state.Item.Name;

				if ( name == null || (name = name.Trim()).Length <= 0 )
					name = state.Name;

				if ( name == null )
					name = "";

				m_Stream.Write( (ushort) (name.Length) );
				m_Stream.WriteAsciiFixed( name, (ushort) (name.Length) );
			}
		}
	}

	public sealed class EndVendorSell : Packet
	{
		public EndVendorSell( Mobile Vendor ) : base( 0x3B, 8 )
		{
			m_Stream.Write( (ushort)8 );//length
			m_Stream.Write( (int)Vendor.Serial );
			m_Stream.Write( (byte)0 );
		}
	}

	public sealed class EndVendorBuy : Packet
	{
		public EndVendorBuy( Mobile Vendor ) : base( 0x3B, 8 )
		{
			m_Stream.Write( (ushort)8 );//length
			m_Stream.Write( (int)Vendor.Serial );
			m_Stream.Write( (byte)0 );
		}
	}

	public sealed class DeathAnimation : Packet
	{
		public DeathAnimation( Mobile killed, Item corpse ) : base( 0xAF, 13 )
		{
			m_Stream.Write( (int) killed.Serial );
			m_Stream.Write( (int) (corpse == null ? Serial.Zero : corpse.Serial) );
			m_Stream.Write( (int) 0 ) ;
		}
	}

	public sealed class StatLockInfo : Packet
	{
		public StatLockInfo( Mobile m ) : base( 0xBF )
		{
			this.EnsureCapacity( 12 );

			m_Stream.Write( (short) 0x19 );
			m_Stream.Write( (byte) 2 );
			m_Stream.Write( (int) m.Serial );
			m_Stream.Write( (byte) 0 );

			int lockBits = 0;

			lockBits |= (int)m.StrLock << 4;
			lockBits |= (int)m.DexLock << 2;
			lockBits |= (int)m.IntLock;

			m_Stream.Write( (byte) lockBits );
		}
	}

	public class EquipInfoAttribute
	{
		private int m_Number;
		private int m_Charges;

		public int Number
		{
			get
			{
				return m_Number;
			}
		}

		public int Charges
		{
			get
			{
				return m_Charges;
			}
		}

		public EquipInfoAttribute( int number ) : this( number, -1 )
		{
		}

		public EquipInfoAttribute( int number, int charges )
		{
			m_Number = number;
			m_Charges = charges;
		}
	}

	public class EquipmentInfo
	{
		private int m_Number;
		private Mobile m_Crafter;
		private bool m_Unidentified;
		private EquipInfoAttribute[] m_Attributes;

		public int Number
		{
			get
			{
				return m_Number;
			}
		}

		public Mobile Crafter
		{
			get
			{
				return m_Crafter;
			}
		}

		public bool Unidentified
		{
			get
			{
				return m_Unidentified;
			}
		}

		public EquipInfoAttribute[] Attributes
		{
			get
			{
				return m_Attributes;
			}
		}

		public EquipmentInfo( int number, Mobile crafter, bool unidentified, EquipInfoAttribute[] attributes )
		{
			m_Number = number;
			m_Crafter = crafter;
			m_Unidentified = unidentified;
			m_Attributes = attributes;
		}
	}

	public sealed class DisplayEquipmentInfo : Packet
	{
		public DisplayEquipmentInfo( Item item, EquipmentInfo info ) : base( 0xBF )
		{
			EquipInfoAttribute[] attrs = info.Attributes;

			this.EnsureCapacity( 17 + (info.Crafter == null ? 0 : 6 + info.Crafter.Name == null ? 0 : info.Crafter.Name.Length) + (info.Unidentified ? 4 : 0) + (attrs.Length * 6) );

			m_Stream.Write( (short) 0x10 );
			m_Stream.Write( (int) item.Serial );

			m_Stream.Write( (int) info.Number );

			if ( info.Crafter != null )
			{
				string name = info.Crafter.Name;

				if ( name == null ) name = "";

				int length = (ushort)name.Length;

				m_Stream.Write( (int) -3 );
				m_Stream.Write( (ushort) length );
				m_Stream.WriteAsciiFixed( name, length );
			}

			if ( info.Unidentified )
			{
				m_Stream.Write( (int) -4 );
			}

			for ( int i = 0; i < attrs.Length; ++i )
			{
				m_Stream.Write( (int) attrs[i].Number );
				m_Stream.Write( (short) attrs[i].Charges );
			}

			m_Stream.Write( (int) -1 );
		}
	}

	public sealed class ChangeUpdateRange : Packet
	{
		private static ChangeUpdateRange[] m_Cache = new ChangeUpdateRange[0x100];

		public static ChangeUpdateRange Instantiate( int range )
		{
			byte idx = (byte)range;
			ChangeUpdateRange p = m_Cache[idx];

			if ( p == null )
				m_Cache[idx] = p = new ChangeUpdateRange( range );

			return p;
		}

		public ChangeUpdateRange( int range ) : base( 0xC8, 2 )
		{
			m_Stream.Write( (byte) range );
		}
	}

	public sealed class ChangeCombatant : Packet
	{
		public ChangeCombatant( Mobile combatant ) : base( 0xAA, 5 )
		{
			m_Stream.Write( combatant != null ? combatant.Serial : Serial.Zero );
		}
	}

	public sealed class DisplayHuePicker : Packet
	{
		public DisplayHuePicker( HuePicker huePicker ) : base( 0x95, 9 )
		{
			m_Stream.Write( (int) huePicker.Serial );
			m_Stream.Write( (short) 0 );
			m_Stream.Write( (short) huePicker.ItemID );
		}
	}

	public sealed class TripTimeResponse : Packet
	{
		public TripTimeResponse( int unk ) : base( 0xC9, 6 )
		{
			m_Stream.Write( (byte) unk );
			m_Stream.Write( (int) Environment.TickCount );
		}
	}

	public sealed class UTripTimeResponse : Packet
	{
		public UTripTimeResponse( int unk ) : base( 0xCA, 6 )
		{
			m_Stream.Write( (byte) unk );
			m_Stream.Write( (int) Environment.TickCount );
		}
	}

	public sealed class UnicodePrompt : Packet
	{
		public UnicodePrompt( Prompt prompt ) : base( 0xC2 )
		{
			this.EnsureCapacity( 21 );

			m_Stream.Write( (int) prompt.Serial );
			m_Stream.Write( (int) prompt.Serial );
			m_Stream.Write( (int) 0 );
			m_Stream.Write( (int) 0 );
			m_Stream.Write( (short) 0 );
		}
	}

	public sealed class ChangeCharacter : Packet
	{
		public ChangeCharacter( IAccount a ) : base( 0x81 )
		{
			this.EnsureCapacity( 305 );

			int count = 0;

			for ( int i = 0; i < a.Length; ++i )
			{
				if ( a[i] != null )
					++count;
			}

			m_Stream.Write( (byte) count );
			m_Stream.Write( (byte) 0 );

			for ( int i = 0; i < a.Length; ++i )
			{
				if ( a[i] != null )
				{
					string name = a[i].Name;

					if ( name == null )
						name = "-null-";
					else if ( (name = name.Trim()).Length == 0 )
						name = "-empty-";

					m_Stream.WriteAsciiFixed( name, 30 );
					m_Stream.Fill( 30 ); // password
				}
				else
				{
					m_Stream.Fill( 60 );
				}
			}
		}
	}

	public sealed class DeathStatus : Packet
	{
		public static readonly DeathStatus Dead = new DeathStatus( true );
		public static readonly DeathStatus Alive = new DeathStatus( false );

		public static DeathStatus Instantiate( bool dead )
		{
			return ( dead ? Dead : Alive );
		}

		public DeathStatus( bool dead ) : base( 0x2C, 2 )
		{
			m_Stream.Write( (byte) (dead ? 0 : 2) );
		}
	}

	public sealed class InvalidMapEnable : Packet
	{
		public InvalidMapEnable() : base( 0xC6, 1 )
		{
		}
	}

	public sealed class BondedStatus : Packet
	{
		public BondedStatus( int val1, Serial serial, int val2 ) : base( 0xBF )
		{
			this.EnsureCapacity( 11 );

			m_Stream.Write( (short) 0x19 );
			m_Stream.Write( (byte) val1 );
			m_Stream.Write( (int) serial );
			m_Stream.Write( (byte) val2 );
		}
	}

	public sealed class DisplayItemListMenu : Packet
	{
		public DisplayItemListMenu( ItemListMenu menu ) : base( 0x7C )
		{
			this.EnsureCapacity( 256 );

			m_Stream.Write( (int) ((IMenu)menu).Serial );
			m_Stream.Write( (short) 0 );

			string question = menu.Question;

			if ( question == null ) question = "";

			int questionLength = (byte)question.Length;

			m_Stream.Write( (byte) questionLength );
			m_Stream.WriteAsciiFixed( question, questionLength );

			ItemListEntry[] entries = menu.Entries;

			int entriesLength = (byte)entries.Length;

			m_Stream.Write( (byte) entriesLength );

			for ( int i = 0; i < entriesLength; ++i )
			{
				ItemListEntry e = entries[i];

				m_Stream.Write( (short) (e.ItemID & 0x3FFF) );
				m_Stream.Write( (short) e.Hue );

				string name = e.Name;

				if ( name == null ) name = "";

				int nameLength = (byte)name.Length;

				m_Stream.Write( (byte) nameLength );
				m_Stream.WriteAsciiFixed( name, nameLength );
			}
		}
	}

	public sealed class DisplayQuestionMenu : Packet
	{
		public DisplayQuestionMenu( QuestionMenu menu ) : base( 0x7C )
		{
			this.EnsureCapacity( 256 );

			m_Stream.Write( (int) ((IMenu)menu).Serial );
			m_Stream.Write( (short) 0 );

			string question = menu.Question;

			if ( question == null ) question = "";

			int questionLength = (byte)question.Length;

			m_Stream.Write( (byte) questionLength );
			m_Stream.WriteAsciiFixed( question, questionLength );

			string[] answers = menu.Answers;

			int answersLength = (byte)answers.Length;

			m_Stream.Write( (byte) answersLength );

			for ( int i = 0; i < answersLength; ++i )
			{
				m_Stream.Write( (int) 0 );

				string answer = answers[i];

				if ( answer == null ) answer = "";

				int answerLength = (byte)answer.Length;

				m_Stream.Write( (byte) answerLength );
				m_Stream.WriteAsciiFixed( answer, answerLength );
			}
		}
	}

	public sealed class GlobalLightLevel : Packet
	{
		private static GlobalLightLevel[] m_Cache = new GlobalLightLevel[0x100];

		public static GlobalLightLevel Instantiate( int level )
		{
			byte lvl = (byte)level;
			GlobalLightLevel p = m_Cache[lvl];

			if ( p == null )
				m_Cache[lvl] = p = new GlobalLightLevel( level );

			return p;
		}

		public GlobalLightLevel( int level ) : base( 0x4F, 2 )
		{
			m_Stream.Write( (sbyte) level );
		}
	}

	public sealed class PersonalLightLevel : Packet
	{
		public PersonalLightLevel( Mobile m ) : this( m, m.LightLevel )
		{
		}

		public PersonalLightLevel( Mobile m, int level ) : base( 0x4E, 6 )
		{
			m_Stream.Write( (int) m.Serial );
			m_Stream.Write( (sbyte) level );
		}
	}

	public sealed class PersonalLightLevelZero : Packet
	{
		public PersonalLightLevelZero( Mobile m ) : base( 0x4E, 6 )
		{
			m_Stream.Write( (int) m.Serial );
			m_Stream.Write( (sbyte) 0 );
		}
	}

	public enum CMEFlags
	{
		None = 0x00,
		Disabled = 0x01,
		Colored = 0x20
	}

	public sealed class DisplayContextMenu : Packet
	{
		public DisplayContextMenu( ContextMenu menu ) : base( 0xBF )
		{
			ContextMenuEntry[] entries = menu.Entries;

			int length = (byte) entries.Length;

			this.EnsureCapacity( 12 + (length * 8) );

			m_Stream.Write( (short) 0x14 );
			m_Stream.Write( (short) 0x01 );

			IEntity target = menu.Target as IEntity;

			m_Stream.Write( (int) ( target == null ? Serial.MinusOne : target.Serial ) );

			m_Stream.Write( (byte) length );

			Point3D p;

			if ( target is Mobile )
				p = target.Location;
			else if ( target is Item )
				p = ((Item)target).GetWorldLocation();
			else
				p = Point3D.Zero;

			for ( int i = 0; i < length; ++i )
			{
				ContextMenuEntry e = entries[i];

				m_Stream.Write( (short) i );
				m_Stream.Write( (ushort) e.Number );

				int range = e.Range;

				if ( range == -1 )
					range = 18;

				CMEFlags flags = (e.Enabled && menu.From.InRange( p, range )) ? CMEFlags.None : CMEFlags.Disabled;

				int color = e.Color & 0xFFFF;

				if ( color != 0xFFFF )
					flags |= CMEFlags.Colored;

				flags |= e.Flags;

				m_Stream.Write( (short) flags );

				if ( (flags & CMEFlags.Colored) != 0 )
					m_Stream.Write( (short) color );
			}
		}
	}

	public sealed class DisplayProfile : Packet
	{
		public DisplayProfile( bool realSerial, Mobile m, string header, string body, string footer ) : base( 0xB8 )
		{
			if ( header == null )
				header = "";

			if ( body == null )
				body = "";

			if ( footer == null )
				footer = "";

			EnsureCapacity( 12 + header.Length + (footer.Length * 2) + (body.Length * 2) );

			m_Stream.Write( (int) (realSerial ? m.Serial : Serial.Zero) );
			m_Stream.WriteAsciiNull( header );
			m_Stream.WriteBigUniNull( footer );
			m_Stream.WriteBigUniNull( body );
		}
	}

	/*public sealed class ProfileResponse : Packet
	{
		public ProfileResponse( Mobile beholder, Mobile beheld ) : base( 0xB8 )
		{
			this.EnsureCapacity( 256 );

			m_Stream.Write( beheld != beholder || !beheld.ProfileLocked ? beheld.Serial : Serial.Zero );

			m_Stream.WriteAsciiNull( Titles.ComputeTitle( beholder, beheld ) );

			string footer;

			if ( beheld.ProfileLocked )
			{
				if ( beheld == beholder )
				{
					footer = "Your profile has been locked.";
				}
				else if ( beholder.AccessLevel >= AccessLevel.Counselor )
				{
					footer = "This profile has been locked.";
				}
				else
				{
					footer = "";
				}
			}
			else
			{
				footer = "";
			}

			m_Stream.WriteBigUniNull( footer );

			string body = beheld.Profile;

			if ( body == null || body.Length <= 0 )
			{
				if ( beheld != beholder )
				{
					body = String.Format( "{0} has written no profile.", beheld.Body.IsHuman ? beheld.Female ? "She" : "He" : beheld.Body.IsGhost ? "That spirit" : "They" );
				}
				else
				{
					body = "";
				}
			}

			m_Stream.WriteBigUniNull( body );
		}
	}*/

	public sealed class CloseGump : Packet
	{
		public CloseGump( int typeID, int buttonID ) : base( 0xBF )
		{
			this.EnsureCapacity( 13 );

			m_Stream.Write( (short) 0x04 );
			m_Stream.Write( (int) typeID );
			m_Stream.Write( (int) buttonID );
		}
	}

	public sealed class EquipUpdate : Packet
	{
		public EquipUpdate( Item item ) : base( 0x2E, 15 )
		{
			Serial parentSerial;

			if ( item.Parent is Mobile )
			{
				parentSerial = ((Mobile)item.Parent).Serial;
			}
			else
			{
				Console.WriteLine( "Warning: EquipUpdate on item with !(parent is Mobile)" );
				parentSerial = Serial.Zero;
			}

			int hue = item.Hue;

			if ( item.Parent is Mobile )
			{
				Mobile mob = (Mobile)item.Parent;

				if ( mob.SolidHueOverride >= 0 )
					hue = mob.SolidHueOverride;
			}

			m_Stream.Write( (int) item.Serial );
			m_Stream.Write( (short) item.ItemID );
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (byte) item.Layer );
			m_Stream.Write( (int) parentSerial );
			m_Stream.Write( (short) hue );
		}
	}

	public sealed class WorldItem : Packet
	{
		public WorldItem( Item item ) : base( 0x1A )
		{
			this.EnsureCapacity( 20 );

			// 14 base length
			// +2 - Amount
			// +2 - Hue
			// +1 - Flags

			uint serial = (uint)item.Serial.Value;
			int itemID = item.ItemID;
			int amount = item.Amount;
			Point3D loc = item.Location;
			int x = loc.m_X;
			int y = loc.m_Y;
			int hue = item.Hue;
			int flags = item.GetPacketFlags();
			int direction = (int)item.Direction;

			if ( amount != 0 )
			{
				serial |= 0x80000000;
			}
			else
			{
				serial &= 0x7FFFFFFF;
			}

			m_Stream.Write( (uint) serial );
			m_Stream.Write( (short) (itemID & 0x7FFF) );

			if ( amount != 0 )
			{
				m_Stream.Write( (short) amount );
			}

			x &= 0x7FFF;

			if ( direction != 0 )
			{
				x |= 0x8000;
			}

			m_Stream.Write( (short) x );

			y &= 0x3FFF;

			if ( hue != 0 )
			{
				y |= 0x8000;
			}

			if ( flags != 0 )
			{
				y |= 0x4000;
			}

			m_Stream.Write( (short) y );

			if ( direction != 0 )
				m_Stream.Write( (byte) direction );

			m_Stream.Write( (sbyte) loc.m_Z );

			if ( hue != 0 )
				m_Stream.Write( (ushort) hue );

			if ( flags != 0 )
				m_Stream.Write( (byte) flags );
		}
	}

	public sealed class LiftRej : Packet
	{
		public LiftRej( LRReason reason ) : base( 0x27, 2 )
		{
			m_Stream.Write( (byte) reason );
		}
	}

	public sealed class LogoutAck : Packet
	{
		public LogoutAck() : base( 0xD1, 2 )
		{
			m_Stream.Write( (byte) 0x01 );
		}
	}

	public sealed class Weather : Packet
	{
		public Weather( int v1, int v2, int v3 ) : base( 0x65, 4 )
		{
			m_Stream.Write( (byte) v1 );
			m_Stream.Write( (byte) v2 );
			m_Stream.Write( (byte) v3 );
		}
	}

	public sealed class UnkD3 : Packet
	{
		public UnkD3( Mobile beholder, Mobile beheld ) : base( 0xD3 )
		{
			this.EnsureCapacity( 256 );

			//int
			//short
			//short
			//short
			//byte
			//byte
			//short
			//byte
			//byte
			//short
			//short
			//short
			//while ( int != 0 )
			//{
			//short
			//byte
			//short
			//}

			m_Stream.Write( (int) beheld.Serial );
			m_Stream.Write( (short) beheld.Body );
			m_Stream.Write( (short) beheld.X );
			m_Stream.Write( (short) beheld.Y );
			m_Stream.Write( (sbyte) beheld.Z );
			m_Stream.Write( (byte) beheld.Direction );
			m_Stream.Write( (ushort) beheld.Hue );
			m_Stream.Write( (byte) beheld.GetPacketFlags() );
			m_Stream.Write( (byte) Notoriety.Compute( beholder, beheld ) );

			m_Stream.Write( (short) 0 );
			m_Stream.Write( (short) 0 );
			m_Stream.Write( (short) 0 );

			m_Stream.Write( (int) 0 );
		}
	}

	public sealed class GQRequest : Packet
	{
		public GQRequest() : base( 0xC3 )
		{
			this.EnsureCapacity( 256 );

			m_Stream.Write( (int) 1 );
			m_Stream.Write( (int) 2 ); // ID
			m_Stream.Write( (int) 3 ); // Customer ? (this)
			m_Stream.Write( (int) 4 ); // Customer this (?)
			m_Stream.Write( (int) 0 );
			m_Stream.Write( (short) 0 );
			m_Stream.Write( (short) 6 );
			m_Stream.Write( (byte) 'r' );
			m_Stream.Write( (byte) 'e' );
			m_Stream.Write( (byte) 'g' );
			m_Stream.Write( (byte) 'i' );
			m_Stream.Write( (byte) 'o' );
			m_Stream.Write( (byte) 'n' );
			m_Stream.Write( (int) 7 ); // Call time in seconds
			m_Stream.Write( (short) 2 ); // Map (0=fel,1=tram,2=ilsh)
			m_Stream.Write( (int) 8 ); // X
			m_Stream.Write( (int) 9 ); // Y
			m_Stream.Write( (int) 10 ); // Z
			m_Stream.Write( (int) 11 ); // Volume
			m_Stream.Write( (int) 12 ); // Rank
			m_Stream.Write( (int) -1 );
			m_Stream.Write( (int) 1 ); // type
		}
	}

	/// <summary>
	/// Causes the client to walk in a given direction. It does not send a movement request.
	/// </summary>
	public sealed class PlayerMove : Packet
	{
		public PlayerMove( Direction d ) : base( 0x97, 2 )
		{
			m_Stream.Write( (byte) d );

			// @4C63B0
		}
	}

	/// <summary>
	/// Displays a message "There are currently [count] available calls in the global queue.".
	/// </summary>
	public sealed class GQCount : Packet
	{
		public GQCount( int unk, int count ) : base( 0xCB, 7 )
		{
			m_Stream.Write( (short) unk );
			m_Stream.Write( (int) count );
		}
	}

	/// <summary>
	/// Asks the client for it's version
	/// </summary>
	public sealed class ClientVersionReq : Packet
	{
		public ClientVersionReq() : base( 0xBD )
		{
			this.EnsureCapacity( 3 );
		}
	}

	/// <summary>
	/// Asks the client for it's "assist version". (Perhaps for UOAssist?)
	/// </summary>
	public sealed class AssistVersionReq : Packet
	{
		public AssistVersionReq( int unk ) : base( 0xBE )
		{
			this.EnsureCapacity( 7 );

			m_Stream.Write( (int) unk );
		}
	}

	public enum EffectType
	{
		Moving    = 0x00,
		Lightning = 0x01,
		FixedXYZ  = 0x02,
		FixedFrom = 0x03
	}

	public class ParticleEffect : Packet
	{
		public ParticleEffect( EffectType type, Serial from, Serial to, int itemID, Point3D fromPoint, Point3D toPoint, int speed, int duration, bool fixedDirection, bool explode, int hue, int renderMode, int effect, int explodeEffect, int explodeSound, Serial serial, int layer, int unknown ) : base( 0xC7, 49 )
		{
			m_Stream.Write( (byte) type );
			m_Stream.Write( (int) from );
			m_Stream.Write( (int) to );
			m_Stream.Write( (short) itemID );
			m_Stream.Write( (short) fromPoint.m_X );
			m_Stream.Write( (short) fromPoint.m_Y );
			m_Stream.Write( (sbyte) fromPoint.m_Z );
			m_Stream.Write( (short) toPoint.m_X );
			m_Stream.Write( (short) toPoint.m_Y );
			m_Stream.Write( (sbyte) toPoint.m_Z );
			m_Stream.Write( (byte) speed );
			m_Stream.Write( (byte) duration );
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (bool) fixedDirection );
			m_Stream.Write( (bool) explode );
			m_Stream.Write( (int) hue );
			m_Stream.Write( (int) renderMode );
			m_Stream.Write( (short) effect );
			m_Stream.Write( (short) explodeEffect );
			m_Stream.Write( (short) explodeSound );
			m_Stream.Write( (int) serial );
			m_Stream.Write( (byte) layer );
			m_Stream.Write( (short) unknown );
		}

		public ParticleEffect( EffectType type, Serial from, Serial to, int itemID, IPoint3D fromPoint, IPoint3D toPoint, int speed, int duration, bool fixedDirection, bool explode, int hue, int renderMode, int effect, int explodeEffect, int explodeSound, Serial serial, int layer, int unknown ) : base( 0xC7, 49 )
		{
			m_Stream.Write( (byte) type );
			m_Stream.Write( (int) from );
			m_Stream.Write( (int) to );
			m_Stream.Write( (short) itemID );
			m_Stream.Write( (short) fromPoint.X );
			m_Stream.Write( (short) fromPoint.Y );
			m_Stream.Write( (sbyte) fromPoint.Z );
			m_Stream.Write( (short) toPoint.X );
			m_Stream.Write( (short) toPoint.Y );
			m_Stream.Write( (sbyte) toPoint.Z );
			m_Stream.Write( (byte) speed );
			m_Stream.Write( (byte) duration );
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (bool) fixedDirection );
			m_Stream.Write( (bool) explode );
			m_Stream.Write( (int) hue );
			m_Stream.Write( (int) renderMode );
			m_Stream.Write( (short) effect );
			m_Stream.Write( (short) explodeEffect );
			m_Stream.Write( (short) explodeSound );
			m_Stream.Write( (int) serial );
			m_Stream.Write( (byte) layer );
			m_Stream.Write( (short) unknown );
		}
	}

	public class HuedEffect : Packet
	{
		public HuedEffect( EffectType type, Serial from, Serial to, int itemID, Point3D fromPoint, Point3D toPoint, int speed, int duration, bool fixedDirection, bool explode, int hue, int renderMode ) : base( 0xC0, 36 )
		{
			m_Stream.Write( (byte) type );
			m_Stream.Write( (int) from );
			m_Stream.Write( (int) to );
			m_Stream.Write( (short) itemID );
			m_Stream.Write( (short) fromPoint.m_X );
			m_Stream.Write( (short) fromPoint.m_Y );
			m_Stream.Write( (sbyte) fromPoint.m_Z );
			m_Stream.Write( (short) toPoint.m_X );
			m_Stream.Write( (short) toPoint.m_Y );
			m_Stream.Write( (sbyte) toPoint.m_Z );
			m_Stream.Write( (byte) speed );
			m_Stream.Write( (byte) duration );
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (bool) fixedDirection );
			m_Stream.Write( (bool) explode );
			m_Stream.Write( (int) hue );
			m_Stream.Write( (int) renderMode );
		}

		public HuedEffect( EffectType type, Serial from, Serial to, int itemID, IPoint3D fromPoint, IPoint3D toPoint, int speed, int duration, bool fixedDirection, bool explode, int hue, int renderMode ) : base( 0xC0, 36 )
		{
			m_Stream.Write( (byte) type );
			m_Stream.Write( (int) from );
			m_Stream.Write( (int) to );
			m_Stream.Write( (short) itemID );
			m_Stream.Write( (short) fromPoint.X );
			m_Stream.Write( (short) fromPoint.Y );
			m_Stream.Write( (sbyte) fromPoint.Z );
			m_Stream.Write( (short) toPoint.X );
			m_Stream.Write( (short) toPoint.Y );
			m_Stream.Write( (sbyte) toPoint.Z );
			m_Stream.Write( (byte) speed );
			m_Stream.Write( (byte) duration );
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (bool) fixedDirection );
			m_Stream.Write( (bool) explode );
			m_Stream.Write( (int) hue );
			m_Stream.Write( (int) renderMode );
		}
	}

	public sealed class TargetParticleEffect : ParticleEffect
	{
		public TargetParticleEffect( IEntity e, int itemID, int speed, int duration, int hue, int renderMode, int effect, int layer, int unknown ) : base( EffectType.FixedFrom, e.Serial, Serial.Zero, itemID, e.Location, e.Location, speed, duration, true, false, hue, renderMode, effect, 1, 0, e.Serial, layer, unknown )
		{
		}
	}

	public sealed class TargetEffect : HuedEffect
	{
		public TargetEffect( IEntity e, int itemID, int speed, int duration, int hue, int renderMode ) : base( EffectType.FixedFrom, e.Serial, Serial.Zero, itemID, e.Location, e.Location, speed, duration, true, false, hue, renderMode )
		{
		}
	}

	public sealed class LocationParticleEffect : ParticleEffect
	{
		public LocationParticleEffect( IEntity e, int itemID, int speed, int duration, int hue, int renderMode, int effect, int unknown ) : base( EffectType.FixedXYZ, e.Serial, Serial.Zero, itemID, e.Location, e.Location, speed, duration, true, false, hue, renderMode, effect, 1, 0, e.Serial, 255, unknown )
		{
		}
	}

	public sealed class LocationEffect : HuedEffect
	{
		public LocationEffect( IPoint3D p, int itemID, int speed, int duration, int hue, int renderMode ) : base( EffectType.FixedXYZ, Serial.Zero, Serial.Zero, itemID, p, p, speed, duration, true, false, hue, renderMode )
		{
		}
	}

	public sealed class MovingParticleEffect : ParticleEffect
	{
		public MovingParticleEffect( IEntity from, IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes, int hue, int renderMode, int effect, int explodeEffect, int explodeSound, EffectLayer layer, int unknown ) : base( EffectType.Moving, from.Serial, to.Serial, itemID, from.Location, to.Location, speed, duration, fixedDirection, explodes, hue, renderMode, effect, explodeEffect, explodeSound, Serial.Zero, (int)layer, unknown )
		{
		}
	}

	public sealed class MovingEffect : HuedEffect
	{
		public MovingEffect( IEntity from, IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes, int hue, int renderMode ) : base( EffectType.Moving, from.Serial, to.Serial, itemID, from.Location, to.Location, speed, duration, fixedDirection, explodes, hue, renderMode )
		{
		}
	}

	public enum DeleteResultType
	{
		PasswordInvalid,
		CharNotExist,
		CharBeingPlayed,
		CharTooYoung,
		CharQueued,
		BadRequest
	}

	public sealed class DeleteResult : Packet
	{
		public DeleteResult( DeleteResultType res ) : base( 0x85, 2 )
		{
			m_Stream.Write( (byte) res );
		}
	}

	/*public sealed class MovingEffect : Packet
	{
		public MovingEffect( IEntity from, IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool turn, int hue, int renderMode ) : base( 0xC0, 36 )
		{
			m_Stream.Write( (byte) 0x00 );
			m_Stream.Write( (int) from.Serial );
			m_Stream.Write( (int) to.Serial );
			m_Stream.Write( (short) itemID );
			m_Stream.Write( (short) from.Location.m_X );
			m_Stream.Write( (short) from.Location.m_Y );
			m_Stream.Write( (sbyte) from.Location.m_Z );
			m_Stream.Write( (short) to.Location.m_X );
			m_Stream.Write( (short) to.Location.m_Y );
			m_Stream.Write( (sbyte) to.Location.m_Z );
			m_Stream.Write( (byte) speed );
			m_Stream.Write( (byte) duration );
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (bool) fixedDirection );
			m_Stream.Write( (bool) turn );
			m_Stream.Write( (int) hue );
			m_Stream.Write( (int) renderMode );
		}
	}*/

	/*public sealed class LocationEffect : Packet
	{
		public LocationEffect( IPoint3D p, int itemID, int duration, int hue, int renderMode ) : base( 0xC0, 36 )
		{
			m_Stream.Write( (byte) 0x02 );
			m_Stream.Write( (int) Serial.Zero );
			m_Stream.Write( (int) Serial.Zero );
			m_Stream.Write( (short) itemID );
			m_Stream.Write( (short) p.X );
			m_Stream.Write( (short) p.Y );
			m_Stream.Write( (sbyte) p.Z );
			m_Stream.Write( (short) p.X );
			m_Stream.Write( (short) p.Y );
			m_Stream.Write( (sbyte) p.Z );
			m_Stream.Write( (byte) 10 );
			m_Stream.Write( (byte) duration );
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (byte) 1 );
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (int) hue );
			m_Stream.Write( (int) renderMode );
		}
	}*/

	public sealed class BoltEffect : Packet
	{
		public BoltEffect( IEntity target, int hue ) : base( 0xC0, 36 )
		{
			m_Stream.Write( (byte) 0x01 ); // type
			m_Stream.Write( (int) target.Serial );
			m_Stream.Write( (int) Serial.Zero );
			m_Stream.Write( (short) 0 ); // itemID
			m_Stream.Write( (short) target.X );
			m_Stream.Write( (short) target.Y );
			m_Stream.Write( (sbyte) target.Z );
			m_Stream.Write( (short) target.X );
			m_Stream.Write( (short) target.Y );
			m_Stream.Write( (sbyte) target.Z );
			m_Stream.Write( (byte) 0 ); // speed
			m_Stream.Write( (byte) 0 ); // duration
			m_Stream.Write( (short) 0 ); // unk
			m_Stream.Write( false ); // fixed direction
			m_Stream.Write( false ); // explode
			m_Stream.Write( (int) hue );
			m_Stream.Write( (int) 0 ); // render mode
		}
	}

	public sealed class DisplaySpellbook : Packet
	{
		public DisplaySpellbook( Item book ) : base( 0x24, 7 )
		{
			m_Stream.Write( (int) book.Serial );
			m_Stream.Write( (short) -1 );
		}
	}

	public sealed class NewSpellbookContent : Packet
	{
		public NewSpellbookContent( Item item, int graphic, int offset, ulong content ) : base( 0xBF )
		{
			EnsureCapacity( 23 );

			m_Stream.Write( (short) 0x1B );
			m_Stream.Write( (short) 0x01 );

			m_Stream.Write( (int) item.Serial );
			m_Stream.Write( (short) graphic );
			m_Stream.Write( (short) offset );

			for ( int i = 0; i < 8; ++i )
				m_Stream.Write( (byte)(content >> (i * 8)) );
		}
	}

	public sealed class SpellbookContent : Packet
	{
		public SpellbookContent( int count, int offset, ulong content, Item item ) : base( 0x3C )
		{
			this.EnsureCapacity( 5 + (count * 19) );

			int written = 0;

			m_Stream.Write( (ushort) 0 );

			ulong mask = 1;

			for ( int i = 0; i < 64; ++i, mask <<= 1 )
			{
				if ( (content & mask) != 0 )
				{
					m_Stream.Write( (int) (0x7FFFFFFF - i) );
					m_Stream.Write( (ushort) 0 );
					m_Stream.Write( (byte) 0 );
					m_Stream.Write( (ushort) (i + offset) );
					m_Stream.Write( (short) 0 );
					m_Stream.Write( (short) 0 );
					m_Stream.Write( (int) item.Serial );
					m_Stream.Write( (short) 0 );

					++written;
				}
			}

			m_Stream.Seek( 3, SeekOrigin.Begin );
			m_Stream.Write( (ushort) written );
		}
	}

	public sealed class ContainerDisplay : Packet
	{
		public ContainerDisplay( Container c ) : base( 0x24, 7 )
		{
			m_Stream.Write( (int) c.Serial );
			m_Stream.Write( (short) c.GumpID );
		}
	}

	public sealed class ContainerContentUpdate : Packet
	{
		public ContainerContentUpdate( Item item ) : base( 0x25, 20 )
		{
			Serial parentSerial;

			if ( item.Parent is Item )
			{
				parentSerial = ((Item)item.Parent).Serial;
			}
			else
			{
				Console.WriteLine( "Warning: ContainerContentUpdate on item with !(parent is Item)" );
				parentSerial = Serial.Zero;
			}

			ushort cid = (ushort) item.ItemID;

			if ( cid > 0x3FFF )
				cid = 0x9D7;

			m_Stream.Write( (int) item.Serial );
			m_Stream.Write( (short) cid );
			m_Stream.Write( (byte) 0 ); // signed, itemID offset
			m_Stream.Write( (ushort) item.Amount );
			m_Stream.Write( (short) item.X );
			m_Stream.Write( (short) item.Y );
			m_Stream.Write( (int) parentSerial );
			m_Stream.Write( (ushort) item.Hue );
		}
	}

	public sealed class ContainerContent : Packet
	{
		public ContainerContent( Mobile beholder, Item beheld ) : base( 0x3C )
		{
			ArrayList items = beheld.Items;
			int count = items.Count;

			this.EnsureCapacity( 5 + (count * 19) );

			long pos = m_Stream.Position;

			int written = 0;

			m_Stream.Write( (ushort) 0 );

			for ( int i = 0; i < count; ++i )
			{
				Item child = (Item)items[i];

				if ( !child.Deleted && beholder.CanSee( child ) )
				{
					Point3D loc = child.Location;

					ushort cid = (ushort) child.ItemID;

					if ( cid > 0x3FFF )
						cid = 0x9D7;

					m_Stream.Write( (int) child.Serial );
					m_Stream.Write( (ushort) cid );
					m_Stream.Write( (byte) 0 ); // signed, itemID offset
					m_Stream.Write( (ushort) child.Amount );
					m_Stream.Write( (short) loc.m_X );
					m_Stream.Write( (short) loc.m_Y );
					m_Stream.Write( (int) beheld.Serial );
					m_Stream.Write( (ushort) child.Hue );

					++written;
				}
			}

			m_Stream.Seek( pos, SeekOrigin.Begin );
			m_Stream.Write( (ushort) written );
		}
	}

	public sealed class SetWarMode : Packet
	{
		public static readonly SetWarMode InWarMode = new SetWarMode( true );
		public static readonly SetWarMode InPeaceMode = new SetWarMode( false );

		public static SetWarMode Instantiate( bool mode )
		{
			return ( mode ? InWarMode : InPeaceMode );
		}

		public SetWarMode( bool mode ) : base( 0x72, 5 )
		{
			m_Stream.Write( mode );
			m_Stream.Write( (byte) 0x00 );
			m_Stream.Write( (byte) 0x32 );
			m_Stream.Write( (byte) 0x00 );
			//m_Stream.Fill();
		}
	}

	public sealed class Swing : Packet
	{
		public Swing( int flag, Mobile attacker, Mobile defender ) : base( 0x2F, 10 )
		{
			m_Stream.Write( (byte) flag );
			m_Stream.Write( (int) attacker.Serial );
			m_Stream.Write( (int) defender.Serial );
		}
	}

	public sealed class NullFastwalkStack : Packet
	{
		public NullFastwalkStack() : base( 0xBF )
		{
			EnsureCapacity(256);
			m_Stream.Write( (short) 0x1 );
			m_Stream.Write( (int) 0x0 );
			m_Stream.Write( (int) 0x0 );
			m_Stream.Write( (int) 0x0 );
			m_Stream.Write( (int) 0x0 );
			m_Stream.Write( (int) 0x0 );
			m_Stream.Write( (int) 0x0 );
		}
	}

	public sealed class RemoveItem : Packet
	{
		public RemoveItem( Item item ) : base( 0x1D, 5 )
		{
			m_Stream.Write( (int) item.Serial );
		}
	}

	public sealed class RemoveMobile : Packet
	{
		public RemoveMobile( Mobile m ) : base( 0x1D, 5 )
		{
			m_Stream.Write( (int) m.Serial );
		}
	}

	public sealed class ServerChange : Packet
	{
		public ServerChange( Mobile m, Map map ) : base( 0x76, 16 )
		{
			m_Stream.Write( (short) m.X );
			m_Stream.Write( (short) m.Y );
			m_Stream.Write( (short) m.Z );
			m_Stream.Write( (byte)  0 );
			m_Stream.Write( (short) 0 );
			m_Stream.Write( (short) 0 );
			m_Stream.Write( (short) map.Width );
			m_Stream.Write( (short) map.Height );
		}
	}

	public sealed class SkillUpdate : Packet
	{
		public SkillUpdate( Skills skills ) : base( 0x3A )
		{
			this.EnsureCapacity( 6 + (skills.Length * 9) );

			m_Stream.Write( (byte) 0x02 ); // type: absolute, capped

			for ( int i = 0; i < skills.Length; ++i )
			{
				Skill s = skills[i];

				double v = s.Value;
				int uv = (int)(v * 10);

				if ( uv < 0 )
					uv = 0;
				else if ( uv >= 0x10000 )
					uv = 0xFFFF;

				m_Stream.Write( (ushort) (s.Info.SkillID + 1) );
				m_Stream.Write( (ushort) uv );
				m_Stream.Write( (ushort) s.BaseFixedPoint );
				m_Stream.Write( (byte) s.Lock );
				m_Stream.Write( (ushort) s.CapFixedPoint );
			}

			m_Stream.Write( (short) 0 ); // terminate
		}
	}

	public sealed class Sequence : Packet
	{
		public Sequence( int num ) : base( 0x7B, 2 )
		{
			m_Stream.Write( (byte)num );
		}
	}

	public sealed class SkillChange : Packet
	{
		public SkillChange( Skill skill ) : base( 0x3A )
		{
			this.EnsureCapacity( 13 );

			double v = skill.Value;
			int uv = (int)(v * 10);

			if ( uv < 0 )
				uv = 0;
			else if ( uv >= 0x10000 )
				uv = 0xFFFF;

			m_Stream.Write( (byte) 0xDF ); // type: delta, capped
			m_Stream.Write( (ushort) skill.Info.SkillID );
			m_Stream.Write( (ushort) uv );
			m_Stream.Write( (ushort) skill.BaseFixedPoint );
			m_Stream.Write( (byte) skill.Lock );
			m_Stream.Write( (ushort) skill.CapFixedPoint );

			/*m_Stream.Write( (short) skill.Info.SkillID );
			m_Stream.Write( (short) (skill.Value * 10.0) );
			m_Stream.Write( (short) (skill.Base * 10.0) );
			m_Stream.Write( (byte) skill.Lock );
			m_Stream.Write( (short) skill.CapFixedPoint );*/
		}
	}

	public sealed class LaunchBrowser : Packet
	{
		public LaunchBrowser( string url ) : base( 0xA5 )
		{
			if ( url == null ) url = "";

			this.EnsureCapacity( 4 + url.Length );

			m_Stream.WriteAsciiNull( url );
		}
	}

	public sealed class MessageLocalized : Packet
	{
		private static MessageLocalized[] m_Cache_IntLoc = new MessageLocalized[15000];
		private static MessageLocalized[] m_Cache_CliLoc = new MessageLocalized[100000];
		private static MessageLocalized[] m_Cache_CliLocCmp = new MessageLocalized[5000];

		public static MessageLocalized InstantiateGeneric( int number )
		{
			MessageLocalized[] cache = null;
			int index = 0;

			if ( number >= 3000000 )
			{
				cache = m_Cache_IntLoc;
				index = number - 3000000;
			}
			else if ( number >= 1000000 )
			{
				cache = m_Cache_CliLoc;
				index = number - 1000000;
			}
			else if ( number >= 500000 )
			{
				cache = m_Cache_CliLocCmp;
				index = number - 500000;
			}

			MessageLocalized p;

			if ( cache != null && index >= 0 && index < cache.Length )
			{
				p = cache[index];

				if ( p == null )
					cache[index] = p = new MessageLocalized( Serial.MinusOne, -1, MessageType.Regular, 0x3B2, 3, number, "System", "" );
			}
			else
			{
				p = new MessageLocalized( Serial.MinusOne, -1, MessageType.Regular, 0x3B2, 3, number, "System", "" );
			}

			return p;
		}

		public MessageLocalized( Serial serial, int graphic, MessageType type, int hue, int font, int number, string name, string args ) : base( 0xC1 )
		{
			if (Core.Config.Features["delocalize"]) {
				string format = StringList.Localization[number];
				if (format != null) {
					string text = StringList.CombineArguments( format, args );

					/* this hack replaces the packet data generated by
					   this class with an AsciiMessage packet */
					Packet p = new AsciiMessage(serial, graphic, type, hue, font, name, text);
					EnsureCapacity(3);
					m_Stream = p.UnderlyingStream;
					return;
				}
			}

			if ( name == null ) name = "";
			if ( args == null ) args = "";

			if ( hue == 0 )
				hue = 0x3B2;

			this.EnsureCapacity( 50 + (args.Length * 2) );

			m_Stream.Write( (int) serial );
			m_Stream.Write( (short) graphic );
			m_Stream.Write( (byte) type );
			m_Stream.Write( (short) hue );
			m_Stream.Write( (short) font );
			m_Stream.Write( (int) number );
			m_Stream.WriteAsciiFixed( name, 30 );
			m_Stream.WriteLittleUniNull( args );
		}
	}

	public sealed class MobileMoving : Packet
	{
		public MobileMoving( Mobile m, int noto/*Mobile beholder, Mobile beheld*/ ) : base( 0x77, 17 )
		{
			Point3D loc = m.Location;

			int hue = m.Hue;

			if ( m.SolidHueOverride >= 0 )
				hue = m.SolidHueOverride;

			m_Stream.Write( (int) m.Serial );
			m_Stream.Write( (short) m.Body );
			m_Stream.Write( (short) loc.m_X );
			m_Stream.Write( (short) loc.m_Y );
			m_Stream.Write( (sbyte) loc.m_Z );
			m_Stream.Write( (byte) m.Direction );
			m_Stream.Write( (short) hue );
			m_Stream.Write( (byte) m.GetPacketFlags() );
			m_Stream.Write( (byte) noto );//Notoriety.Compute( beholder, beheld ) );
		}
	}

	public sealed class MultiTargetReq : Packet
	{
		public MultiTargetReq( MultiTarget t ) : base( 0x99, 26 )
		{
			m_Stream.Write( (bool) t.AllowGround );
			m_Stream.Write( (int) t.TargetID );
			m_Stream.Write( (byte) t.Flags );

			m_Stream.Fill();

			m_Stream.Seek( 18, SeekOrigin.Begin );
			m_Stream.Write( (short) t.MultiID );
			m_Stream.Write( (short) t.Offset.X );
			m_Stream.Write( (short) t.Offset.Y );
			m_Stream.Write( (short) t.Offset.Z );
		}
	}

	public sealed class CancelTarget : Packet
	{
		private static Packet m_Instance;

		public static Packet Instance
		{
			get
			{
				if ( m_Instance == null )
					m_Instance = new CancelTarget();

				return m_Instance;
			}
		}

		public CancelTarget() : base( 0x6C, 19 )
		{
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (int)  0 );
			m_Stream.Write( (byte) 3 );
			m_Stream.Fill();
		}
	}

	public sealed class TargetReq : Packet
	{
		public TargetReq( Target t ) : base( 0x6C, 19 )
		{
			m_Stream.Write( (bool) t.AllowGround );
			m_Stream.Write( (int)  t.TargetID );
			m_Stream.Write( (byte) t.Flags );
			m_Stream.Fill();
		}
	}

	public sealed class DragEffect : Packet
	{
		public DragEffect( IEntity src, IEntity trg, int itemID, int hue, int amount ) : base( 0x23, 26 )
		{
			m_Stream.Write( (short) itemID );
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (short) hue );
			m_Stream.Write( (short) amount );
			m_Stream.Write( (int) src.Serial );
			m_Stream.Write( (short) src.X );
			m_Stream.Write( (short) src.Y );
			m_Stream.Write( (sbyte) src.Z );
			m_Stream.Write( (int) trg.Serial );
			m_Stream.Write( (short) trg.X );
			m_Stream.Write( (short) trg.Y );
			m_Stream.Write( (sbyte) trg.Z );
		}
	}

	public sealed class DisplayGumpFast : Packet
	{
		private int m_TextEntries, m_Switches;

		private int m_LayoutLength;

		public int TextEntries{ get{ return m_TextEntries; } set{ m_TextEntries = value; } }
		public int Switches{ get{ return m_Switches; } set{ m_Switches = value; } }

		public DisplayGumpFast( Gump g ) : base( 0xB0 )
		{
			EnsureCapacity( 1024 );

			m_Stream.Write( (int) g.Serial );
			m_Stream.Write( (int) g.TypeID );
			m_Stream.Write( (int) g.X );
			m_Stream.Write( (int) g.Y );
			m_Stream.Write( (ushort) 0xFFFF );
		}

		private static byte[] m_True = Gump.StringToBuffer( " 1" );
		private static byte[] m_False = Gump.StringToBuffer( " 0" );

		private static byte[] m_Buffer = new byte[48];

		static DisplayGumpFast()
		{
			m_Buffer[0] = (byte)' ';
		}

		public void AppendLayout( bool val )
		{
			AppendLayout( val ? m_True : m_False );
		}

		public void AppendLayout( int val )
		{
			string toString = val.ToString();
			int bytes = System.Text.Encoding.ASCII.GetBytes( toString, 0, toString.Length, m_Buffer, 1 ) + 1;

			m_Stream.Write( m_Buffer, 0, bytes );
			m_LayoutLength += bytes;
		}

		public void AppendLayoutNS( int val )
		{
			string toString = val.ToString();
			int bytes = System.Text.Encoding.ASCII.GetBytes( toString, 0, toString.Length, m_Buffer, 1 );

			m_Stream.Write( m_Buffer, 1, bytes );
			m_LayoutLength += bytes;
		}

		public void AppendLayout( byte[] buffer )
		{
			int length = buffer.Length;
			m_Stream.Write( buffer, 0, length );
			m_LayoutLength += length;
		}

		public void WriteText( ArrayList text )
		{
			m_Stream.Seek( 19, SeekOrigin.Begin );
			m_Stream.Write( (ushort) m_LayoutLength );
			m_Stream.Seek( 0, SeekOrigin.End );

			m_Stream.Write( (ushort) text.Count );

			for ( int i = 0; i < text.Count; ++i )
			{
				string v = (string)text[i];

				if ( v == null )
					v = "";

				int length = (ushort) v.Length;

				m_Stream.Write( (ushort) length );
				m_Stream.WriteBigUniFixed( v, length );
			}
		}
	}

	public sealed class DisplayGump : Packet
	{
		public DisplayGump( Gump g, string layout, string[] text ) : base( 0xB0 )
		{
			if ( layout == null ) layout = "";

			this.EnsureCapacity( 256 );

			m_Stream.Write( (int) g.Serial );
			m_Stream.Write( (int) g.TypeID );
			m_Stream.Write( (int) g.X );
			m_Stream.Write( (int) g.Y );
			m_Stream.Write( (ushort) (layout.Length + 1) );
			m_Stream.WriteAsciiNull( layout );

			m_Stream.Write( (ushort) text.Length );

			for ( int i = 0; i < text.Length; ++i )
			{
				string v = text[i];

				if ( v == null ) v = "";

				int length = (ushort) v.Length;

				m_Stream.Write( (ushort) length );
				m_Stream.WriteBigUniFixed( v, length );
			}
		}
	}

	public sealed class DisplayPaperdoll : Packet
	{
		public DisplayPaperdoll( Mobile m, string text, bool canLift ) : base( 0x88, 66 )
		{
			byte flags = 0x00;

			if ( m.Warmode )
				flags |= 0x01;

			if ( canLift )
				flags |= 0x02;

			m_Stream.Write( (int) m.Serial );
			m_Stream.WriteAsciiFixed( text, 60 );
			m_Stream.Write( (byte) flags );
		}
	}

	public sealed class PopupMessage : Packet
	{
		public PopupMessage( PMMessage msg ) : base( 0x53, 2 )
		{
			m_Stream.Write( (byte)msg );
		}
	}

	public sealed class PlaySound : Packet
	{
		public PlaySound( int soundID, IPoint3D target ) : base( 0x54, 12 )
		{
			m_Stream.Write( (byte) 1 ); // flags
			m_Stream.Write( (short) soundID );
			m_Stream.Write( (short) 0 ); // volume
			m_Stream.Write( (short) target.X );
			m_Stream.Write( (short) target.Y );
			m_Stream.Write( (short) target.Z );
		}
	}

	public sealed class PlayMusic : Packet
	{
		public static readonly Packet InvalidInstance = new PlayMusic( MusicName.Invalid );

		private static Packet[] m_Instances = new Packet[60];

		public static Packet GetInstance( MusicName name )
		{
			if ( name == MusicName.Invalid )
				return InvalidInstance;

			int v = (int)name;
			Packet p;

			if ( v >= 0 && v < m_Instances.Length )
			{
				p = m_Instances[v];

				if ( p == null )
					m_Instances[v] = p = new PlayMusic( name );
			}
			else
			{
				p = new PlayMusic( name );
			}

			return p;
		}

		public PlayMusic( MusicName name ) : base( 0x6D, 3 )
		{
			m_Stream.Write( (short)name );
		}
	}

	public sealed class ScrollMessage : Packet
	{
		public ScrollMessage( int type, int tip, string text ) : base( 0xA6 )
		{
			if ( text == null ) text = "";

			this.EnsureCapacity( 10 + text.Length );

			m_Stream.Write( (byte) type );
			m_Stream.Write( (int) tip );
			m_Stream.Write( (ushort) text.Length );
			m_Stream.WriteAsciiFixed( text, text.Length );
		}
	}

	public sealed class CurrentTime : Packet
	{
		public CurrentTime() : base( 0x5B, 4 )
		{
			DateTime now = Core.Now;

			m_Stream.Write( (byte) now.Hour );
			m_Stream.Write( (byte) now.Minute );
			m_Stream.Write( (byte) now.Second );
		}
	}

	public sealed class MapChange : Packet
	{
		public MapChange( Mobile m ) : base( 0xBF )
		{
			this.EnsureCapacity( 6 );

			m_Stream.Write( (short) 0x08 );
			m_Stream.Write( (byte) (m.Map == null ? 0 : m.Map.MapID) );
		}
	}

	public sealed class SeasonChange : Packet
	{
		private static SeasonChange[][] m_Cache = new SeasonChange[5][]
			{
				new SeasonChange[2],
				new SeasonChange[2],
				new SeasonChange[2],
				new SeasonChange[2],
				new SeasonChange[2]
			};

		public static SeasonChange Instantiate( int season )
		{
			return Instantiate( season, true );
		}

		public static SeasonChange Instantiate( int season, bool playSound )
		{
			if ( season >= 0 && season < m_Cache.Length )
			{
				int idx = playSound ? 1 : 0;

				SeasonChange p = m_Cache[season][idx];

				if ( p == null )
					m_Cache[season][idx] = p = new SeasonChange( season, playSound );

				return p;
			}
			else
			{
				return new SeasonChange( season, playSound );
			}
		}

		public SeasonChange( int season ) : this( season, true )
		{
		}

		public SeasonChange( int season, bool playSound ) : base( 0xBC, 3 )
		{
			m_Stream.Write( (byte) season );
			m_Stream.Write( (bool) playSound );
		}
	}

	public sealed class SupportedFeatures : Packet
	{
		private static int m_AdditionalFlags;

		public static int Value{ get{ return m_AdditionalFlags; } set{ m_AdditionalFlags = value; } }

		[Obsolete( "Specify account instead" )]
		public static SupportedFeatures Instantiate()
		{
			return Instantiate( null );
		}

		public static SupportedFeatures Instantiate( IAccount account )
		{
			return new SupportedFeatures( account );
		}

		public SupportedFeatures( IAccount acct ) : base( 0xB9, 3 )
		{
			int flags = 0x0001 | m_AdditionalFlags;

			if (!Core.Config.Features["oldschool"])
				flags |= 0x0002;

			if ( Core.SE )
				flags |= 0x0040;

			if ( Core.AOS )
				flags |= 0x801C;

			if ( acct != null && acct.Limit >= 6 )
			{
				flags |= 0x8020;
				flags &= ~0x004;
			}

			m_Stream.Write( (ushort) flags );
			//m_Stream.Write( (ushort) m_Value ); // 0x01 = T2A, 0x02 = LBR
		}
	}

	public class AttributeNormalizer
	{
		private static int m_Maximum = 25;
		private static bool m_Enabled = true;

		public static int Maximum
		{
			get{ return m_Maximum; }
			set{ m_Maximum = value; }
		}

		public static bool Enabled
		{
			get{ return m_Enabled; }
			set{ m_Enabled = value; }
		}

		public static void Write( PacketWriter stream, int cur, int max )
		{
			if ( m_Enabled && max != 0 )
			{
				stream.Write( (short) m_Maximum );
				stream.Write( (short) ((cur * m_Maximum) / max) );
			}
			else
			{
				stream.Write( (short) max );
				stream.Write( (short) cur );
			}
		}

		public static void WriteReverse( PacketWriter stream, int cur, int max )
		{
			if ( m_Enabled && max != 0 )
			{
				stream.Write( (short) ((cur * m_Maximum) / max) );
				stream.Write( (short) m_Maximum );
			}
			else
			{
				stream.Write( (short) cur );
				stream.Write( (short) max );
			}
		}
	}

	public sealed class MobileHits : Packet
	{
		public MobileHits( Mobile m ) : base( 0xA1, 9 )
		{
			m_Stream.Write( (int) m.Serial );
			m_Stream.Write( (short) m.HitsMax );
			m_Stream.Write( (short) m.Hits );
		}
	}

	public sealed class MobileHitsN : Packet
	{
		public MobileHitsN( Mobile m ) : base( 0xA1, 9 )
		{
			m_Stream.Write( (int) m.Serial );
			AttributeNormalizer.Write( m_Stream, m.Hits, m.HitsMax );
		}
	}

	public sealed class MobileMana : Packet
	{
		public MobileMana( Mobile m ) : base( 0xA2, 9 )
		{
			m_Stream.Write( (int) m.Serial );
			m_Stream.Write( (short) m.ManaMax );
			m_Stream.Write( (short) m.Mana );
		}
	}

	public sealed class MobileManaN : Packet
	{
		public MobileManaN( Mobile m ) : base( 0xA2, 9 )
		{
			m_Stream.Write( (int) m.Serial );
			AttributeNormalizer.Write( m_Stream, m.Mana, m.ManaMax );
		}
	}

	public sealed class MobileStam : Packet
	{
		public MobileStam( Mobile m ) : base( 0xA3, 9 )
		{
			m_Stream.Write( (int) m.Serial );
			m_Stream.Write( (short) m.StamMax );
			m_Stream.Write( (short) m.Stam );
		}
	}

	public sealed class MobileStamN : Packet
	{
		public MobileStamN( Mobile m ) : base( 0xA3, 9 )
		{
			m_Stream.Write( (int) m.Serial );
			AttributeNormalizer.Write( m_Stream, m.Stam, m.StamMax );
		}
	}

	public sealed class MobileAttributes : Packet
	{
		public MobileAttributes( Mobile m ) : base( 0x2D, 17 )
		{
			m_Stream.Write( m.Serial );

			m_Stream.Write( (short) m.HitsMax );
			m_Stream.Write( (short) m.Hits );

			m_Stream.Write( (short) m.ManaMax );
			m_Stream.Write( (short) m.Mana );

			m_Stream.Write( (short) m.StamMax );
			m_Stream.Write( (short) m.Stam );
		}
	}

	public sealed class MobileAttributesN : Packet
	{
		public MobileAttributesN( Mobile m ) : base( 0x2D, 17 )
		{
			m_Stream.Write( m.Serial );

			AttributeNormalizer.Write( m_Stream, m.Hits, m.HitsMax );
			AttributeNormalizer.Write( m_Stream, m.Mana, m.ManaMax );
			AttributeNormalizer.Write( m_Stream, m.Stam, m.StamMax );
		}
	}

	public sealed class PathfindMessage : Packet
	{
		public PathfindMessage( IPoint3D p ) : base( 0x38, 7 )
		{
			m_Stream.Write( (short) p.X );
			m_Stream.Write( (short) p.Y );
			m_Stream.Write( (short) p.Z );
		}
	}

	// unsure of proper format, client crashes
	public sealed class MobileName : Packet
	{
		public MobileName( Mobile m ) : base( 0x98 )
		{
			string name = m.Name;

			if ( name == null ) name = "";

			this.EnsureCapacity( 37 );

			m_Stream.Write( (int) m.Serial );
			m_Stream.WriteAsciiFixed( name, 30 );
		}
	}

	public sealed class MobileAnimation : Packet
	{
		public MobileAnimation( Mobile m, int action, int frameCount, int repeatCount, bool forward, bool repeat, int delay ) : base( 0x6E, 14 )
		{
			m_Stream.Write( (int) m.Serial );
			m_Stream.Write( (short) action );
			m_Stream.Write( (short) frameCount );
			m_Stream.Write( (short) repeatCount );
			m_Stream.Write( (bool) !forward ); // protocol has really "reverse" but I find this more intuitive
			m_Stream.Write( (bool) repeat );
			m_Stream.Write( (byte) delay );
		}
	}

	public sealed class MobileStatusCompact : Packet
	{
		public MobileStatusCompact( bool canBeRenamed, Mobile m ) : base( 0x11 )
		{
			string name = m.Name;
			if ( name == null ) name = "";

			this.EnsureCapacity( 43 );

			m_Stream.Write( (int) m.Serial );
			m_Stream.WriteAsciiFixed( name, 30 );

			AttributeNormalizer.WriteReverse( m_Stream, m.Hits, m.HitsMax );

			m_Stream.Write( canBeRenamed );

			m_Stream.Write( (byte) 0 ); // type
		}
	}

	public sealed class MobileStatusExtended : Packet
	{
		public MobileStatusExtended( Mobile m ) : base( 0x11 )
		{
			string name = m.Name;
			if ( name == null ) name = "";

			this.EnsureCapacity( 88 );

			m_Stream.Write( (int) m.Serial );
			m_Stream.WriteAsciiFixed( name, 30 );

			m_Stream.Write( (short) m.Hits );
			m_Stream.Write( (short) m.HitsMax );

			m_Stream.Write( m.CanBeRenamedBy( m ) );

			m_Stream.Write( (byte) (MobileStatus.SendAosInfo ? 0x04 : 0x03) ); // type

			m_Stream.Write( m.Female );

			m_Stream.Write( (short) m.Str );
			m_Stream.Write( (short) m.Dex );
			m_Stream.Write( (short) m.Int );

			m_Stream.Write( (short) m.Stam );
			m_Stream.Write( (short) m.StamMax );

			m_Stream.Write( (short) m.Mana );
			m_Stream.Write( (short) m.ManaMax );

			m_Stream.Write( (int) m.TotalGold );
			m_Stream.Write( (short) (Core.AOS ? m.PhysicalResistance : (int)(m.ArmorRating + 0.5)) );
			m_Stream.Write( (short) (Mobile.BodyWeight + m.TotalWeight) );

			m_Stream.Write( (short) m.StatCap );

			m_Stream.Write( (byte) m.Followers );
			m_Stream.Write( (byte) m.FollowersMax );

			if ( MobileStatus.SendAosInfo )
			{
				m_Stream.Write( (short) m.FireResistance ); // Fire
				m_Stream.Write( (short) m.ColdResistance ); // Cold
				m_Stream.Write( (short) m.PoisonResistance ); // Poison
				m_Stream.Write( (short) m.EnergyResistance ); // Energy
				m_Stream.Write( (short) m.Luck ); // Luck

				IWeapon weapon = m.Weapon;

				int min = 0, max = 0;

				if ( weapon != null )
					weapon.GetStatusDamage( m, out min, out max );

				m_Stream.Write( (short) min ); // Damage min
				m_Stream.Write( (short) max ); // Damage max

				m_Stream.Write( (int) m.TithingPoints );
			}
		}
	}

	public sealed class MobileStatus : Packet
	{
		private static bool m_SendAosInfo;

		public static bool SendAosInfo{ get{ return m_SendAosInfo; } set{ m_SendAosInfo = value; } }

		public MobileStatus( Mobile beholder, Mobile beheld ) : base( 0x11 )
		{
			string name = beheld.Name;
			if ( name == null ) name = "";

			this.EnsureCapacity( 43 + (beholder == beheld ? 45 : 0) );

			m_Stream.Write( beheld.Serial );

			m_Stream.WriteAsciiFixed( name, 30 );

			if ( beholder == beheld )
				WriteAttr( beheld.Hits, beheld.HitsMax );
			else
				WriteAttrNorm( beheld.Hits, beheld.HitsMax );

			m_Stream.Write( beheld.CanBeRenamedBy( beholder ) );

			if ( beholder == beheld )
			{
				m_Stream.Write( (byte) (m_SendAosInfo ? 0x04 : 0x03) );

				m_Stream.Write( beheld.Female );

				m_Stream.Write( (short) beheld.Str );
				m_Stream.Write( (short) beheld.Dex );
				m_Stream.Write( (short) beheld.Int );

				WriteAttr( beheld.Stam, beheld.StamMax );
				WriteAttr( beheld.Mana, beheld.ManaMax );

				m_Stream.Write( (int) beheld.TotalGold );
				m_Stream.Write( (short) (Core.AOS ? beheld.PhysicalResistance : (int)(beheld.ArmorRating + 0.5)) );
				m_Stream.Write( (short) (Mobile.BodyWeight + beheld.TotalWeight) );

				m_Stream.Write( (short) beheld.StatCap );

				m_Stream.Write( (byte) beheld.Followers );
				m_Stream.Write( (byte) beheld.FollowersMax );

				if ( m_SendAosInfo )
				{
					m_Stream.Write( (short) beheld.FireResistance ); // Fire
					m_Stream.Write( (short) beheld.ColdResistance ); // Cold
					m_Stream.Write( (short) beheld.PoisonResistance ); // Poison
					m_Stream.Write( (short) beheld.EnergyResistance ); // Energy
					m_Stream.Write( (short) beheld.Luck ); // Luck

					IWeapon weapon = beheld.Weapon;

					int min = 0, max = 0;

					if ( weapon != null )
						weapon.GetStatusDamage( beheld, out min, out max );

					m_Stream.Write( (short) min ); // Damage min
					m_Stream.Write( (short) max ); // Damage max

					m_Stream.Write( (int) beheld.TithingPoints );
				}
			}
			else
			{
				m_Stream.Write( (byte) 0x00 );
			}
		}

		private void WriteAttr( int current, int maximum )
		{
			m_Stream.Write( (short) current );
			m_Stream.Write( (short) maximum );
		}

		private void WriteAttrNorm( int current, int maximum )
		{
			AttributeNormalizer.WriteReverse( m_Stream, current, maximum );
		}
	}

	public sealed class MobileUpdate : Packet
	{
		public MobileUpdate( Mobile m ) : base( 0x20, 19 )
		{
			int hue = m.Hue;

			if ( m.SolidHueOverride >= 0 )
				hue = m.SolidHueOverride;

			m_Stream.Write( (int) m.Serial );
			m_Stream.Write( (short) m.Body );
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (short) hue );
			m_Stream.Write( (byte) m.GetPacketFlags() );
			m_Stream.Write( (short) m.X );
			m_Stream.Write( (short) m.Y );
			m_Stream.Write( (short) 0 );
			m_Stream.Write( (byte) m.Direction );
			m_Stream.Write( (sbyte) m.Z );
		}
	}

	public sealed class MobileIncoming : Packet
	{
		private static int[] m_DupedLayers = new int[256];
		private static int m_Version;

		public Mobile m_Beheld;

		public MobileIncoming( Mobile beholder, Mobile beheld ) : base( 0x78 )
		{
			m_Beheld = beheld;
			++m_Version;

			ArrayList eq = beheld.Items;
			int count = eq.Count;

			this.EnsureCapacity( 23 + (count * 9) );

			int hue = beheld.Hue;

			if ( beheld.SolidHueOverride >= 0 )
				hue = beheld.SolidHueOverride;

			m_Stream.Write( (int) beheld.Serial );
			m_Stream.Write( (short) beheld.Body );
			m_Stream.Write( (short) beheld.X );
			m_Stream.Write( (short) beheld.Y );
			m_Stream.Write( (sbyte) beheld.Z );
			m_Stream.Write( (byte) beheld.Direction );
			m_Stream.Write( (short) hue );
			m_Stream.Write( (byte) beheld.GetPacketFlags() );
			m_Stream.Write( (byte) Notoriety.Compute( beholder, beheld ) );

			for ( int i = 0; i < count; ++i )
			{
				Item item = (Item)eq[i];

				byte layer = (byte) item.Layer;

				if ( !item.Deleted && beholder.CanSee( item ) && m_DupedLayers[layer] != m_Version )
				{
					m_DupedLayers[layer] = m_Version;

					hue = item.Hue;

					if ( beheld.SolidHueOverride >= 0 )
						hue = beheld.SolidHueOverride;

					int itemID = item.ItemID & 0x3FFF;
					bool writeHue = ( hue != 0 );

					if ( writeHue )
						itemID |= 0x8000;

					m_Stream.Write( (int) item.Serial );
					m_Stream.Write( (short) itemID );
					m_Stream.Write( (byte) layer );

					if ( writeHue )
						m_Stream.Write( (short) hue );
				}
			}

			m_Stream.Write( (int) 0 ); // terminate
		}
	}

	public sealed class AsciiMessage : Packet
	{
		public AsciiMessage( Serial serial, int graphic, MessageType type, int hue, int font, string name, string text ) : base( 0x1C )
		{
			if ( name == null )
				name = "";

			if ( text == null )
				text = "";

			if ( hue == 0 )
				hue = 0x3B2;

			this.EnsureCapacity( 45 + text.Length );

			m_Stream.Write( (int) serial );
			m_Stream.Write( (short) graphic );
			m_Stream.Write( (byte) type );
			m_Stream.Write( (short) hue );
			m_Stream.Write( (short) font );
			m_Stream.WriteAsciiFixed( name, 30 );
			m_Stream.WriteAsciiNull( text );
		}
	}

	public sealed class UnicodeMessage : Packet
	{
		public UnicodeMessage( Serial serial, int graphic, MessageType type, int hue, int font, string lang, string name, string text ) : base( 0xAE )
		{
			if ( lang == null || lang == "" ) lang = "ENU";
			if ( name == null ) name = "";
			if ( text == null ) text = "";

			if ( hue == 0 )
				hue = 0x3B2;

			this.EnsureCapacity( 50 + (text.Length * 2) );

			m_Stream.Write( (int) serial );
			m_Stream.Write( (short) graphic );
			m_Stream.Write( (byte) type );
			m_Stream.Write( (short) hue );
			m_Stream.Write( (short) font );
			m_Stream.WriteAsciiFixed( lang, 4 );
			m_Stream.WriteAsciiFixed( name, 30 );
			m_Stream.WriteBigUniNull( text );
		}
	}

	public sealed class PingAck : Packet
	{
		private static PingAck[] m_Cache = new PingAck[0x100];

		public static PingAck Instantiate( byte ping )
		{
			PingAck p = m_Cache[ping];

			if ( p == null )
				m_Cache[ping] = p = new PingAck( ping );

			return p;
		}

		public PingAck( byte ping ) : base( 0x73, 2 )
		{
			m_Stream.Write( ping );
		}
	}

	public sealed class MovementRej : Packet
	{
		public MovementRej( int seq, Mobile m ) : base( 0x21, 8 )
		{
			m_Stream.Write( (byte) seq );
			m_Stream.Write( (short) m.X );
			m_Stream.Write( (short) m.Y );
			m_Stream.Write( (byte) m.Direction );
			m_Stream.Write( (sbyte) m.Z );
		}
	}

	public sealed class MovementAck : Packet
	{
		private static MovementAck[][] m_Cache = new MovementAck[8][]
			{
				new MovementAck[256],
				new MovementAck[256],
				new MovementAck[256],
				new MovementAck[256],
				new MovementAck[256],
				new MovementAck[256],
				new MovementAck[256],
				new MovementAck[256]
			};

		public static MovementAck Instantiate( int seq, Mobile m )
		{
			int noto = Notoriety.Compute( m, m );

			MovementAck p = m_Cache[noto][seq];

			if ( p == null )
				m_Cache[noto][seq] = p = new MovementAck( seq, noto );

			return p;
		}

		private MovementAck( int seq, int noto ) : base( 0x22, 3 )
		{
			m_Stream.Write( (byte) seq );
			m_Stream.Write( (byte) noto );
		}
	}

	public sealed class LoginConfirm : Packet
	{
		public LoginConfirm( Mobile m ) : base( 0x1B, 37 )
		{
			m_Stream.Write( (int) m.Serial );
			m_Stream.Write( (int) 0 );
			m_Stream.Write( (short) m.Body );
			m_Stream.Write( (short) m.X );
			m_Stream.Write( (short) m.Y );
			m_Stream.Write( (short) m.Z );
			m_Stream.Write( (byte) m.Direction );
			m_Stream.Write( (byte) 0 );
			m_Stream.Write( (int) -1 );

			Map map = m.Map;

			if ( map == null || map == Map.Internal )
				map = m.LogoutMap;

			m_Stream.Write( (short) 0 );
			m_Stream.Write( (short) 0 );
			m_Stream.Write( (short) (map==null?6144:map.Width) );
			m_Stream.Write( (short) (map==null?4096:map.Height) );

			m_Stream.Fill();
		}
	}

	public sealed class LoginComplete : Packet
	{
		public static readonly LoginComplete Instance = new LoginComplete();

		public LoginComplete() : base( 0x55, 1 )
		{
		}
	}

	public sealed class CityInfo
	{
		private string m_City;
		private string m_Building;
		private Point3D m_Location;
		private Map m_Map;

		public CityInfo( string city, string building, int x, int y, int z, Map m )
		{
			m_City = city;
			m_Building = building;
			m_Location = new Point3D( x, y, z );
			m_Map = m;
		}

		public CityInfo( string city, string building, int x, int y, int z ) : this( city, building, x, y, z, Map.Trammel )
		{
		}

		public string City
		{
			get
			{
				return m_City;
			}
			set
			{
				m_City = value;
			}
		}

		public string Building
		{
			get
			{
				return m_Building;
			}
			set
			{
				m_Building = value;
			}
		}

		public int X
		{
			get
			{
				return m_Location.X;
			}
			set
			{
				m_Location.X = value;
			}
		}

		public int Y
		{
			get
			{
				return m_Location.Y;
			}
			set
			{
				m_Location.Y = value;
			}
		}

		public int Z
		{
			get
			{
				return m_Location.Z;
			}
			set
			{
				m_Location.Z = value;
			}
		}

		public Point3D Location
		{
			get
			{
				return m_Location;
			}
			set
			{
				m_Location = value;
			}
		}

		public Map Map
		{
			get{ return m_Map; }
			set{ m_Map = value; }
		}
	}

	public sealed class CharacterListUpdate : Packet
	{
		public CharacterListUpdate( IAccount a ) : base( 0x86 )
		{
			this.EnsureCapacity( 304 );

			m_Stream.Write( (byte) a.Count );

			for ( int i = 0; i < a.Length; ++i )
			{
				Mobile m = a[i];

				if ( m != null )
				{
					m_Stream.WriteAsciiFixed( m.Name, 30 );
					m_Stream.Fill( 30 ); // password
				}
				else
				{
					m_Stream.Fill( 60 );
				}
			}
		}
	}

	public sealed class CharacterList : Packet
	{
		public CharacterList( IAccount a, CityInfo[] info ) : base( 0xA9 )
		{
			this.EnsureCapacity( 309 + (info.Length * 63) );

			int highSlot = -1;

			for ( int i = 0; i < a.Length; ++i )
			{
				if ( a[i] != null )
					highSlot = i;
			}

			int count = Math.Max( Math.Max( highSlot + 1, a.Limit ), 5 );

			m_Stream.Write( (byte) count );

			for ( int i = 0; i < count; ++i )
			{
				if ( a[i] != null )
				{
					m_Stream.WriteAsciiFixed( a[i].Name, 30 );
					m_Stream.Fill( 30 ); // password
				}
				else
				{
					m_Stream.Fill( 60 );
				}
			}

			m_Stream.Write( (byte) info.Length );

			for ( int i = 0; i < info.Length; ++i )
			{
				CityInfo ci = info[i];

				m_Stream.Write( (byte) i );
				m_Stream.WriteAsciiFixed( ci.City, 31 );
				m_Stream.WriteAsciiFixed( ci.Building, 31 );
			}

			int flags = 0x08; //Context Menus

			if ( Core.SE )
				flags |= 0xA0; //SE & AOS
			else if ( Core.AOS )
				flags |= 0x20; //AOS

			if ( count >= 6 )
				flags |= 0x40; // 6th character slot
			else if ( count == 1 )
				flags |= 0x14; // Limit characters & one character

			m_Stream.Write( (int)(flags | m_AdditionalFlags) ); // flags
		}

		private static int m_AdditionalFlags;

		public static int AdditionalFlags
		{
			get{ return m_AdditionalFlags; }
			set{ m_AdditionalFlags = value; }
		}
	}

	public class ClearWeaponAbility : Packet
	{
		public static readonly Packet Instance = new ClearWeaponAbility();

		public ClearWeaponAbility() : base( 0xBF )
		{
			EnsureCapacity( 5 );

			m_Stream.Write( (short) 0x21 );
		}
	}

	public class DisplayStringQuery : Packet
	{
		public DisplayStringQuery( int serial, string caption, bool cancellable,
								   byte number, int max, string label ) : base( 0xAB )
		{
			int len = caption.Length + label.Length + 21;
			
			EnsureCapacity( len );

			m_Stream.Write( serial ); // query serial
			m_Stream.Write( (short)0 ); // unknown

			m_Stream.Write( (short)(caption.Length + 1) );
			m_Stream.WriteAsciiNull( caption );
			
			m_Stream.Write( cancellable ); // is able to cancel? true/false

			m_Stream.Write( number ); // query type 1 = string, 2 = number

			m_Stream.Write( max ); // max. if string it's max length, if number it's maximum number

			m_Stream.Write( (short)(label.Length + 1) );
			m_Stream.WriteAsciiNull( label );
		}
	}

	public enum ALRReason : byte
	{
		Invalid = 0x00,
		InUse = 0x01,
		Blocked = 0x02,
		BadPass = 0x03,
		Idle = 0xFE,
		BadComm = 0xFF
	}

	public sealed class AccountLoginRej : Packet
	{
		public AccountLoginRej( ALRReason reason ) : base( 0x82, 2 )
		{
			m_Stream.Write( (byte)reason );
		}
	}

	public enum AffixType : byte
	{
		Append = 0x00,
		Prepend = 0x01,
		System = 0x02
	}

	public sealed class MessageLocalizedAffix : Packet
	{
		public MessageLocalizedAffix( Serial serial, int graphic, MessageType messageType, int hue, int font, int number, string name, AffixType affixType, string affix, string args ) : base( 0xCC )
		{
			if (Core.Config.Features["delocalize"]) {
				string text = StringList.Localization[number];
				if (text != null) {
					if ( (affixType & AffixType.Prepend) == AffixType.Prepend )
						text = affix + text;
					else
						text += affix;

					/* this hack replaces the packet data generated by
					   this class with an AsciiMessage packet */
					Packet p = new AsciiMessage(serial, graphic, messageType,
												hue, font, name, text);
					EnsureCapacity(3);
					m_Stream = p.UnderlyingStream;
					return;
				}
			}

			if ( name == null ) name = "";
			if ( affix == null ) affix = "";
			if ( args == null ) args = "";

			if ( hue == 0 )
				hue = 0x3B2;

			this.EnsureCapacity( 52 + affix.Length + (args.Length * 2) );

			m_Stream.Write( (int) serial );
			m_Stream.Write( (short) graphic );
			m_Stream.Write( (byte) messageType );
			m_Stream.Write( (short) hue );
			m_Stream.Write( (short) font );
			m_Stream.Write( (int) number );
			m_Stream.Write( (byte) affixType );
			m_Stream.WriteAsciiFixed( name, 30 );
			m_Stream.WriteAsciiNull( affix );
			m_Stream.WriteBigUniNull( args );
		}
	}

	public sealed class ServerInfo
	{
		private string m_Name;
		private int m_FullPercent;
		private int m_TimeZone;
		private IPEndPoint m_Address;

		public string Name
		{
			get
			{
				return m_Name;
			}
			set
			{
				m_Name = value;
			}
		}

		public int FullPercent
		{
			get
			{
				return m_FullPercent;
			}
			set
			{
				m_FullPercent = value;
			}
		}

		public int TimeZone
		{
			get
			{
				return m_TimeZone;
			}
			set
			{
				m_TimeZone = value;
			}
		}

		public IPEndPoint Address
		{
			get
			{
				return m_Address;
			}
			set
			{
				m_Address = value;
			}
		}

		public ServerInfo( string name, int fullPercent, TimeZone tz, IPEndPoint address )
		{
			m_Name = name;
			m_FullPercent = fullPercent;
			m_TimeZone = tz.GetUtcOffset( Core.Now ).Hours;
			m_Address = address;
		}
	}

	public sealed class FollowMessage : Packet
	{
		public FollowMessage( Serial serial1, Serial serial2 ) : base( 0x15, 9 )
		{
			m_Stream.Write( (int) serial1 );
			m_Stream.Write( (int) serial2 );
		}
	}

	public sealed class AccountLoginAck : Packet
	{
		public AccountLoginAck( ServerInfo[] info ) : base( 0xA8 )
		{
			this.EnsureCapacity( 6 + (info.Length * 40) );

			m_Stream.Write( (byte) 0x5D ); // Unknown

			m_Stream.Write( (ushort) info.Length );

			for ( int i = 0; i < info.Length; ++i )
			{
				ServerInfo si = info[i];

				m_Stream.Write( (ushort) i );
				m_Stream.WriteAsciiFixed( si.Name, 32 );
				m_Stream.Write( (byte) si.FullPercent );
				m_Stream.Write( (sbyte) si.TimeZone );
				m_Stream.Write( (int) si.Address.Address.Address );
			}
		}
	}

	public sealed class DisplaySignGump : Packet
	{
		public DisplaySignGump( Serial serial, int gumpID, string unknown, string caption ) : base( 0x8B )
		{
			if ( unknown == null ) unknown = "";
			if ( caption == null ) caption = "";

			this.EnsureCapacity( 16 + unknown.Length + caption.Length );

			m_Stream.Write( (int) serial );
			m_Stream.Write( (short) gumpID );
			m_Stream.Write( (short) (unknown.Length) );
			m_Stream.WriteAsciiFixed( unknown, unknown.Length );
			m_Stream.Write( (short) (caption.Length + 1) );
			m_Stream.WriteAsciiFixed( caption, caption.Length + 1 );
		}
	}

	public sealed class GodModeReply : Packet
	{
		public GodModeReply( bool reply ) : base( 0x2B, 2 )
		{
			m_Stream.Write( reply );
		}
	}

	public sealed class PlayServerAck : Packet
	{
		internal static int m_AuthID = -1;

		public PlayServerAck( ServerInfo si ) : base( 0x8C, 11 )
		{
			int addr = (int)si.Address.Address.Address;

			m_Stream.Write( (byte) addr );
			m_Stream.Write( (byte)(addr >> 8) );
			m_Stream.Write( (byte)(addr >> 16) );
			m_Stream.Write( (byte)(addr >> 24) );

			m_Stream.Write( (short) si.Address.Port );
			m_Stream.Write( (int) m_AuthID );
		}
	}

	public sealed class AddAuthIDAck : Packet
	{
		public AddAuthIDAck( int authID ) : base( 0xBF )
		{
			EnsureCapacity( 7 + 4 );

			m_Stream.Write( (ushort) 0x5555 );
			m_Stream.Write( (ushort) 0x0002 );
			m_Stream.Write( (int) authID );
		}
	}

	public abstract class Packet
	{
		protected PacketWriter m_Stream;
		private int m_PacketID;
		private int m_Length;

		public int PacketID
		{
			get{ return m_PacketID; }
		}

		public Packet( int packetID )
		{
			m_PacketID = packetID;

			PacketProfile prof = PacketProfile.GetOutgoingProfile( (byte)packetID );

			if ( prof != null )
				prof.RegConstruct();
		}

		public void EnsureCapacity( int length )
		{
			m_Stream = PacketWriter.CreateInstance( length );// new PacketWriter( length );
			m_Stream.Write( (byte) m_PacketID );
			m_Stream.Write( (short) 0 );
		}

		public Packet( int packetID, int length )
		{
			m_PacketID = packetID;
			m_Length = length;

			m_Stream = PacketWriter.CreateInstance( length );// new PacketWriter( length );
			m_Stream.Write( (byte) packetID );

			PacketProfile prof = PacketProfile.GetOutgoingProfile( (byte)packetID );

			if ( prof != null )
				prof.RegConstruct();
		}

		public PacketWriter UnderlyingStream
		{
			get
			{
				return m_Stream;
			}
		}

		private byte[] m_CompiledBuffer;

		public byte[] Compile( bool compress )
		{
			if ( m_CompiledBuffer == null )
				InternalCompile( compress );

			return m_CompiledBuffer;
		}

		private void InternalCompile( bool compress )
		{
			if ( m_Length == 0 )
			{
				long streamLen = m_Stream.Length;

				m_Stream.Seek( 1, SeekOrigin.Begin );
				m_Stream.Write( (ushort) streamLen );
			}
			else if ( m_Stream.Length != m_Length )
			{
				int diff = (int)m_Stream.Length - m_Length;

				Console.WriteLine( "Packet: 0x{0:X2}: Bad packet length! ({1}{2} bytes)", m_PacketID, diff >= 0 ? "+" : "", diff );
			}

			MemoryStream ms = m_Stream.UnderlyingStream;

			int length;

			m_CompiledBuffer = ms.GetBuffer();
			length = (int)ms.Length;

			if ( compress )
			{
				try
				{
					Compression.Compress( m_CompiledBuffer, length, out m_CompiledBuffer, out length );
				}
				catch ( IndexOutOfRangeException )
				{
					Console.WriteLine( "Warning: Compression buffer overflowed on packet 0x{0:X2} ('{1}') (length={2})", m_PacketID, GetType().Name, length );

					m_CompiledBuffer = null;
				}
			}

			if ( m_CompiledBuffer != null )
			{
				byte[] old = m_CompiledBuffer;

				m_CompiledBuffer = new byte[length];

				Buffer.BlockCopy( old, 0, m_CompiledBuffer, 0, length );
			}

			PacketWriter.ReleaseInstance( m_Stream );
			m_Stream = null;
		}
	}
}