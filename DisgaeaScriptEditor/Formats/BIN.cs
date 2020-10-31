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

        static byte opcode;
        static byte opcodeLen;
        static byte argCount;
        static byte varCount;

        static string unk;
        static string arg0;
        static string arg1;
        static string arg2;
        static string arg3;
        static string arg4;
        static string arg5;
        static string arg6;
        static string arg7;
        static string op1;
        static string op2;
        static string op3;
        static string team;
        static string operand;
        static string variable;
        static string boolean;

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
                            sw.WriteLine(indent + "input.lock(true);");
                            break;

                        case 0x06:
                            // Unlock Controls
                            pointer = nexptr;
                            sw.WriteLine(indent + "input.lock(false);");
                            break;

                        case 0x07:
                            // If Statement
                            unk = fileData[pointer + 2].ToString();
                            varCount = fileData[pointer + 3];
                            IfStatement(); indent = "\t\t";
                            break;

                        case 0x08:
                            // Set Operation
                            unk = fileData[pointer + 2].ToString("X2");
                            arg1 = parseInt16(fileData.Skip(pointer + 3).Take(2).ToArray());
                            operand = parseInt16(fileData.Skip(pointer + 5).Take(2).ToArray()); arg2 = Operator();
                            arg3 = parseInt16(fileData.Skip(pointer + 7).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "set.flag(" + arg1 + arg2 + arg3 + ");");
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

                        case 0x27:
                            // Move Cursor
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 6).Take(2).ToArray());
                            arg4 = parseTime(fileData.Skip(pointer + 8).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "cursor.pos(" + arg1 + ", x[" + arg2 + "], y[" + arg3 + "], time[" + arg4 + "]);");
                            break;

                        case 0x28:
                            // Camera Lock
                            arg1 = fileData[pointer + 2].ToString(); boolean = arg1;
                            pointer = nexptr;
                            sw.WriteLine(indent + "camera.lock = " + Boolean() + ";");
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
                            sw.WriteLine(indent + "camera.pan(" + arg1 + ", x[" + arg2 + "], y[" + arg3 + "], time[" + arg4 + "]);");
                            break;

                        case 0x32:
                            // Print String
                            arg1 = parseString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "print(\"" + arg1 + "\");");
                            break;

                        case 0x39:
                            // Set Background
                            arg1 = fileData[pointer + 2].ToString();
                            arg1 = fileData[pointer + 3].ToString();
                            arg1 = fileData[pointer + 4].ToString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "set.bg(" + arg1 + ", " + arg2 + ", " + arg3 + ");");
                            break;

                        case 0x3A:
                            // Clear Background
                            arg1 = fileData[pointer + 2].ToString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "system.bg(" + arg1 + ");");
                            break;

                        case 0x4E:
                            // Spawn Actor
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = parseInt16(fileData.Skip(pointer + 3).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "spawn.actor(actor[" + arg1 + "], " + arg2 + ");");
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
                            sw.WriteLine(indent + "input(" + arg1 + ");");
                            break;

                        case 0x5A:
                            // Game State
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = fileData[pointer + 3].ToString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "set.state(" + arg1 + ", " + arg2 + ");");
                            break;

                        case 0x65:
                            // Set Position
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = parseInt16(fileData.Skip(pointer + 3).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 5).Take(2).ToArray());
                            arg4 = parseInt16(fileData.Skip(pointer + 7).Take(2).ToArray());
                            arg5 = parseInt16(fileData.Skip(pointer + 9).Take(2).ToArray());
                            arg6 = parseTime(fileData.Skip(pointer + 11).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "set.pos(actor[" + arg1 + "], " + arg2 + ", x[" + arg3 + "], z[" + arg4 + "], y[" + arg5 + "], time[" + arg6 + "]);");
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

                        case 0x6D:
                            // Spawn NPC
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = parseInt16(fileData.Skip(pointer + 3).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 5).Take(2).ToArray());
                            arg4 = fileData[pointer + 7].ToString(); team = arg4;
                            pointer = nexptr;
                            sw.WriteLine(indent + "spawn.npc(actor[" + arg1 + "], class[" + arg2 + "], level[" + arg3 + "], team[" + arg4 + "]);");
                            break;

                        case 0x6E:
                            // Spawn Prop
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = fileData[pointer + 3].ToString();
                            arg3 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "spawn.prop(actor[" + arg1 + "], " + arg2 + ", " + arg3 + ");");
                            break;

                        case 0x73:
                            // Set Rotation
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = parseTime(fileData.Skip(pointer + 3).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 5).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "set.rot(actor[" + arg1 + "], time[" + arg2 + "], " + arg3 + ");");
                            break;

                        case 0x7A:
                            // Set Color \ Alpha
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = fileData[pointer + 3].ToString();
                            arg3 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            arg4 = parseInt16(fileData.Skip(pointer + 6).Take(2).ToArray());
                            arg5 = parseInt16(fileData.Skip(pointer + 8).Take(2).ToArray());
                            arg6 = parseInt16(fileData.Skip(pointer + 10).Take(2).ToArray());
                            arg7 = parseInt16(fileData.Skip(pointer + 12).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "set.filter(actor[" + arg1 + "], " + arg2 + ", red[" + arg3 + "], blue[" + arg4 + "], green[" + arg5 + "], alpha[" + arg6 + "], " + arg7 + ");");
                            break;

                        case 0x8C:
                            // Play SFX
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = fileData[pointer + 4].ToString();
                            arg3 = parseInt16(fileData.Skip(pointer + 5).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "sound.sfx(id[" + arg1 + "], " + arg2 + ", " + arg3 + ");");
                            break;

                        case 0x91:
                            // Play BGM
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "sound.bgm(id[" + arg1 + "]);");
                            break;

                        case 0x98:
                            // Set Talk ID
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = fileData[pointer + 3].ToString();
                            arg3 = Convert.ToString(fileData[pointer + 4] | (fileData[pointer + 5] << 8) | (fileData[pointer + 6] << 16));
                            pointer = nexptr;
                            sw.WriteLine(indent + "set.talk(actor[" + arg1 + "], " + arg2 + ", " + arg3 + ");");
                            break;

                        case 0xC8:
                            // Hardcoded Handler
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = fileData[pointer + 3].ToString();
                            arg3 = fileData[pointer + 4].ToString();
                            arg4 = fileData[pointer + 5].ToString();
                            arg5 = fileData[pointer + 6].ToString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "system(function[" + arg1 + "], " + arg2 + ", " + arg3 + ", " + arg4 + ", " + arg5 + ");");
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
                            sw.WriteLine(indent + "print.ds(\"" + arg1 + "\");");
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
            if (varCount == 1)
            {
                variable = fileData[pointer + 4].ToString();
                arg1 = parseUInt16(fileData.Skip(pointer + 5).Take(2).ToArray()); arg0 = arg1; arg1 = Variable();
                operand = fileData[pointer + 7].ToString(); op1 = Operator();
                variable = fileData[pointer + 8].ToString();
                arg2 = parseUInt16(fileData.Skip(pointer + 9).Take(2).ToArray()); arg0 = arg2; arg2 = Variable();
                pointer = pointer + 4 + 7;
                sw.WriteLine(indent + "if (" + arg1 + op1 + arg2 + ") then");

            }
            else
            {
                variable = fileData[pointer + 4].ToString();
                arg1 = parseUInt16(fileData.Skip(pointer + 5).Take(2).ToArray()); arg0 = arg1; arg1 = Variable();
                operand = fileData[pointer + 7].ToString(); op1 = Operator();
                variable = fileData[pointer + 8].ToString();
                arg2 = parseUInt16(fileData.Skip(pointer + 9).Take(2).ToArray()); arg0 = arg2; arg2 = Variable();
                operand = fileData[pointer + 11].ToString(); op2 = Operator();
                variable = fileData[pointer + 12].ToString();
                arg3 = parseUInt16(fileData.Skip(pointer + 13).Take(2).ToArray()); arg0 = arg3; arg3 = Variable();
                operand = fileData[pointer + 15].ToString(); op3 = Operator();
                variable = fileData[pointer + 16].ToString();
                arg4 = parseUInt16(fileData.Skip(pointer + 17).Take(2).ToArray()); arg0 = arg4; arg4 = Variable();

                pointer = pointer + 4 + 15;
                sw.WriteLine(indent + "if (" + arg1 + op1 + arg2 + op2 + arg3 + op3 + arg4 + ") then");
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

        public static string Variable()
        {
            switch (variable)
            {
                case "0":
                    // Int16
                    return arg0;

                case "1":
                    // Flag
                    return "flag[" + arg0 + "]";

                default:
                    // Unknown Var Type
                    return "unknown";
            }
        }

        public static string Boolean()
        {
            switch (boolean)
            {
                case "0":
                    return "false";

                case "1":
                    return "true";

                default:
                    // Unknown Bool
                    return "error";
            }
        }

        public static string Team()
        {
            switch (team)
            {
                case "0":
                    // Player Controlled Characters
                    return "ally";

                case "1":
                    // Enemy Characters (Attacks Everyone)
                    return "enemy";

                case "2":
                    // Neutral Characters (Attacks Baddies)
                    return "neutral";

                case "3":
                    // Nemesis Characters (Attacks Player)
                    return "nemesis";

                default:
                    // Unknown Team
                    return "error";
            }
        }
    }
}
