using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using zlib;

namespace mca2json
{
	enum TAG_ID
	{
		TAG_END = 0,
		TAG_BYTE = 1,
		TAG_SHORT = 2,
		TAG_INT = 3,
		TAG_LONG = 4,
		TAG_FLOAT = 5,
		TAG_DOUBLE = 6,
		TAG_BYTEARRAY = 7,
		TAG_STRING = 8,
		TAG_LIST = 9,
		TAG_COMPOUND = 10,
		TAG_INTARRAY = 11,
	};
	static class ChunkUtils
    {
        // return true if failed
        public static bool ReadRegionFile(string fileName, out Region outRegion)
        {
            bool readResult = false;
			outRegion = new Region();
            try
            {
                FileStream fileStream = new FileStream(fileName, FileMode.Open);
                byte[] datas = new byte[fileStream.Length];
                fileStream.Read(datas, 0, (int)fileStream.Length);
                fileStream.Close();
                outRegion.regionX = int.Parse(fileName.Split('.')[1]);
                outRegion.regionZ = int.Parse(fileName.Split('.')[2]);
                outRegion.datas = datas;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                readResult = true;
            }
            return readResult;
        }

		//decode the chunk data from the region data, return true if failed
		public static bool DecodeChunkData(Region inRegion)
		{
			bool result=false;
			for(int chunkindex = 0; chunkindex < 1024;++chunkindex)
			{
				Chunk tempChunk = new Chunk();

				int regionidx = inRegion.regionX << 5;
				int regionidz = inRegion.regionZ << 5;
				// bias for block
				int biasx = chunkindex & 31;
				int biasz = (chunkindex >> 5) & 31;

				tempChunk.chunkIDX = regionidx + biasx;
				tempChunk.chunkIDZ = regionidz + biasz;

				inRegion.chunkDatas.Add(tempChunk);
				tempChunk.isEmpty = false;
				int offset = inRegion.datas[4 * chunkindex + 0] * 65536 + inRegion.datas[4 * chunkindex + 1] * 256 + inRegion.datas[4 * chunkindex + 2];
				tempChunk.sectorCount = inRegion.datas[4 * chunkindex + 3];
				if (offset == 0) { tempChunk.isEmpty = true;continue; }

				int chunkdatabias = 4096 * offset;
				
				int chunkSize = inRegion.datas[chunkdatabias + 0] * 65536 * 256 + inRegion.datas[chunkdatabias + 1] * 65536 + inRegion.datas[chunkdatabias + 2] * 256 + inRegion.datas[chunkdatabias + 3];
				tempChunk.compressType = inRegion.datas[chunkdatabias + 4];
				if (chunkSize < 200) { tempChunk.isEmpty = true; continue; }
				// save our data to chunk
				// the compressType was count in so...
				tempChunk.chunkSize = chunkSize - 1;
				tempChunk.chunkData = new byte[tempChunk.chunkSize];
				Array.Copy(inRegion.datas, chunkdatabias + 5, tempChunk.chunkData, 0, tempChunk.chunkSize);
			}
			return result;
		}
		// decode the section data from chunk data. return true if failed
		public static bool DecodeSectionData(Region inRegion)
		{
			bool result = false;
			foreach(Chunk chunk in inRegion.chunkDatas)
			{
				if(!chunk.isEmpty)
				{
					byte[] deCompressedData = deCompressBytes(chunk.chunkData);
					int outputtagid;
					ReadChunk(deCompressedData, 3, chunk, out outputtagid);
				}
			}
			return result;
		}

		// decode the block data from the section data. return true if failed
		public static bool DecodeBlockData(Region inRegion)
		{
			bool result = false;
			foreach(Chunk chunk in inRegion.chunkDatas)
			{
				/*
				foreach(Section section in chunk.sections)
				{
					if (!section.YValid) { continue; }
					for (int i = 0; i < 4096; i++)
					{
						// get the relative location
						Block block = new Block();
						block.XYZ[0] = (byte)(i & 0x000f);
						block.XYZ[1] = (byte)(i >> 4 & 0x000f);
						block.XYZ[2] = (byte)(((i >> 8) & 0x000f) | (section.Y[0] << 4));
						block.blockID = section.block[i];
						block.dataID = (byte)( i % 2 == 0 ? section.data[i / 2] & 0xf : (section.data[i / 2] >> 4) & 0xf);
						section.blockUnits.Add(block);
					}
				}
				*/
				chunk.MakeUp();
			}
			return result;
		}

		private static int ReadChunk(byte[] pBuffer, int offset, Chunk refChunk, out int outputtagid)
		{
			int tagid = pBuffer[offset];
			outputtagid = tagid;
			if(tagid==0)
			{
				return offset + 1; //tag end
			}
			// we assume that the label string is not longer than 255
			offset += 2;

			int strnamelenght = pBuffer[offset];
			offset += 1;
			string tagname = "";
			for (int i = 0; i < strnamelenght; ++i) { tagname += (char)(pBuffer[offset + i]); }
			offset += strnamelenght;
			switch(tagname)
			{
				case "Blocks":
				{
						refChunk.sections[refChunk.currentBlockIndex].blockValid = true;
						return ParseChunk(pBuffer, offset, (TAG_ID)tagid, refChunk, refChunk.sections[refChunk.currentBlockIndex++].block);
				}
				case "Data":
				{
						refChunk.sections[refChunk.currentDataIndex].dataValid = true;
						return ParseChunk(pBuffer, offset, (TAG_ID)tagid, refChunk, refChunk.sections[refChunk.currentDataIndex++].data);
				}
				case "Y":
				{
						refChunk.sections[refChunk.currentYIndex].YValid = true;
						return ParseChunk(pBuffer, offset, (TAG_ID)tagid, refChunk, refChunk.sections[refChunk.currentYIndex++].Y);
				}
				case "TileEntities": case "Entities":
				{
						for(int i=0;i<32&&(offset+i)<pBuffer.Length;++i)
						{
							pBuffer[offset + i] = 0;
						}
						return offset;
				}
				default:
				{
						return ParseChunk(pBuffer, offset, (TAG_ID)tagid, refChunk, null);
				}
			}
		}

		private static int ParseChunk(byte[] pbuffer, int offset, TAG_ID tagid, Chunk refChunk, byte[] readbuf)
		{
			int listtagid;
			int arraylength;
			int comtagid = 1;
			switch (tagid)
			{
				case TAG_ID.TAG_BYTE:
				{
						if(readbuf!=null)
						{
							readbuf[0] = pbuffer[offset];
						}
						offset += 1;
						break;
				}
				case TAG_ID.TAG_SHORT:
				{
						offset += 2;
						break;
				}
				case TAG_ID.TAG_INT:
				{
						offset += 4;
						break;
				}
				case TAG_ID.TAG_LONG:
				{
						offset += 8;
						break;
				}
				case TAG_ID.TAG_FLOAT:
				{
						offset += 4;
						break;
				}
				case TAG_ID.TAG_DOUBLE:
				{
						offset += 8;
						break;
				}
				case TAG_ID.TAG_BYTEARRAY:
				{
						arraylength = pbuffer[offset + 0] << 24 | pbuffer[offset + 1] << 16 | pbuffer[offset + 2] << 8 | pbuffer[offset + 3];
						offset += 4;
						if(readbuf!=null)
						{
							Array.Copy(pbuffer, offset, readbuf, 0, arraylength);
						}
						offset += arraylength;
						break;
				}
				case TAG_ID.TAG_STRING:
				{
						offset += pbuffer[offset] + 1;
						break;
				}
				case TAG_ID.TAG_LIST:
				{
						listtagid = pbuffer[offset];
						offset += 1;
						arraylength = pbuffer[offset+0] << 24 | pbuffer[offset + 1] << 16 | pbuffer[offset + 2] << 8 | pbuffer[offset + 3];
						offset += 4;
						for (int i = 0; i < arraylength; i++)
						{
							offset = ParseChunk(pbuffer, offset, (TAG_ID)listtagid, refChunk, null);
						}
						break;
				}
				case TAG_ID.TAG_COMPOUND:
				{
						while ((TAG_ID)comtagid != TAG_ID.TAG_END)
						{
							offset = ReadChunk(pbuffer, offset, refChunk, out comtagid);
						}
						break;
				}
				case TAG_ID.TAG_INTARRAY:
				{
						arraylength = pbuffer[offset + 0] << 24 | pbuffer[offset + 1] << 16 | pbuffer[offset + 2] << 8 | pbuffer[offset + 3];
						offset += 4;
						offset += 4 * arraylength;
						break;
				}
			}
			return offset;
		}

		#region Compress/Decompress
		/// <summary>
		/// 复制流
		/// </summary>
		/// <param name="input">原始流</param>
		/// <param name="output">目标流</param>
		private static void CopyStream(System.IO.Stream input, System.IO.Stream output)
		{
			byte[] buffer = new byte[2000];
			int len;
			while ((len = input.Read(buffer, 0, 2000)) > 0)
			{
				output.Write(buffer, 0, len);
			}
			output.Flush();
		}
		/// <summary>
		/// 压缩字节数组
		/// </summary>
		/// <param name="sourceByte">需要被压缩的字节数组</param>
		/// <returns>压缩后的字节数组</returns>
		private static byte[] compressBytes(byte[] sourceByte)
		{
			MemoryStream inputStream = new MemoryStream(sourceByte);
			Stream outStream = compressStream(inputStream);
			byte[] outPutByteArray = new byte[outStream.Length];
			outStream.Position = 0;
			outStream.Read(outPutByteArray, 0, outPutByteArray.Length);
			outStream.Close();
			inputStream.Close();
			return outPutByteArray;
		}
		/// <summary>
		/// 解压缩字节数组
		/// </summary>
		/// <param name="sourceByte">需要被解压缩的字节数组</param>
		/// <returns>解压后的字节数组</returns>
		private static byte[] deCompressBytes(byte[] sourceByte)
		{
			MemoryStream inputStream = new MemoryStream(sourceByte);
			Stream outputStream = deCompressStream(inputStream);
			byte[] outputBytes = new byte[outputStream.Length];
			outputStream.Position = 0;
			outputStream.Read(outputBytes, 0, outputBytes.Length);
			outputStream.Close();
			inputStream.Close();
			return outputBytes;
		}
		/// <summary>
		/// 压缩流
		/// </summary>
		/// <param name="sourceStream">需要被压缩的流</param>
		/// <returns>压缩后的流</returns>
		private static Stream compressStream(Stream sourceStream)
		{
			MemoryStream streamOut = new MemoryStream();
			ZOutputStream streamZOut = new ZOutputStream(streamOut, zlibConst.Z_DEFAULT_COMPRESSION);
			CopyStream(sourceStream, streamZOut);
			streamZOut.finish();
			return streamOut;
		}
		/// <summary>
		/// 解压缩流
		/// </summary>
		/// <param name="sourceStream">需要被解压缩的流</param>
		/// <returns>解压后的流</returns>
		private static Stream deCompressStream(Stream sourceStream)
		{
			MemoryStream outStream = new MemoryStream();
			ZOutputStream outZStream = new ZOutputStream(outStream);
			CopyStream(sourceStream, outZStream);
			outZStream.finish();
			return outStream;
		} 
		#endregion
	}
}
