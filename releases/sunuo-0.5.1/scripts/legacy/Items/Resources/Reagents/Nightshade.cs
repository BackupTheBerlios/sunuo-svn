using System;
using Server;
using Server.Items;

namespace Server.Items
{
	public class Nightshade : BaseReagent, ICommodity
	{
		string ICommodity.Description
		{
			get
			{
				return String.Format( "{0} nightshade", Amount );
			}
		}

		[Constructable]
		public Nightshade() : this( 1 )
		{
		}

		[Constructable]
		public Nightshade( int amount ) : base( 0xF88, amount )
		{
		}

		public Nightshade( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Nightshade( amount ), amount );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			/*int version = */reader.ReadInt();
		}
	}
}