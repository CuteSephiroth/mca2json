ģʽ1��
����mac�ļ��Զ�ת���������ļ���ͬĿ¼����.json��β

ģʽ2:
��һ������Ϊmca�ļ���ַ���ڶ�������Ϊ�����ַ��

��ע��
�����json��ʽ�У�δ����block�����ꡣ��Ϊ���������󣬱����������л����ļ��ߴＸ��M��

����Block�����Section���������£�
//ÿһ��Section����16x16x16��block��4096
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