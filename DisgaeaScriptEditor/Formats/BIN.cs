using System;
using System.Linq;
using System.IO;
using System.Text;

namespace DisgaeaScriptEditor.Formats
{
    class BIN
    {
        static string filename;
        static string pardir;
        static string indent;

        static byte[] fileData;
        static int fileDataLength;
        static int pointer;

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
        static string parseString(params byte[] args) => Encoding.GetEncoding("shift_jis").GetString(fileData.Skip(pointer + 2).Take(opcodeLen - 1).ToArray());
        static string parseTime(params byte[] args) => Decimal.Divide(BitConverter.ToInt16(args, 0), 60).ToString("F");
        static string parseUnknown(params byte[] args) => BitConverter.ToString(args);

        static FileStream fs;
        static StreamWriter sw;

        public static void Parse()
        {
            fileData = MainWindow.WorkingFile;
            fileDataLength = fileData.Length;
            pointer = 0;
            indent = "\t";

            filename = System.IO.Path.GetFileNameWithoutExtension(MainWindow.UserFile);
            pardir = System.IO.Path.GetDirectoryName(MainWindow.UserFile) + "/Parsed/";

            Directory.CreateDirectory(pardir);
            File.Delete(pardir + filename + ".TXT");
            fs = new FileStream(pardir + filename + ".TXT", FileMode.Append);

            using (sw = new StreamWriter(fs))
            {
                sw.WriteLine("script(" + filename + ")\n{");

                while (pointer < fileDataLength)
                {
                    opcode = fileData[pointer];
                    opcodeLen = fileData[pointer + 1];
                    argCount = opcodeLen;

                    int nexptr = pointer + 2 + opcodeLen;

                    switch (opcode)
                    {
                        case 0x01:
                            // Sleep
                            arg1 = parseTime(fileData.Skip(pointer + 2).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "sleep(time[" + arg1 + "]);");
                            break;

                        case 0x02:
                            // End Script
                            pointer = nexptr;
                            if (pointer != fileDataLength)
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
                            pointer = nexptr;
                            sw.WriteLine(indent + "control.lock();");
                            break;

                        case 0x06:
                            // Unlock Controls
                            pointer = nexptr;
                            sw.WriteLine(indent + "control.unlock();");
                            break;

                        case 0x07:
                            // If Statement
                            count = fileData[pointer + 3];
                            IfStatement(); indent = "\t\t";
                            break;

                        case 0x08:
                            // Set Operation
                            unk = fileData[pointer + 2].ToString();
                            arg1 = parseInt16(fileData.Skip(pointer + 3).Take(2).ToArray());
                            operand = parseInt16(fileData.Skip(pointer + 5).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 7).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "set.flag(" + arg1 + Operator() + arg2 + ");");
                            break;

                        case 0x09:
                            // EndIf Statement
                            pointer = nexptr;
                            indent = "\t";
                            sw.WriteLine(indent + "endif;");
                            break;

                        case 0x0A:
                            // Fade Screen
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = parseTime(fileData.Skip(pointer + 3).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "fadeout(" + arg1 + ", time[" + arg2 + "]);");
                            break;

                        case 0x12:
                            // Call Script
                            arg1 = Convert.ToString(fileData[pointer + 2] | (fileData[pointer + 3] << 8) | (fileData[pointer + 4] << 16)).PadLeft(8, '0');
                            pointer = nexptr;
                            sw.WriteLine(indent + "load.script(" + arg1 + ");");
                            break;

                        case 0x15:
                            // Load Map
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "load.map(" + arg1 + ");");
                            break;

                        case 0x29:
                            // Camera Zoom
                            arg1 = parseTime(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "camera.zoom(time[" + arg1 + "], " + arg2 + ");");
                            break;

                        case 0x2B:
                            // Camera Pitch
                            arg1 = parseTime(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "camera.pitch(time[" + arg1 + "], " + arg2 + ");");
                            break;

                        case 0x2C:
                            // Camera Pan
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 6).Take(2).ToArray());
                            arg4 = parseTime(fileData.Skip(pointer + 8).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "camera.pan(" + arg1 + ", " + arg2 + ", " + arg3 + ", time[" + arg4 + "]);");
                            break;

                        case 0x32:
                            // Print String
                            arg1 = parseString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "print(" + arg1 + ");");
                            break;

                        case 0x4F:
                            // Give Character
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "get.actor(class[" + arg1 + "], level[" + arg2 + "]);");
                            break;

                        case 0x50:
                            // Give HL
                            arg1 = parseInt32(fileData.Skip(pointer + 2).Take(4).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "get.money(" + arg1 + ");");
                            break;

                        case 0x51:
                            // Give Item
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "get.item(" + arg1 + ", " + arg2 + ");");
                            break;

                        case 0x53:
                            // Input Control
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "control.input(" + arg1 + ");");
                            break;

                        case 0x5A:
                            // Game State
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = fileData[pointer + 3].ToString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "set.state(" + arg1 + ", " + arg2 + ");");
                            break;

                        case 0x66:
                            // Set Sprite
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = fileData[pointer + 3].ToString();
                            arg3 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "set.sprite(actor[" + arg1 + "], " + arg2 + ", " + arg3 + ");");
                            break;

                        case 0x6B:
                            // Set Animation
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = parseInt16(fileData.Skip(pointer + 3).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "set.anim(actor[" + arg1 + "], " + arg2 + ");");
                            break;

                        case 0x73:
                            // Set Rotation
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = parseTime(fileData.Skip(pointer + 3).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 5).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "set.rotation(actor[" + arg1 + "], time[" + arg2 + "], " + arg3 + ");");
                            break;

                        case 0x98:
                            // Set Talk ID
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = fileData[pointer + 3].ToString();
                            arg3 = Convert.ToString(fileData[pointer + 4] | (fileData[pointer + 5] << 8) | (fileData[pointer + 6] << 16));
                            pointer = nexptr;
                            sw.WriteLine(indent + "set.talk(actor[" + arg1 + "], " + arg2 + ", " + arg3 + ");");
                            break;

                        case 0xC9:
                            // Show UI
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "load.menu(" + arg1 + ");");
                            break;

                        case 0xD0:
                            // Unknown Opcode 0xD0
                            unk = opcode.ToString("X2");
                            arg1 = parseUnknown(fileData.Skip(pointer + 1).Take(4).ToArray());
                            pointer = pointer + 5;
                            sw.WriteLine(indent + "unknown" + "(op[" + unk + "], args[" + "4" + "], " + arg1 + ");");
                            break;

                        case 0xDD:
                            // Print String for DS Version (Top Screen)
                            arg1 = parseString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "print.ds(" + arg1 + ");");
                            break;

                        default:
                            // Unknown Opcode
                            unk = opcode.ToString("X2");
                            arg1 = parseUnknown(fileData.Skip(pointer + 2).Take(opcodeLen).ToArray());
                            pointer = nexptr;
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
                type = fileData[pointer + 4];
                if (type != 0)
                {
                    type1 = fileData[pointer + 4].ToString();
                    arg1 = parseUInt16(fileData.Skip(pointer + 5).Take(2).ToArray());
                    operand = parseInt16(fileData.Skip(pointer + 7).Take(2).ToArray());
                    arg2 = parseInt16(fileData.Skip(pointer + 9).Take(2).ToArray());
                    pointer = pointer + 4 + 7;
                    sw.WriteLine(indent + "if (flag[" + arg1 + "]" + Operator() + arg2 + ") then");
                }
                else
                {
                    type1 = fileData[pointer + 4].ToString();
                    arg1 = parseInt16(fileData.Skip(pointer + 5).Take(2).ToArray());
                    operand = fileData[pointer + 7].ToString();
                    unk = fileData[pointer + 8].ToString();
                    arg2 = parseUInt16(fileData.Skip(pointer + 9).Take(2).ToArray());
                    pointer = pointer + 4 + 7;
                    sw.WriteLine(indent + "if (" + arg1 + Operator() + "flag[" + arg2 + "]) then");
                }
            }
            else
            {
                type1 = fileData[pointer + 4].ToString();
                arg1 = parseUInt16(fileData.Skip(pointer + 5).Take(2).ToArray());
                operand = parseInt16(fileData.Skip(pointer + 7).Take(2).ToArray()); ops1 = Operator();
                arg2 = parseInt16(fileData.Skip(pointer + 9).Take(2).ToArray());
                operand = fileData[pointer + 11].ToString(); ops2 = Operator();
                type2 = fileData[pointer + 12].ToString();
                arg3 = parseUInt16(fileData.Skip(pointer + 13).Take(2).ToArray());
                operand = parseInt16(fileData.Skip(pointer + 15).Take(2).ToArray()); ops3 = Operator();
                arg4 = parseInt16(fileData.Skip(pointer + 17).Take(2).ToArray());
                pointer = pointer + 4 + 15;
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
