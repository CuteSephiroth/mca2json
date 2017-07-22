using System;
using System.Collections.Generic;
using System.Text;

namespace mca2json
{
	class Block
	{
		public Block()
		{
			XYZ = new byte[3];
		}
		public byte[] XYZ;

		public byte blockID;
		public byte dataID;
	}
}
