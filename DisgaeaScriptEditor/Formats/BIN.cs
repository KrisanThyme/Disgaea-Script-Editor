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
        static byte flag1type;
        static byte opcode;
        static byte opcodelen;
        static byte argCount;

        static string unk;
        static string arg1;
        static string arg2;
        static string arg3;
        static string arg4;
        static string type1;
        static string type2;
        static string ops1;
        static string ops2;
        static string ops3;
        static string operand;
        
        static string parseTime(params byte[] args) => Decimal.Divide(BitConverter.ToInt16(args, 0), 60).ToString("F");
        static string parseInt16(params byte[] args) => Convert.ToString(BitConverter.ToInt16(args, 0));
        static string parseUInt16(params byte[] args) => Convert.ToString(BitConverter.ToUInt16(args, 0));
        static string parseInt32(params byte[] args) => Convert.ToString(BitConverter.ToInt32(args, 0));
        static string parseUnknown(params byte[] args) => BitConverter.ToString(args);

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
                            arg1 = parseTime(scrdat.Skip(scrptr + 2).Take(2).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(prefix + "sleep(" + arg1 + "s)");
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
                            IfStatement(); indent = "\t\t";
                            break;

                        case 0x08:
                            // Set Operation
                            unk = scrdat[scrptr + 2].ToString();
                            arg1 = parseInt16(scrdat.Skip(scrptr + 3).Take(2).ToArray());
                            operand = parseInt16(scrdat.Skip(scrptr + 5).Take(2).ToArray());
                            arg2 = parseInt16(scrdat.Skip(scrptr + 7).Take(2).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(prefix + "setflag(" + arg1 + Operator() + arg2 + ")");
                            break;

                        case 0x09:
                            // EndIf Statement
                            scrptr = nexptr;
                            indent = "\t"; prefix = opcode.ToString("X2") + ":" + indent;
                            sw.WriteLine(prefix + "endif");
                            break;

                        case 0x0A:
                            // Fade Screen
                            arg1 = scrdat[scrptr + 2].ToString();
                            arg2 = parseTime(scrdat.Skip(scrptr + 3).Take(2).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(prefix + "fadeout(" + arg1 + ", " + arg2 + "s)");
                            break;

                        case 0x12:
                            // Call Script
                            arg1 = Convert.ToString(scrdat[scrptr + 2] | (scrdat[scrptr + 3] << 8) | (scrdat[scrptr + 4] << 16));
                            scrptr = nexptr;
                            sw.WriteLine(prefix + "script(" + arg1 + ")");
                            break;

                        case 0x15:
                            // Load Map
                            arg1 = parseInt16(scrdat.Skip(scrptr + 2).Take(2).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(prefix + "load.Map(" + arg1 + ")");
                            break;

                        case 0x29:
                            // Camera Zoom
                            arg1 = parseTime(scrdat.Skip(scrptr + 2).Take(2).ToArray());
                            arg2 = parseInt16(scrdat.Skip(scrptr + 4).Take(2).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(prefix + "camera.Zoom(" + arg1 + "s, " + arg2 + ")");
                            break;

                        case 0x2B:
                            // Camera Pitch
                            arg1 = parseTime(scrdat.Skip(scrptr + 2).Take(2).ToArray());
                            arg2 = parseInt16(scrdat.Skip(scrptr + 4).Take(2).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(prefix + "camera.Pitch(" + arg1 + "s, " + arg2 + ")");
                            break;

                        case 0x50:
                            // Give HL
                            arg1 = parseInt32(scrdat.Skip(scrptr + 2).Take(4).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(prefix + "get.HL(" + arg1 + ")");
                            break;

                        case 0x51:
                            // Give Item
                            arg1 = parseInt16(scrdat.Skip(scrptr + 2).Take(2).ToArray());
                            arg2 = parseInt16(scrdat.Skip(scrptr + 4).Take(2).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(prefix + "get.Item(" + arg1 + ", " + arg2 + ")");
                            break;

                        case 0x53:
                            // Input Control
                            arg1 = parseInt16(scrdat.Skip(scrptr + 2).Take(2).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(prefix + "control.Input(" + arg1 + ")");
                            break;

                        case 0xD0:
                            // Unknown Opcode 0xD0
                            arg1 = parseUnknown(scrdat.Skip(scrptr + 2).Take(4).ToArray());
                            scrptr = scrptr + 4 + 1;
                            sw.WriteLine(prefix + "unknown_opcode" + " - args[" + 4 + "]: " + arg1);
                            break;

                        default:
                            // Unknown Opcode
                            arg1 = parseUnknown(scrdat.Skip(scrptr + 2).Take(opcodelen).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(prefix + "unknown_opcode" + " - args[" + argCount + "]: " + arg1);
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
                    arg1 = parseUInt16(scrdat.Skip(scrptr + 5).Take(2).ToArray());
                    operand = parseInt16(scrdat.Skip(scrptr + 7).Take(2).ToArray());
                    arg2 = parseInt16(scrdat.Skip(scrptr + 9).Take(2).ToArray());
                    scrptr = scrptr + 4 + 7;
                    sw.WriteLine(prefix + "if (flag[" + arg1 + "]" + Operator() + arg2 + ") then");
                }
                else
                {
                    arg1 = parseInt16(scrdat.Skip(scrptr + 5).Take(2).ToArray());
                    operand = scrdat[scrptr + 7].ToString();
                    unk = scrdat[scrptr + 8].ToString();
                    arg2 = parseUInt16(scrdat.Skip(scrptr + 9).Take(2).ToArray());
                    scrptr = scrptr + 4 + 7;
                    sw.WriteLine(prefix + "if (" + arg1 + Operator() + "flag[" + arg2 + "]) then");
                }
            }
            else
            {
                type1 = scrdat[scrptr + 4].ToString();
                arg1 = parseUInt16(scrdat.Skip(scrptr + 5).Take(2).ToArray());
                operand = parseInt16(scrdat.Skip(scrptr + 7).Take(2).ToArray()); ops1 = Operator();
                arg2 = parseInt16(scrdat.Skip(scrptr + 9).Take(2).ToArray());
                operand = scrdat[scrptr + 11].ToString(); ops2 = Operator();
                type2 = scrdat[scrptr + 12].ToString();
                arg3 = parseUInt16(scrdat.Skip(scrptr + 13).Take(2).ToArray());
                operand = parseInt16(scrdat.Skip(scrptr + 15).Take(2).ToArray()); ops3 = Operator();
                arg4 = parseInt16(scrdat.Skip(scrptr + 17).Take(2).ToArray());
                scrptr = scrptr + 4 + 15;
                sw.WriteLine(prefix + "if (flag[" + arg1 + "]" + ops1 + arg2 + ops2 + "flag[" + arg3 + "]" + ops3 + arg4 + ") then");
            }
        }

        public static string Operator()
        {
            switch (operand) 
            {
                case "1":
                    // Assignment Operator
                    return " = ";

                case "10":
                    // Equality Operator
                    return " == ";

				case "20":
                    // Conditional Logical OR Operator
                    return " || ";

                case "21":
                    // Conditional Logical AND Operator
                    return " && ";

                default:
                    // Unknown Operator
					return " ?? ";
            }
        }
    }
}
