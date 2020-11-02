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
                            sw.WriteLine(indent + "sleep(timer = " + arg1 + ");");
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
                            sw.WriteLine(indent + "system.fade(intensity = " + arg1 + ", timer = " + arg2 + ");");
                            break;

                        case 0x0B:
                            // Wait for Input
                            arg1 = parseTime(fileData.Skip(pointer + 2).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "input.wait(timer = " + arg2 + ");");
                            break;

                        case 0x0C:
                            // Shake Screen
                            arg1 = parseTime(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseTime(fileData.Skip(pointer + 4).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "system.shake(intensity = " + arg1 + ", timer = " + arg2 + ");");
                            break;

                     /* case 0x0D:
                               // Unknown Opcode. Not used in any retail script.
                               break; */

                        case 0x0E:
                            // Set Fade Color
                            arg1 = Convert.ToInt32(fileData[pointer + 4] | (fileData[pointer + 3] << 8) | (fileData[pointer + 2] << 16)).ToString("X2").PadLeft(6, '0');
                            pointer = nexptr;
                            sw.WriteLine(indent + "system.fadecolor(" + arg1 + ");");
                            break;

                     /* case 0x0F, 0x10:
                               // Unknown Opcode. Not used in any retail script.
                               break; */

                     /* case 0x11:
                               // NEEDS PARSING
                               break; */

                        case 0x12:
                            // Call Script
                            arg1 = Convert.ToString(fileData[pointer + 2] | (fileData[pointer + 3] << 8) | (fileData[pointer + 4] << 16)).PadLeft(8, '0');
                            pointer = nexptr;
                            sw.WriteLine(indent + "load.script(" + arg1 + ");");
                            break;

                     /* case 0x13, 0x14:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0x15:
                            // Load Map
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "load.map(" + arg1 + ");");
                            break;

                        case 0x16:
                            // Load Item World Map
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "load.itemworld(" + arg1 + ");");
                            break;

                     /* case 0x17:
                            // NEEDS PARSING
                            break; */

                        case 0x18:
                            // Reset Camera and Player Position
                            pointer = nexptr;
                            sw.WriteLine(indent + "system.resetmap();");
                            break;

                     /* case 0x19, 0x1A:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0x1B:
                            // Set Background Gradient
                            arg1 = parseTime(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = Convert.ToInt32(fileData[pointer + 6] | (fileData[pointer + 5] << 8) | (fileData[pointer + 4] << 16)).ToString("X2").PadLeft(6, '0');
                            arg3 = Convert.ToInt32(fileData[pointer + 9] | (fileData[pointer + 8] << 8) | (fileData[pointer + 7] << 16)).ToString("X2").PadLeft(6, '0');
                            arg4 = Convert.ToInt32(fileData[pointer + 12] | (fileData[pointer + 11] << 8) | (fileData[pointer + 10] << 16)).ToString("X2").PadLeft(6, '0');
                            arg5 = Convert.ToInt32(fileData[pointer + 15] | (fileData[pointer + 14] << 8) | (fileData[pointer + 13] << 16)).ToString("X2").PadLeft(6, '0');
                            pointer = nexptr;
                            sw.WriteLine(indent + "set.gradient(topLeft = " + arg2 + ", topRight = " + arg3 + ", bottomLeft = " + arg4 + ", bottomRight = " + arg5 + ", timer = " + arg1 + ");");
                            break;

                     /* case 0x1C:
                            // NEEDS PARSING
                            break; */

                     /* case 0x1D, 0x1E, 0x1F, 0x20, 0x21, 0x22:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                     /* case 0x23, 0x24:
                            // NEEDS PARSING
                            break; */

                     /* case 0x25, 0x26:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0x27:
                            // Move Cursor
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 6).Take(2).ToArray());
                            arg4 = parseTime(fileData.Skip(pointer + 8).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "cursor.pos(" + arg1 + ", x = " + arg2 + ", y = " + arg3 + ", time = " + arg4 + ");");
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
                            sw.WriteLine(indent + "camera.zoom(" + arg2 + ", timer = " + arg1 + ");");
                            break;

                     /* case 0x2A:
                            // NEEDS PARSING
                            break; */

                        case 0x2B:
                            // Camera Pitch
                            arg1 = parseTime(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "camera.pitch(" + arg2 + ", timer = " + arg1 + ");");
                            break;

                        case 0x2C:
                            // Camera Pan
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 6).Take(2).ToArray());
                            arg4 = parseTime(fileData.Skip(pointer + 8).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "camera.pan(" + arg1 + ", x = " + arg2 + ", y = " + arg3 + ", timer = " + arg4 + ");");
                            break;

                     /* case 0x2D, 0x2E:
                            // NEEDS PARSING
                            break; */

                     /* case 0x2F, 0x30, 0x31:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0x32:
                            // Print String
                            arg1 = parseString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "print(\"" + arg1 + "\");");
                            break;

                     /* case 0x33:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                     case 0x34:
                            // Wait for Input (Indefinitely)
                            pointer = nexptr;
                            sw.WriteLine(indent + "system.wait();");
                            break;

                     /* case 0x35, 0x36:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                     /* case 0x37, 0x38:
                            // NEEDS PARSING
                            break; */

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

                     /* case 0x3B, 0x3C, 0x3D, 0x3E, 0x3F, 0x40, 0x41, 0x42, 0x43, 0x44, 0x45:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                     /* case 0x46:
                            // NEEDS PARSING
                            break; */

                     /* case 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0x4E:
                            // Spawn Actor
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = parseInt16(fileData.Skip(pointer + 3).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "spawn.actor(entity = " + arg1 + ", class = " + arg2 + ");");
                            break;

                        case 0x4F:
                            // Give Character
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "get.actor(class = " + arg1 + ", level = " + arg2 + ");");
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

                     /* case 0x52:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0x53:
                            // Input Control
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "input(" + arg1 + ");");
                            break;

                     /* case 0x54, 0x55:
                            // NEEDS PARSING
                            break; */

                     /* case 0x56, 0x57, 0x58, 0x59:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0x5A:
                            // Game State
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = fileData[pointer + 3].ToString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "set.state(" + arg1 + ", " + arg2 + ");");
                            break;

                     /* case 0x5B:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                     /* case 0x5C:
                            // NEEDS PARSING
                            break; */

                     /* case 0x5D, 0x5E, 0x5F, 0x60:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                     /* case 0x61:
                            // NEEDS PARSING
                            break; */

                     /* case 0x62, 0x63:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                     /* case 0x64:
                            // NEEDS PARSING
                            break; */

                        case 0x65:
                            // Set Position
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 6).Take(2).ToArray());
                            arg4 = parseInt16(fileData.Skip(pointer + 8).Take(2).ToArray());
                            arg5 = parseInt16(fileData.Skip(pointer + 10).Take(2).ToArray());
                            arg6 = parseTime(fileData.Skip(pointer + 12).Take(2).ToArray());
                            arg7 = fileData[pointer + 14].ToString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "set.pos(entity = " + arg1 + ", " + arg2 + ", x = " + arg3 + ", y = " + arg5 + ", z = " + arg4 + ", time = " + arg6 + ", " + arg7 + ");");
                            break;

                        case 0x66:
                            // Set Sprite
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "set.sprite(entity = " + arg1 + ", class = " + arg2 + ");");
                            break;

                        case 0x67:
                            // Set Animation
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = parseInt32(fileData.Skip(pointer + 3).Take(4).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "set.anim(entity = " + arg1 + ", " + arg2 + ");");
                            break;

                     /* case 0x68, 0x69, 0x6A, 0x6B:
                            // NEEDS PARSING
                            break; */

                     /* case 0x6C:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0x6D:
                            // Spawn NPC
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = parseInt16(fileData.Skip(pointer + 3).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 5).Take(2).ToArray());
                            arg4 = fileData[pointer + 7].ToString(); team = arg4;
                            pointer = nexptr;
                            sw.WriteLine(indent + "spawn.npc(entity = " + arg1 + ", class = " + arg2 + ", level = " + arg3 + ", role = " + arg4 + ");");
                            break;

                        case 0x6E:
                            // Spawn Prop
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "spawn.prop(entity = " + arg1 + ", class = "+ arg2 + ");");
                            break;

                     /* case 0x6F, 0x70, 0x71, 0x72:
                            // NEEDS PARSING
                            break; */

                        case 0x73:
                            // Set Rotation
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = parseTime(fileData.Skip(pointer + 3).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 5).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "set.rot(entity = " + arg1 + ", rotate = " + arg3 + ", timer = " + arg2 + ");");
                            break;

                     /* case 0x74:
                            // NEEDS PARSING
                            break; */

                     /* case 0x75, 0x76:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                     /* case 0x77, 0x78, 0x79:
                            // NEEDS PARSING
                            break; */

                        case 0x7A:
                            // Set Color \ Alpha
                            // Yes the Actor, Color, and Alpha values are Int16 each.. I don't know why either.
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 6).Take(2).ToArray());
                            arg4 = parseInt16(fileData.Skip(pointer + 8).Take(2).ToArray());
                            arg5 = parseInt16(fileData.Skip(pointer + 10).Take(2).ToArray());
                            arg6 = parseTime(fileData.Skip(pointer + 12).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "set.filter(entity = " + arg1 + ", red = " + arg2 + ", blue = " + arg3 + ", green = " + arg4 + ", alpha = " + arg5 + ", timer = " + arg6 + ");");
                            break;

                     /* case 0x7B:
                            // NEEDS PARSING
                            break; */

                     /* case 0x7C, 0x7D, 0x7E, 0x7F, 0x80, 0x81:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                     /* case 0x82:
                            // NEEDS PARSING
                            break; */

                        case 0x83:
                            // Play Voice Over
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = fileData[pointer + 4].ToString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "sound.voice(" + arg1 + ", volume = " + arg2 + ");");
                            break;

                     /* case 0x84, 0x85, 0x86:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                     /* case 0x87:
                            // NEEDS PARSING
                            break; */

                        case 0x88:
                            // Play Battle Cry
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = fileData[pointer + 4].ToString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "sound.battle(" + arg1 + ", volume = " + arg2 + ");");
                            break;

                     /* case 0x89, 0x8A, 0x8B:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0x8C:
                            // Play SFX
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = fileData[pointer + 4].ToString();
                            arg3 = parseInt16(fileData.Skip(pointer + 5).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "sound.sfx(" + arg1 + ", " + arg2 + ", volume = " + arg3 + ");");
                            break;

                     /* case 0x8D:
                            // NEEDS PARSING
                            break; */

                     /* case 0x8E, 0x8F, 0x90:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0x91:
                            // Play BGM
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "sound.bgm(" + arg1 + ");");
                            break;

                        case 0x92:
                            // Adjust BGM Volume
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = parseTime(fileData.Skip(pointer + 2).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "sound.bgmvol(volume = " + arg1 + ", timer = " + arg2 + ");");
                            break;

                     /* case 0x93, 0x94, 0x95:
                            // NEEDS PARSING
                            break; */

                     /* case 0x96, 0x97:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0x98:
                            // Set Talk ID
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = Convert.ToString(fileData[pointer + 4] | (fileData[pointer + 5] << 8) | (fileData[pointer + 6] << 16));
                            pointer = nexptr;
                            sw.WriteLine(indent + "set.talk(entity = " + arg1 + ", dialog = " + arg3 + ";");
                            break;

                     /* case 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
                             0xB0, 0xB1, 0xB2, 0xB3, 0xB4, 0xB5, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0xC8:
                            // Hardcoded Handler
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = fileData[pointer + 3].ToString();
                            arg3 = fileData[pointer + 4].ToString();
                            arg4 = fileData[pointer + 5].ToString();
                            arg5 = fileData[pointer + 6].ToString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "system(" + arg1 + ", " + arg2 + ", " + arg3 + ", " + arg4 + ", " + arg5 + ");");
                            break;

                        case 0xC9:
                            // Show UI
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "load.menu(" + arg1 + ");");
                            break;

                     /* case 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF:
                            // NEEDS PARSING
                            break; */

                        case 0xD0:
                            // Unknown Opcode 0xD0
                            unk = opcode.ToString("X2");
                            arg1 = parseUnknown(fileData.Skip(pointer + 1).Take(4).ToArray());
                            pointer = pointer + 5;
                            sw.WriteLine(indent + "unknown" + "(opcode = " + unk + ", length = " + "4" + ", args[" + arg1 + "]);");
                            break;

                     /* case 0xD1:
                            // NEEDS PARSING
                            break; */

                     /* case 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0xDD:
                            // Print String for DS Version (Top Screen)
                            arg1 = parseString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "print.ds(\"" + arg1 + "\");");
                            break;

                     /* case 0xDE, 0xDF, 0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        default:
                            // Unknown Opcode
                            unk = opcode.ToString("X2");
                            arg1 = parseUnknown(fileData.Skip(pointer + 2).Take(opcodeLen).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "unknown" + "(opcode = " + unk + ", length = " + argCount + ", args[" + arg1 + "]);");
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
