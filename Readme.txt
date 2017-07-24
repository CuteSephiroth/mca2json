模式1：
拖入一连串mac文件自动转换，生成文件在同目录下同文件名以.json结尾

模式2:
第一个参数为mca文件地址，第二个参数为保存地址。

备注：
*保存的json格式中，未保存block的坐标。因为数据量过大，保存坐标序列化后单文件高达几百M。
*只保存了Block 和 Data两种数据，其他如Lightpropulated等数据并未保存。

对于Block相对于Section的坐标如下：
//每一个Section包含16x16x16个block即4096
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
