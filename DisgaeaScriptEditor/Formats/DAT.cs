using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace DisgaeaScriptEditor.Formats
{
    class DAT
    {
        static string filename;
        static string exdir;
        static string packdir;

        static byte[] fileData;
        static int fileDataLength;
        static int fileCount;
        static int fileSize;
        static int tableSize;
        static int pointerValue;
        static int pointerOffset;
        static int scriptOffset;
        static int dataOffset;
        static int dataSize;
        static int script;
        static int offset;
        static int nextOffset;

        static byte[] header = new byte[4];
        static byte[] scriptID = new byte[4];
        static byte[] listValue = new byte[4];
        static byte[] workingFile;
        static byte[] pointerArray;
        static byte[] scriptArray;
        static byte[] dataArray;
        static byte[] packedArray;

        static int storeInt32(params byte[] args) => BitConverter.ToInt32(args, 0);

        static BinaryReader br;

        public static void Unpack()
        {
            using (br = new BinaryReader(File.OpenRead(MainWindow.UserFile)))
            {
                fileData = File.ReadAllBytes(MainWindow.UserFile);
                fileDataLength = fileData.Length;
                fileCount = BitConverter.ToInt32(fileData.Skip(0).Take(4).ToArray(), 0);
                tableSize = 4 * fileCount;
                dataSize = fileDataLength - (tableSize * 2) - 4;
                pointerOffset = 4;
                scriptOffset = 4 * (fileCount + 1);
                dataOffset = 4 * (fileCount * 2 + 1);
                pointerArray = new byte[tableSize];
                scriptArray = new byte[tableSize];
                dataArray = new byte[dataSize];

                Array.Copy(fileData, pointerOffset, pointerArray, 0, tableSize); 
                Array.Copy(fileData, scriptOffset, scriptArray, 0, tableSize);
                Array.Copy(fileData, dataOffset, dataArray, 0, dataSize);

                exdir = System.IO.Path.GetDirectoryName(MainWindow.UserFile) + "/Extracted/";
                Directory.CreateDirectory(exdir);

                for (int i = 0; i < fileCount; i++)
                {
                    offset = storeInt32(fileData.Skip(pointerOffset + (4 * i)).Take(4).ToArray());
                    script = storeInt32(fileData.Skip(scriptOffset + (4 * i)).Take(4).ToArray());
                    filename = Convert.ToString(script).PadLeft(8, '0');

                    if (i != (fileCount - 1))
                    {
                        nextOffset = storeInt32(pointerArray.Skip(pointerOffset + (4 * i)).Take(4).ToArray());
                        fileSize = nextOffset - offset;

                        workingFile = new byte[fileSize];
                        Array.Copy(dataArray, offset, workingFile, 0, fileSize);

                        var bw = new BinaryWriter(File.Create(exdir + filename + ".BIN"));
                        bw.Write(workingFile);
                        bw.Close();
                    }
                    else
                    {
                        fileSize = dataSize - offset;

                        if (fileSize == 786)
                        {
                            if (BitConverter.ToInt32(dataArray.Skip(offset + 773).Take(13).ToArray(), 0) == 0)
                            {
                                fileSize = 773;
                            }
                        }

                        workingFile = new byte[fileSize];
                        Array.Copy(dataArray, offset, workingFile, 0, fileSize);

                        var bw = new BinaryWriter(File.Create(exdir + filename + ".BIN"));
                        bw.Write(workingFile);
                        bw.Close();
                    }
                }
                br.Close();
            }
        }

        public static void Pack()
        {
            List<byte[]> pointerList = new List<byte[]>();
            List<byte[]> scriptList = new List<byte[]>();
            List<byte[]> dataList = new List<byte[]>();
            List<byte[]> packedList = new List<byte[]>();

            dataSize = 0;
            fileCount = Directory.EnumerateFiles(MainWindow.UserFolder, "*.bin").Count();
            header = BitConverter.GetBytes(fileCount);
            pointerList.Add(BitConverter.GetBytes(0));

            foreach (string scrfile in Directory.EnumerateFiles(MainWindow.UserFolder, "*.bin"))
            {
                filename = System.IO.Path.GetFileNameWithoutExtension(scrfile);
                script = Int32.Parse(filename);
                scriptID = BitConverter.GetBytes(script);
                scriptList.Add(scriptID);

                using (br = new BinaryReader(File.OpenRead(scrfile)))
                {
                    fileData = File.ReadAllBytes(scrfile);
                    fileDataLength = fileData.Length;
                    pointerValue = dataSize + fileDataLength;
                    dataSize = pointerValue;

                    listValue = BitConverter.GetBytes(pointerValue);
                    pointerList.Add(listValue);
                    dataList.Add(fileData);

                    br.Close();
                }
            }

            pointerList.Remove(listValue);
            pointerArray = pointerList.SelectMany(a => a).ToArray();
            scriptArray = scriptList.SelectMany(a => a).ToArray();
            dataArray = dataList.SelectMany(a => a).ToArray();
            packedList.Add(header);
            packedList.Add(pointerArray);
            packedList.Add(scriptArray);
            packedList.Add(dataArray);
            packedArray = packedList.SelectMany(a => a).ToArray();

            packdir = MainWindow.UserFolder + "/Packed/";
            Directory.CreateDirectory(packdir);

            var bw = new BinaryWriter(File.Create(packdir + "SCRIPT.DAT"));
            bw.Write(packedArray);
            bw.Close();
        }
    }
}
