using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace mca2json
{
	[JsonObject(MemberSerialization.OptIn)]
	class Chunk
    {
		public Chunk()
		{
			isEmpty = false;
			sectorCount = 0;
			chunkSize = 0;
			compressType = 0;
			chunkIDX = -1;
			chunkIDZ = -1;
			sections = new Section[16];
			for(int i=0;i<16;++i)
			{
				sections[i] = new Section();
			}

			sectionUnits = new List<Section>();

			currentBlockIndex = 0;
			currentDataIndex = 0;
			currentYIndex = 0;
		}

		public void CleanUp()
		{
			sections = null;
			chunkData = null;
			foreach(Section section in sectionUnits)
			{
				section.CleanUp();
			}
		}

		public void MakeUp()
		{
			for(int i=0;i<sections.Length;++i)
			{
				if(sections[i].YValid)
				{
					sectionUnits.Add(sections[i]);
				}
			}
		}

		[JsonProperty]
		public int chunkIDX;
		[JsonProperty]
		public int chunkIDZ;
		[JsonProperty]
		public bool isEmpty;

		public int sectorCount;
		public int chunkSize;
		public int compressType;

		[JsonProperty]
		public List<Section> sectionUnits;

		public Section[] sections;

		public byte[] chunkData;

		// temp data
		public int currentBlockIndex;
		public int currentDataIndex;
		public int currentYIndex;
	}
}
