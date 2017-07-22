using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace mca2json
{
	[JsonObject(MemberSerialization.OptIn)]
	class Section
    {
		public Section()
		{
			block = new byte[4096];
			data = new byte[2048];
			add = new byte[2048];
			Y = new byte[1];
			//blockUnits = new List<Block>(4096);

			blockValid = false;
			dataValid = false;
			addValid = false;
			YValid = false;
		}

		public void CleanUp()
		{
			/*
			block = null;
			data = null;
			add = null;
			Y = null;
			*/
		}

		public bool blockValid;
		public bool dataValid;
		public bool addValid;
		public bool YValid;

		[JsonProperty]
		public byte[] block;

		[JsonProperty]
		public byte[] data;

		public byte[] add;

		[JsonProperty]
		public byte[] Y;

		//[JsonProperty]
		//public List<Block> blockUnits;
    }
}
