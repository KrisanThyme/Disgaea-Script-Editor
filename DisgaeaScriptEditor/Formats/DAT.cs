using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace DisgaeaScriptEditor.Formats
{
    class DAT
    {
        static byte[] scrdat;
        static int scrlen;
        static int count;
        static int size;
        static int tablesize;
        static int datsize;
        static int pointerval;
        static int pointoff;
        static int idoff;
        static int dataoff;
        static int filesize;
        static int datlen;
        static int scrid;
        static int scroff;
        static int scrnex;

        static byte[] pointarray;
        static byte[] idarray;
        static byte[] datarray;
        static byte[] packarray;
        static byte[] header = new byte[4];
        static byte[] pointer = new byte[4];
        static byte[] curoff = new byte[4];
        static byte[] nextoff = new byte[4];
        static byte[] curid = new byte[4];
        static byte[] curfile = new byte[0];

        static List<byte[]> idarraylist = new List<byte[]>();
        static List<byte[]> pointarraylist = new List<byte[]>();
        static List<byte[]> datarraylist = new List<byte[]>();
        static List<byte[]> packarraylist = new List<byte[]>();

        static string filename;
        static string exdir;
        static string packdir;

        static BinaryReader br;

        public static void Unpack()
        {
            using (br = new BinaryReader(File.OpenRead(MainWindow.UserFile)))
            {
                scrdat = File.ReadAllBytes(MainWindow.UserFile);
                scrlen = scrdat.Length;
                count = BitConverter.ToInt32(scrdat.Skip(0).Take(4).ToArray(), 0);
                tablesize = 4 * count;
                datsize = scrlen - (tablesize * 2) - 4;
                pointoff = 4;
                idoff = 4 * (count + 1);
                dataoff = 4 * (count * 2 + 1);
                pointarray = new byte[tablesize];
                idarray = new byte[tablesize];
                datarray = new byte[datsize];

                Array.Copy(scrdat, pointoff, pointarray, 0, tablesize); 
                Array.Copy(scrdat, idoff, idarray, 0, tablesize);
                Array.Copy(scrdat, dataoff, datarray, 0, datsize);

                exdir = System.IO.Path.GetDirectoryName(MainWindow.UserFile) + "/Extracted/";
                Directory.CreateDirectory(exdir);

                for (int i = 0; i < count; i++)
                {
                    Array.Copy(scrdat, pointoff + (4 * i), curoff, 0, 4);
                    Array.Copy(scrdat, idoff + (4 * i), curid, 0, 4);

                    datlen = datarray.Length;
                    scroff = BitConverter.ToInt32(curoff, 0);
                    scrid = BitConverter.ToInt32(curid, 0);
                    filename = Convert.ToString(scrid).PadLeft(8, '0');

                    if (i != (count - 1))
                    {
                        Array.Copy(pointarray, pointoff + (4 * i), nextoff, 0, 4);

                        scrnex = BitConverter.ToInt32(nextoff, 0);
                        filesize = scrnex - scroff;
                        curfile = new byte[filesize];

                        Array.Copy(datarray, scroff, curfile, 0, filesize);

                        var bw = new BinaryWriter(File.Create(exdir + filename + ".BIN"));
                        bw.Write(curfile);
                        bw.Close();
                    }
                    else
                    {
                        filesize = datlen - scroff;

                        if (filesize == 786)
                        {
                            if (BitConverter.ToInt32(datarray.Skip(scroff + 773).Take(13).ToArray(), 0) == 0)
                            {
                                filesize = 773;
                            }
                        }

                        curfile = new byte[filesize];
                        Array.Copy(datarray, scroff, curfile, 0, filesize);

                        var bw = new BinaryWriter(File.Create(exdir + filename + ".BIN"));
                        bw.Write(curfile);
                        bw.Close();
                    }
                }
                br.Close();
            }
        }

        public static void Pack()
        {
            size = 0;
            count = Directory.EnumerateFiles(MainWindow.UserFolder, "*.bin").Count();
            header = BitConverter.GetBytes(count);
            pointer = BitConverter.GetBytes(size);
            pointarraylist.Add(pointer);

            foreach (string scrfile in Directory.EnumerateFiles(MainWindow.UserFolder, "*.bin"))
            {
                filename = System.IO.Path.GetFileNameWithoutExtension(scrfile);
                scrid = Int32.Parse(filename);
                curid = BitConverter.GetBytes(scrid);
                idarraylist.Add(curid);

                using (br = new BinaryReader(File.OpenRead(scrfile)))
                {
                    scrdat = File.ReadAllBytes(scrfile);
                    datlen = scrdat.Length;
                    pointerval = size + datlen;
                    pointer = BitConverter.GetBytes(pointerval);
                    size = pointerval;
                    pointarraylist.Add(pointer);
                    datarraylist.Add(scrdat);

                    br.Close();
                }
            }

            pointarraylist.Remove(pointer);
            pointarray = pointarraylist.SelectMany(a => a).ToArray();
            idarray = idarraylist.SelectMany(a => a).ToArray();
            datarray = datarraylist.SelectMany(a => a).ToArray();
            packarraylist.Add(header);
            packarraylist.Add(pointarray);
            packarraylist.Add(idarray);
            packarraylist.Add(datarray);
            packarray = packarraylist.SelectMany(a => a).ToArray();

            packdir = MainWindow.UserFolder + "/Packed/";
            Directory.CreateDirectory(packdir);

            var bw = new BinaryWriter(File.Create(packdir + "SCRIPT.DAT"));
            bw.Write(packarray);
            bw.Close();
        }
    }
}
