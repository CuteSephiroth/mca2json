using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace mca2json
{
    class Program
    {
		static void ProcessOne(string inFile, string outFile)
		{
			Console.WriteLine("Process: " + inFile); 
			string fileName = inFile.Split('/')[inFile.Split('/').Length - 1];
			Region region;
			Console.WriteLine("Read region file");
			if (ChunkUtils.ReadRegionFile(inFile, out region))
			{
				Console.WriteLine("Read file failed");
				return;
			}
			Console.WriteLine("Decode chunk data");
			if (ChunkUtils.DecodeChunkData(region))
			{
				Console.WriteLine("Decode chunk data failed");
				return;
			}
			Console.WriteLine("Decode section data");
			if (ChunkUtils.DecodeSectionData(region))
			{
				Console.WriteLine("Decode section data failed");
				return;
			}
			Console.WriteLine("Decode block data");
			if (ChunkUtils.DecodeBlockData(region))
			{
				Console.WriteLine("Decode block data failed");
				return;
			}
			Console.WriteLine("Clean up");
			region.CleanUp();
			Console.WriteLine("Serialize data");
			string regionStr = JsonConvert.SerializeObject(region, Formatting.Indented);
			Console.WriteLine("Saving");
			FileStream fs;
			fs = new FileStream(outFile, FileMode.CreateNew);
			StreamWriter sw = new StreamWriter(fs);
			sw.WriteLine(regionStr);
			sw.Close();
			fs.Close();
			Console.WriteLine(inFile+" done");
			Console.WriteLine();
		}
        static void Main(string[] args)
        {
			if (args.Length < 1) { Console.WriteLine("No file input"); return; }
			bool listMode = true;
			foreach(string str in args)
			{
				if (!str.Contains(".mca")) { listMode = false; break; }
			}
			if(listMode)
			{
				foreach(string fileUrl in args)
				{
					string fileName = fileUrl.Split('/')[fileUrl.Split('/').Length - 1];
					string newFileName = fileName.Replace(".mca", ".json");
					string newFileUrl = fileUrl.Replace(fileName, newFileName);
					ProcessOne(fileUrl, newFileUrl);
				}
			}
			else
			{
				ProcessOne(args[0], args[1]);	
			}
			Console.WriteLine("All done");
        }
    }
}
