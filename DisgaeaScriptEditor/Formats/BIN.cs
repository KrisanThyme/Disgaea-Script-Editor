using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace DisgaeaScriptEditor.Formats
{
    class BIN
    {
        static byte[] scrdat;
        static int scrlen;
        static int scrptr;

        static string filename;
        static string pardir;
        static string indent;
        static string prefix;

        static byte flagcount;
        static byte flagsop;
        static byte flag1type;
        static byte flag2type;
        static byte flagunk;
        static byte[] flag1 = new byte[4];
        static byte[] flag1val = new byte[2];
        static byte[] flag1op = new byte[2];
        static byte[] flag2 = new byte[4];
        static byte[] flag2val = new byte[2];
        static byte[] flag2op = new byte[2];
        static string flag1str;
        static string flag2str;
        static string flagstr;

        static string op;
        static byte opcode = 0;
        static byte opcodelen;

        static byte argCount;
        static byte argByte;
        static byte[] argInt16 = new byte[2];
        static byte[] argInt32 = new byte[4];
        static byte[] argUnk;
        static decimal arg1Dec;
        static decimal arg2Dec;
        static string arg1;
        static string arg2;

        static FileStream fs;
        static StreamWriter sw;

        public static void Parse()
        {
            scrdat = MainWindow.WorkingFile;
            scrlen = scrdat.Length;
            scrptr = 0;

            filename = System.IO.Path.GetFileNameWithoutExtension(MainWindow.UserFile);
            pardir = System.IO.Path.GetDirectoryName(MainWindow.UserFile) + "/Parsed/";
            indent = "\t";

            Directory.CreateDirectory(pardir);
            File.Delete(pardir + filename + ".TXT");
            fs = new FileStream(pardir + filename + ".TXT", FileMode.Append);

            using (sw = new StreamWriter(fs))
            {
                sw.WriteLine("ID:\t" + filename);

                while (scrptr < scrlen)
                {
                    opcode = scrdat[scrptr];
                    opcodelen = scrdat[scrptr + 1];
                    argCount = opcodelen;
                    prefix = opcode.ToString("X2") + ":" + indent;

                    int nexptr = scrptr + 2 + opcodelen;

                    switch (opcode)
                    {
                        case 0x01:
                            // Sleep
                            Array.Copy(scrdat, scrptr + 2, argInt16, 0, 2);
                            arg1Dec = Decimal.Divide(BitConverter.ToInt16(argInt16, 0), 60);
                            scrptr = nexptr;
                            sw.WriteLine(prefix + "sleep(" + arg1Dec.ToString("F") + "s)");
                            break;

                        case 0x02:
                            // End Script
                            scrptr = nexptr;
                            sw.WriteLine(prefix + "return");
                            break;

                        case 0x05:
                            // Lock Controls
                            scrptr = nexptr;
                            sw.WriteLine(prefix + "control.Lock");
                            break;

                        case 0x06:
                            // Unlock Controls
                            scrptr = nexptr;
                            sw.WriteLine(prefix + "control.Unlock");
                            break;

                        case 0x07:
                            // If Statement
                            flagcount = scrdat[scrptr + 3];
                            IfStatement();
                            indent = "\t\t";
                            break;

                        case 0x09:
                            // EndIf Statement
                            scrptr = nexptr;
                            indent = "\t";
                            prefix = opcode.ToString("X2") + ":" + indent;
                            sw.WriteLine(prefix + "endif");
                            break;

                        case 0x0A:
                            // Fade Screen
                            argByte = scrdat[scrptr + 2];
                            Array.Copy(scrdat, scrptr + 3, argInt16, 0, 2);
                            arg1Dec = Decimal.Divide(BitConverter.ToInt16(argInt16, 0), 60);
                            scrptr = nexptr;
                            sw.WriteLine(prefix + "fadeout(" + argByte + ", " + arg1Dec.ToString("F") + "s)");
                            break;

                        case 0x12:
                            // Call Script
                            Array.Copy(scrdat, scrptr + 2, argInt32, 0, opcodelen);
                            scrptr = nexptr;
                            sw.WriteLine(prefix + "script(" + BitConverter.ToInt32(argInt32, 0) + ")");
                            break;

                        case 0x29:
                            // Camera Zoom
                            Array.Copy(scrdat, scrptr + 2, argInt16, 0, 2);
                            arg1Dec = Decimal.Divide(BitConverter.ToInt16(argInt16, 0), 60);
                            Array.Copy(scrdat, scrptr + 4, argInt16, 0, 2);
                            arg2Dec = Decimal.Divide(BitConverter.ToInt16(argInt16, 0), 60);
                            scrptr = nexptr;
                            sw.WriteLine(prefix + "camera.Zoom(" + arg1Dec.ToString("F") + ", " + arg2Dec.ToString("F") + "s)");
                            break;

                        case 0xD0:
                            // Unknown Opcode 0xD0
                            argUnk = new byte[4];
                            Array.Copy(scrdat, scrptr + 2, argUnk, 0, 4);
                            scrptr = scrptr + 4 + 1;
                            sw.WriteLine(prefix + "unknown_opcode" + " - args[" + 4 + "]: " + BitConverter.ToString(argUnk));
                            break;

                        default:
                            // Unknown Opcode
                            argUnk = new byte[opcodelen];
                            Array.Copy(scrdat, scrptr + 2, argUnk, 0, opcodelen);
                            scrptr = nexptr;
                            sw.WriteLine(prefix + "unknown_opcode" + " - args[" + argCount + "]: " + BitConverter.ToString(argUnk));
                            break;
                    }
                }
            }
        }

        public static void IfStatement()
        {
            if (flagcount != 2)
            {
                flag1type = scrdat[scrptr + 4];
                if (flag1type != 0)
                {
                    Array.Copy(scrdat, scrptr + 5, flag1, 0, 2);
                    Array.Copy(scrdat, scrptr + 7, flag1op, 0, 2);
                    Array.Copy(scrdat, scrptr + 9, flag1val, 0, 2);
                    op = Convert.ToString(BitConverter.ToInt16(flag1op, 0));
                    flag1str = Operator();
                    scrptr = scrptr + 4 + 7;
                    sw.WriteLine(prefix + "if (Flag #" + BitConverter.ToInt32(flag1, 0) + flag1str + BitConverter.ToInt16(flag1val, 0) + ") then");
                }
                else
                {
                    Array.Copy(scrdat, scrptr + 5, flag1val, 0, 2);
                    Array.Copy(scrdat, scrptr + 7, flag1op, 0, 1);
                    Array.Copy(scrdat, scrptr + 9, flag1, 0, 2);
                    flagunk = scrdat[scrptr + 8];
                    op = Convert.ToString(BitConverter.ToInt16(flag1op, 0));
                    flag1str = Operator();
                    scrptr = scrptr + 4 + 7;
                    sw.WriteLine(prefix + "if (" + BitConverter.ToInt16(flag1val, 0) + flag1str + "Flag #" + BitConverter.ToInt32(flag1, 0) + ") then");
                }
            }
            else
            {
                flag1type = scrdat[scrptr + 4];
                Array.Copy(scrdat, scrptr + 5, flag1, 0, 2);
                Array.Copy(scrdat, scrptr + 7, flag1op, 0, 2);
                Array.Copy(scrdat, scrptr + 9, flag1val, 0, 2);
                flagsop = scrdat[scrptr + 11];
                flag2type = scrdat[scrptr + 12];
                Array.Copy(scrdat, scrptr + 13, flag2, 0, 2);
                Array.Copy(scrdat, scrptr + 15, flag2op, 0, 2);
                Array.Copy(scrdat, scrptr + 17, flag2val, 0, 2);
                op = Convert.ToString(BitConverter.ToInt16(flag1op, 0));
                flag1str = Operator();
                op = Convert.ToString(BitConverter.ToInt16(flag2op, 0));
                flag2str = Operator();
                op = Convert.ToString(flagsop);
                flagstr = Operator();
                scrptr = scrptr + 4 + 15;
                sw.WriteLine(prefix + "if (Flag #" + BitConverter.ToInt32(flag1, 0) + flag1str + BitConverter.ToInt16(flag1val, 0) + flagstr + "Flag #" + BitConverter.ToInt32(flag2, 0) + flag2str + BitConverter.ToInt16(flag2val, 0) + ") then");
            }
        }

        public static string Operator()
        {
            switch (op) 
            {
				case "10":
                    // Equality Operator: ==
                    return " == ";

				case "20":
                    // Conditional Logical OR Operator: ||
                    return " || ";

                case "21":
                    // Conditional Logical AND Operator: &&
                    return " && ";

                default:
                    // Unknown Operator: ??
					return " ?? ";
            }
        }
    }
}
