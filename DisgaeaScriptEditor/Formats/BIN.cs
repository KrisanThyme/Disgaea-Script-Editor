using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

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

        static byte count;
        static byte type;
        static byte opcode;
        static byte opcodeLen;
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

        static string parseInt16(params byte[] args) => Convert.ToString(BitConverter.ToInt16(args, 0));
        static string parseUInt16(params byte[] args) => Convert.ToString(BitConverter.ToUInt16(args, 0));
        static string parseInt32(params byte[] args) => Convert.ToString(BitConverter.ToInt32(args, 0));
        static string parseString(params byte[] args) => Encoding.GetEncoding("shift_jis").GetString(scrdat.Skip(scrptr + 2).Take(opcodeLen - 1).ToArray());
        static string parseTime(params byte[] args) => Decimal.Divide(BitConverter.ToInt16(args, 0), 60).ToString("F");
        static string parseUnknown(params byte[] args) => BitConverter.ToString(args);

        static FileStream fs;
        static StreamWriter sw;

        public static void Parse()
        {
            scrdat = MainWindow.WorkingFile;
            scrlen = scrdat.Length;
            scrptr = 0;
            indent = "\t";

            filename = System.IO.Path.GetFileNameWithoutExtension(MainWindow.UserFile);
            pardir = System.IO.Path.GetDirectoryName(MainWindow.UserFile) + "/Parsed/";

            Directory.CreateDirectory(pardir);
            File.Delete(pardir + filename + ".TXT");
            fs = new FileStream(pardir + filename + ".TXT", FileMode.Append);

            using (sw = new StreamWriter(fs))
            {
                sw.WriteLine("script(" + filename + ")\n{");

                while (scrptr < scrlen)
                {
                    opcode = scrdat[scrptr];
                    opcodeLen = scrdat[scrptr + 1];
                    argCount = opcodeLen;

                    int nexptr = scrptr + 2 + opcodeLen;

                    switch (opcode)
                    {
                        case 0x01:
                            // Sleep
                            arg1 = parseTime(scrdat.Skip(scrptr + 2).Take(2).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(indent + "sleep(time[" + arg1 + "]);");
                            break;

                        case 0x02:
                            // End Script
                            scrptr = nexptr;
                            if (scrptr != scrlen)
                            {
                                sw.WriteLine(indent + "return;");
                            }
                            else
                            {
                                sw.WriteLine(indent + "return;" + "\n}");
                            }
                            break;

                        case 0x05:
                            // Lock Controls
                            scrptr = nexptr;
                            sw.WriteLine(indent + "control.lock();");
                            break;

                        case 0x06:
                            // Unlock Controls
                            scrptr = nexptr;
                            sw.WriteLine(indent + "control.unlock();");
                            break;

                        case 0x07:
                            // If Statement
                            count = scrdat[scrptr + 3];
                            IfStatement(); indent = "\t\t";
                            break;

                        case 0x08:
                            // Set Operation
                            unk = scrdat[scrptr + 2].ToString();
                            arg1 = parseInt16(scrdat.Skip(scrptr + 3).Take(2).ToArray());
                            operand = parseInt16(scrdat.Skip(scrptr + 5).Take(2).ToArray());
                            arg2 = parseInt16(scrdat.Skip(scrptr + 7).Take(2).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(indent + "set.flag(" + arg1 + Operator() + arg2 + ");");
                            break;

                        case 0x09:
                            // EndIf Statement
                            scrptr = nexptr;
                            indent = "\t";
                            sw.WriteLine(indent + "endif;");
                            break;

                        case 0x0A:
                            // Fade Screen
                            arg1 = scrdat[scrptr + 2].ToString();
                            arg2 = parseTime(scrdat.Skip(scrptr + 3).Take(2).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(indent + "fadeout(" + arg1 + ", time[" + arg2 + "]);");
                            break;

                        case 0x12:
                            // Call Script
                            arg1 = Convert.ToString(scrdat[scrptr + 2] | (scrdat[scrptr + 3] << 8) | (scrdat[scrptr + 4] << 16)).PadLeft(8, '0');
                            scrptr = nexptr;
                            sw.WriteLine(indent + "load.script(" + arg1 + ");");
                            break;

                        case 0x15:
                            // Load Map
                            arg1 = parseInt16(scrdat.Skip(scrptr + 2).Take(2).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(indent + "load.map(" + arg1 + ");");
                            break;

                        case 0x29:
                            // Camera Zoom
                            arg1 = parseTime(scrdat.Skip(scrptr + 2).Take(2).ToArray());
                            arg2 = parseInt16(scrdat.Skip(scrptr + 4).Take(2).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(indent + "camera.zoom(time[" + arg1 + "], " + arg2 + ");");
                            break;

                        case 0x2B:
                            // Camera Pitch
                            arg1 = parseTime(scrdat.Skip(scrptr + 2).Take(2).ToArray());
                            arg2 = parseInt16(scrdat.Skip(scrptr + 4).Take(2).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(indent + "camera.pitch(time[" + arg1 + "], " + arg2 + ");");
                            break;

                        case 0x32:
                            // Print String
                            arg1 = parseString();
                            scrptr = nexptr;
                            sw.WriteLine(indent + "print(" + arg1 + ");");
                            break;

                        case 0x4F:
                            // Give Character
                            arg1 = parseInt16(scrdat.Skip(scrptr + 2).Take(2).ToArray());
                            arg2 = parseInt16(scrdat.Skip(scrptr + 4).Take(2).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(indent + "get.actor(class[" + arg1 + "], level[" + arg2 + "]);");
                            break;

                        case 0x50:
                            // Give HL
                            arg1 = parseInt32(scrdat.Skip(scrptr + 2).Take(4).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(indent + "get.money(" + arg1 + ");");
                            break;

                        case 0x51:
                            // Give Item
                            arg1 = parseInt16(scrdat.Skip(scrptr + 2).Take(2).ToArray());
                            arg2 = parseInt16(scrdat.Skip(scrptr + 4).Take(2).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(indent + "get.item(" + arg1 + ", " + arg2 + ");");
                            break;

                        case 0x53:
                            // Input Control
                            arg1 = parseInt16(scrdat.Skip(scrptr + 2).Take(2).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(indent + "control.input(" + arg1 + ");");
                            break;

                        case 0x5A:
                            // Game State
                            arg1 = scrdat[scrptr + 2].ToString();
                            arg2 = scrdat[scrptr + 3].ToString();
                            scrptr = nexptr;
                            sw.WriteLine(indent + "set.state(" + arg1 + ", " + arg2 + ");");
                            break;

                        case 0x98:
                            // Set Talk ID
                            arg1 = parseInt16(scrdat.Skip(scrptr + 2).Take(2).ToArray());
                            arg2 = Convert.ToString(scrdat[scrptr + 4] | (scrdat[scrptr + 5] << 8) | (scrdat[scrptr + 6] << 16));
                            scrptr = nexptr;
                            sw.WriteLine(indent + "set.talk(actor[" + arg1 + "], " + arg2 + ");");
                            break;

                        case 0xC9:
                            // Show UI
                            arg1 = parseInt16(scrdat.Skip(scrptr + 2).Take(2).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(indent + "load.menu(" + arg1 + ");");
                            break;

                        case 0xD0:
                            // Unknown Opcode 0xD0
                            unk = opcode.ToString("X2");
                            arg1 = parseUnknown(scrdat.Skip(scrptr + 1).Take(4).ToArray());
                            scrptr = scrptr + 5;
                            sw.WriteLine(indent + "unknown" + "(op[" + unk + "], args[" + "4" + "], " + arg1 + ");");
                            break;

                        case 0xDD:
                            // Print String for DS Version (Top Screen)
                            arg1 = parseString();
                            scrptr = nexptr;
                            sw.WriteLine(indent + "print.ds(" + arg1 + ");");
                            break;

                        default:
                            // Unknown Opcode
                            unk = opcode.ToString("X2");
                            arg1 = parseUnknown(scrdat.Skip(scrptr + 2).Take(opcodeLen).ToArray());
                            scrptr = nexptr;
                            sw.WriteLine(indent + "unknown" + "(op[" + unk + "], args[" + argCount + "], " + arg1 + ");");
                            break;
                    }
                }
            }
        }

        public static void IfStatement()
        {
            if (count == 1)
            {
                type = scrdat[scrptr + 4];
                if (type != 0)
                {
                    type1 = scrdat[scrptr + 4].ToString();
                    arg1 = parseUInt16(scrdat.Skip(scrptr + 5).Take(2).ToArray());
                    operand = parseInt16(scrdat.Skip(scrptr + 7).Take(2).ToArray());
                    arg2 = parseInt16(scrdat.Skip(scrptr + 9).Take(2).ToArray());
                    scrptr = scrptr + 4 + 7;
                    sw.WriteLine(indent + "if (flag[" + arg1 + "]" + Operator() + arg2 + ") then");
                }
                else
                {
                    type1 = scrdat[scrptr + 4].ToString();
                    arg1 = parseInt16(scrdat.Skip(scrptr + 5).Take(2).ToArray());
                    operand = scrdat[scrptr + 7].ToString();
                    unk = scrdat[scrptr + 8].ToString();
                    arg2 = parseUInt16(scrdat.Skip(scrptr + 9).Take(2).ToArray());
                    scrptr = scrptr + 4 + 7;
                    sw.WriteLine(indent + "if (" + arg1 + Operator() + "flag[" + arg2 + "]) then");
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
                sw.WriteLine(indent + "if (flag[" + arg1 + "]" + ops1 + arg2 + ops2 + "flag[" + arg3 + "]" + ops3 + arg4 + ") then");
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
