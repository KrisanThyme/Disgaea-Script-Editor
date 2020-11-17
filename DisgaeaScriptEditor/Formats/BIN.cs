using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

using formMBox = System.Windows.Forms.MessageBox;

namespace DisgaeaScriptEditor.Formats
{
    static class BIN
    {
        static string filename;
        static string pardir;
        static string scrdir;
        static string indent;
        static string script;

        static byte[] fileData;
        static int fileDataLength;
        static int pointer;
        static int ifLen;
        static int ifPointer;

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
        static string arg8;
        static string op1;
        static string op2;
        static string op3;
        static string operand;
        static string variable;
        static string boolean;

        static string curLine;
        static byte[] opArray;
        static byte[] ifArray;
        static byte[] newScript;
        static byte[] tempScript;
        static byte[] tempAddress;

        static List<byte[]> newScriptList = new List<byte[]>();
        static List<byte[]> tempAddressList = new List<byte[]>();

        static string parseString() => Encoding.GetEncoding("shift_jis").GetString(fileData.Skip(pointer + 2).Take(opcodeLen - 1).ToArray());
        static string parseInt16(byte[] args) => Convert.ToString(BitConverter.ToInt16(args, 0));
        static string parseUInt16(byte[] args) => Convert.ToString(BitConverter.ToUInt16(args, 0));
        static string parseInt32(byte[] args) => Convert.ToString(BitConverter.ToInt32(args, 0)); 
        static string parseTime(byte[] args) => Decimal.Divide(BitConverter.ToInt16(args, 0), 60).ToString("F");
        static string parseUnknown(byte[] args) => BitConverter.ToString(args);

        static bool stringContains(string args) => curLine.Contains(args + "(", StringComparison.OrdinalIgnoreCase);
        static bool stringContainsNoArgs(string args) => curLine.Contains(args, StringComparison.OrdinalIgnoreCase);

        static Array writeValue(int args) => BitConverter.GetBytes(args);
        static Array writeByte(string args) => BitConverter.GetBytes(Convert.ToByte(args));
        static Array writeHex(string args) => BitConverter.GetBytes(Convert.ToByte(args, 16));
        static Array writeInt16(string args) => BitConverter.GetBytes(Convert.ToInt16(args));
        static Array writeUInt16(string args) => BitConverter.GetBytes(Convert.ToUInt16(args));
        static Array writeInt32(string args) => BitConverter.GetBytes(Convert.ToInt32(args));
        static Array writeTime(string args) => BitConverter.GetBytes(Convert.ToInt16(Convert.ToDouble(args) * 60));

        static FileStream fs;
        static StreamWriter sw;
        static BinaryWriter bw;

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
                    int nexptr = 0;
                    opcode = fileData[pointer];

                    if (opcode != 0)
                    {
                        if (opcode != 7)
                        {
                            opcodeLen = fileData[pointer + 1];
                            argCount = opcodeLen;
                            nexptr = pointer + 2 + opcodeLen;
                        }
                        else
                        {
                            ifLen = BitConverter.ToUInt16(fileData.Skip(pointer + 1).Take(2).ToArray(), 0);
                            nexptr = pointer + 2 + ifLen;
                        }
                    }
                    else
                    {
                        opcodeLen = fileData[fileDataLength - pointer];
                        nexptr = pointer + 2 + opcodeLen;
                        sw.WriteLine(indent + "-Error Parsing Script-" + "\n}");
                    }

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

                     /* case 0x03, 0x04:
                           // NEEDS PARSING
                           // These seem to be unused in the original PS2 release, interestingly enough..
                           break; */

                        case 0x05:
                            // Lock Player Controls
                            pointer = nexptr;
                            sw.WriteLine(indent + "input.lock(true);");
                            break;

                        case 0x06:
                            // Unlock Player Controls
                            pointer = nexptr;
                            sw.WriteLine(indent + "input.lock(false);");
                            break;

                        case 0x07:
                            // If Statement
                            varCount = fileData[pointer + 3];
                            ParseIfStatement(); indent = indent + "\t";
                            break;

                        case 0x08:
                            // Set Operation
                            // The Unknown is always set to 0x01 in retail scripts.
                            unk = fileData[pointer + 2].ToString("X2");
                            arg1 = parseInt16(fileData.Skip(pointer + 3).Take(2).ToArray());
                            operand = parseInt16(fileData.Skip(pointer + 5).Take(2).ToArray()); arg2 = Operator();
                            arg3 = parseInt16(fileData.Skip(pointer + 7).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "system.flag(" + arg1 + arg2 + arg3 + ");");
                            break;

                        case 0x09:
                            // EndIf Statement
                            pointer = nexptr;
                            indent = indent.Substring(0, indent.Length - 1);
                            sw.WriteLine(indent + "endif;");
                            break;

                        case 0x0A:
                            // Fade Screen
                            // Setting the Intensity level to 0x00 will unfade the screen if it is currently faded out.
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = parseTime(fileData.Skip(pointer + 3).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "screenfade(intensity = " + arg1 + ", timer = " + arg2 + ");");
                            break;

                        case 0x0B:
                            // Wait for Input
                            // Pauses the game for the set duration unless Confirm or Cancel is pressed before time has elapsed.
                            arg1 = parseTime(fileData.Skip(pointer + 2).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "input.wait(timer = " + arg1 + ");");
                            break;

                        case 0x0C:
                            // Shake Screen
                            arg1 = parseTime(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseTime(fileData.Skip(pointer + 4).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "shakescreen(intensity = " + arg1 + ", timer = " + arg2 + ");");
                            break;

                     /* case 0x0D:
                               // Unknown Opcode. Not used in any retail script.
                               break; */

                        case 0x0E:
                            // Fadeout and Fadein Filter
                            // Determines the color used for the system.fade Opcode.
                            arg1 = Convert.ToInt32(fileData[pointer + 4] | (fileData[pointer + 3] << 8) | (fileData[pointer + 2] << 16)).ToString("X2").PadLeft(6, '0');
                            pointer = nexptr;
                            sw.WriteLine(indent + "screenfade.filter(rgb = " + arg1 + ");");
                            break;

                     /* case 0x0F, 0x10:
                               // Unknown Opcode. Not used in any retail script.
                               break; */

                     /* case 0x11:
                               // NEEDS PARSING
                               break; */

                        case 0x12:
                            // Call Script
                            // After being called, the current script will resume where it left off.
                            arg1 = Convert.ToString(fileData[pointer + 2] | (fileData[pointer + 3] << 8) | (fileData[pointer + 4] << 16)).PadLeft(8, '0');
                            pointer = nexptr;
                            sw.WriteLine(indent + "system.load.script(" + arg1 + ");");
                            break;

                     /* case 0x13, 0x14:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0x15:
                            // Load Map
                            // Loading is instantaneous, with no transition or pause on PC!
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "system.load.map(" + arg1 + ");");
                            break;

                        case 0x16:
                            // Load Item World Map
                            // Loading is instantaneous, with no transition or pause on PC!
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "system.load.itemworld(" + arg1 + ");");
                            break;

                        case 0x17:
                            // Load Unrendered Map
                            // Only 0xFFFF (-1) is ever used, and only one script contains this opcode.
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "system.load.void(" + arg1 + ");");
                            break;

                        case 0x18:
                            // Reset Camera and Player Position
                            pointer = nexptr;
                            sw.WriteLine(indent + "system.refresh();");
                            break;

                     /* case 0x19, 0x1A:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0x1B:
                            // Setup Background Colors
                            arg1 = parseTime(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = Convert.ToInt32(fileData[pointer + 6] | (fileData[pointer + 5] << 8) | (fileData[pointer + 4] << 16)).ToString("X2").PadLeft(6, '0');
                            arg3 = Convert.ToInt32(fileData[pointer + 9] | (fileData[pointer + 8] << 8) | (fileData[pointer + 7] << 16)).ToString("X2").PadLeft(6, '0');
                            arg4 = Convert.ToInt32(fileData[pointer + 12] | (fileData[pointer + 11] << 8) | (fileData[pointer + 10] << 16)).ToString("X2").PadLeft(6, '0');
                            arg5 = Convert.ToInt32(fileData[pointer + 15] | (fileData[pointer + 14] << 8) | (fileData[pointer + 13] << 16)).ToString("X2").PadLeft(6, '0');
                            pointer = nexptr;
                            sw.WriteLine(indent + "background(rgb.topLeft = " + arg2 + ", rgb.topRight = " + arg3 + ", rgb.bottomLeft = " + arg4 + ", rgb.bottomRight = " + arg5 + ", timer = " + arg1 + ");");
                            break;

                        case 0x1C:
                            // Move Asset
                            // Moving a normal Actor with this will treat it as a 2D overlay on the screen, effectively breaking their functionality.
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 6).Take(2).ToArray());
                            arg4 = parseInt16(fileData.Skip(pointer + 8).Take(2).ToArray());
                            arg5 = fileData[pointer + 10].ToString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "actor.asset.pos(entity = " + arg1 + ", pos.x = " + arg2 + ", pos.y = " + arg3 + ", pos.z = " + arg4 + ", " + arg5 + ");");
                            break;

                     /* case 0x1D, 0x1E, 0x1F, 0x20, 0x21, 0x22:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0x23:
                            // Disable Actor
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "actor.disable(entity = " + arg1 + ");");
                            break;

                        case 0x24:
                            // Actor Cutscene Filter
                            // Used for special abilities and magic with unique cutscenes.
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            arg3 = Convert.ToInt32(fileData[pointer + 8] | (fileData[pointer + 7] << 8) | (fileData[pointer + 6] << 16)).ToString("X2").PadLeft(6, '0');
                            arg4 = fileData[pointer + 9].ToString();
                            arg5 = parseInt16(fileData.Skip(pointer + 10).Take(2).ToArray());
                            arg6 = parseInt16(fileData.Skip(pointer + 12).Take(2).ToArray());
                            arg7 = parseInt16(fileData.Skip(pointer + 14).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "actor.cutscene.filter(entity = " + arg1 + ", rgb = " + arg3 + ", alpha = " + arg4 + ", stretch.x = " + arg5 + ", stretch.y = " + arg6 + ", rotate.z = " + arg7 + ", " + arg2 + ");");
                            break;

                     /* case 0x25, 0x26:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0x27:
                            // Move Cursor
                            // The offset arg displaces the position of the cursor, this can even off-center the cursor from a tile.
                            // Sadly, it's not currently understood how it works or why the values are defined as they are in retail scripts.
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 6).Take(2).ToArray());
                            arg4 = parseTime(fileData.Skip(pointer + 8).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "cursor(pos.x = " + arg2 + ", pos.y = " + arg3 + ", pos.offset = " + arg1 + ", timer = " + arg4 + ");");
                            break;

                        case 0x28:
                            // Camera Lock and Unlock
                            arg1 = fileData[pointer + 2].ToString(); variable = arg1; arg1 = Camera();
                            pointer = nexptr;
                            sw.WriteLine(indent + "camera(" + arg1 + ");");
                            break;

                        case 0x29:
                            // Camera Zoom
                            arg1 = parseTime(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "camera.zoom(" + arg2 + ", timer = " + arg1 + ");");
                            break;

                        case 0x2A:
                            // Camera Roll
                            // The "Z-Pos" arg doesn't seem to work and always seems set to 0x00 in retail scripts.
                            arg1 = parseTime(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 6).Take(2).ToArray());
                            arg4 = parseInt16(fileData.Skip(pointer + 8).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "camera.roll(pos.x = " + arg2 + ", pos.y = " + arg3 + ", timer = " + arg1 + ");");
                            break;

                        case 0x2B:
                            // Camera Pitch
                            arg1 = parseTime(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "camera.pitch(" + arg2 + ", timer = " + arg1 + ");");
                            break;

                        case 0x2C:
                            // Camera Pan
                            // The offset arg displaces the position of the camera.
                            // Sadly, it's not currently understood how it works or why the values are defined as they are in retail scripts.
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 6).Take(2).ToArray());
                            arg4 = parseTime(fileData.Skip(pointer + 8).Take(2).ToArray());
                            arg5 = parseInt16(fileData.Skip(pointer + 10).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "camera.pan(pos.x = " + arg2 + ", pos.y = " + arg3 + ", pos.offset = " + arg1 + ", timer = " + arg4 + ", " + arg5 + ");");
                            break;

                        case 0x2D:
                            // Position Settings
                            // Setting the last arguement to 0x00 will cause the Actor to freeze at their current Y position, ignoring gravity.
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "actor.pos.settings(entity = " + arg1 + ", " + arg2 + ");");
                            break;

                        case 0x2E:
                            // Camera Rotation
                            // The "Z-Pos" arg doesn't seem to work and always seems set to 0 in retail scripts.
                            arg1 = parseTime(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 6).Take(2).ToArray());
                            arg4 = parseInt16(fileData.Skip(pointer + 8).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "camera.rotate(pos.x = " + arg2 + ", pos.y = " + arg3 + ", timer = " + arg1 + ");");
                            break;

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
                            sw.WriteLine(indent + "input.wait();");
                            break;

                     /* case 0x35, 0x36:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                     /* case 0x37, 0x38:
                            // NEEDS PARSING
                            break; */

                        case 0x39:
                            // Background Image Setup
                            // Assigns the designated BG Image to the Index chosen. Last arg is unknown currently..
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = fileData[pointer + 3].ToString();
                            arg3 = fileData[pointer + 4].ToString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "background.image(imageIndex = " + arg1 + ", bgID = " + arg2 + ", " + arg3 + ");");
                            break;

                        case 0x3A:
                            // Show or Hide a Background Image
                            // Using 0xFF (-1) clears any currently displayed image from the screen.
                            arg1 = fileData[pointer + 2].ToString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "background.image.display(imageIndex = " + arg1 + ");");
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
                            // Add 10000 to the character ID to load an actor from the current save file instead of randomly generating a new one.
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = parseInt16(fileData.Skip(pointer + 3).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "actor(entity = " + arg1 + ", charID = " + arg2 + ");");
                            break;

                        case 0x4F:
                            // Give the player the designated Character
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "get.chara(charID = " + arg1 + ", level = " + arg2 + ");");
                            break;

                        case 0x50:
                            // Give the player the designated amount of HL
                            arg1 = parseInt32(fileData.Skip(pointer + 2).Take(4).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "get.money(" + arg1 + ");");
                            break;

                        case 0x51:
                            // Give the player the designated Item
                            // The second arg appears to relate to rarity somehow, or perhaps even acting as a seed for item generation?
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "get.item(itemID = " + arg1 + ", " + arg2 + ");");
                            break;

                     /* case 0x52:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0x53:
                            // Send controller Input
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "input(buttonID = " + arg1 + ");");
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
                            sw.WriteLine(indent + "system.state(" + arg1 + ", chapter = " + arg2 + ");");
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
                            // Move Actor
                            // The offset arg displaces the position of the actor, this can even off-center an actor from its tile.
                            // Sadly, it's not currently understood how it works or why the values are defined as they are in retail scripts.
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 6).Take(2).ToArray());
                            arg4 = parseInt16(fileData.Skip(pointer + 8).Take(2).ToArray());
                            arg5 = parseInt16(fileData.Skip(pointer + 10).Take(2).ToArray());
                            arg6 = parseTime(fileData.Skip(pointer + 12).Take(2).ToArray());
                            arg7 = fileData[pointer + 14].ToString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "actor.pos(entity = " + arg1 + ", pos.x = " + arg3 + ", pos.y = " + arg4 + ", pos.z = " + arg5 + ", pos.offset = " + arg2 + ", timer = " + arg6 + ", " + arg7 + ");");
                            break;

                        case 0x66:
                            // Play Animation
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseUInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "actor.anim(entity = " + arg1 + ", anmID = " + arg2 + ");");
                            break;

                        case 0x67:
                            // Play Animation
                            // Unlike actor.anim the ID used here doesn't seem to match any known index..
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(4).ToArray());
                            arg3 = fileData[pointer + 6].ToString(); //variable = arg3; arg3 = Direction();
                            pointer = nexptr;
                            sw.WriteLine(indent + "actor.anim.settings(entity = " + arg1 + ", ID = " + arg2 + ", direction = " + arg3 + ");");
                            break;

                        /* case 0x68, 0x69:
                               // NEEDS PARSING
                               break; */

                        case 0x6A:
                            // Play Animation
                            // Unlike actor.anim the ID used here doesn't seem to match any known index..
                            // The values used seem to behave differently compared to actor.anim.settings, but little else is known.
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(4).ToArray());
                            arg3 = fileData[pointer + 6].ToString(); //variable = arg3; arg3 = Direction();
                            pointer = nexptr;
                            sw.WriteLine(indent + "actor.anim.settings.alt(entity = " + arg1 + ", ID = " + arg2 + ", direction = " + arg3 + ");");
                            break;

                        case 0x6B:
                            // Actor Idle
                            // This seems like it sets a designated idle animation for the actor. (IE: NPC, Battle, etc..)
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = fileData[pointer + 4].ToString(); //stance = arg2; arg2 = Stance();
                            pointer = nexptr;
                            sw.WriteLine(indent + "actor.anim.idle(entity = " + arg1 + ", " + arg2 + ");");
                            break;

                     /* case 0x6C:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0x6D:
                            // Spawn NPC
                            // Add 10000 to the charID to load an actor from the current save file instead of randomly generating a new one.
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = parseInt16(fileData.Skip(pointer + 3).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 5).Take(2).ToArray());
                            arg4 = fileData[pointer + 7].ToString(); variable = arg4; arg4 = Role();
                            pointer = nexptr;
                            sw.WriteLine(indent + "actor.npc(entity = " + arg1 + ", charID = " + arg2 + ", level = " + arg3 + ", role = " + arg4 + ");");
                            break;

                        case 0x6E:
                            // Spawn Asset
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "actor.asset(entity = " + arg1 + ", charID = " + arg2 + ");");
                            break;

                     /* case 0x6F:
                            // NEEDS PARSING
                            break; */

                        case 0x70:
                            // Modify the Map Terrain
                            // Primarily used in special ability cutscenes to create craters after a mass-explosion.
                            // Currently not understood how it works, but it definitely has some intriguing potential..
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 6).Take(2).ToArray());
                            arg4 = parseInt16(fileData.Skip(pointer + 8).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "system.edit.map(" + arg1 + ", " + arg2 + ", " + arg3 + ", " + arg4 + ");");
                            break;

                        case 0x71:
                            // Spawn GFX Effect
                            // The effectID correlates to what is stored in EFFECT.KM3, presumably anyway.
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 6).Take(2).ToArray());
                            arg4 = parseInt16(fileData.Skip(pointer + 8).Take(2).ToArray());
                            arg5 = parseInt16(fileData.Skip(pointer + 10).Take(2).ToArray());
                            arg6 = parseInt16(fileData.Skip(pointer + 12).Take(2).ToArray());
                            arg7 = parseInt16(fileData.Skip(pointer + 14).Take(2).ToArray());
                            arg8 = fileData[pointer + 16].ToString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "system.gfx(effectID = " + arg1 + ", pos.x = " + arg3 + ", pos.y = " + arg4 + ", pos.z = " + arg5 + ", pos.offset = " + arg2 + ", scale = " + arg6 + ", speed = " + arg7 + ", " + arg8 + ");");
                            break;

                     /* case 0x72:
                            // NEEDS PARSING
                            break; */

                        case 0x73:
                            // Rotate Actor
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = parseTime(fileData.Skip(pointer + 3).Take(2).ToArray());
                            arg3 = parseInt16(fileData.Skip(pointer + 5).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "actor.rot(entity = " + arg1 + ", rotate = " + arg3 + ", timer = " + arg2 + ");");
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
                            // Actor Filter
                            // Yes the Actor, Color, and Alpha values are Int16 each.. I don't know why either.
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = Convert.ToInt32(fileData[pointer + 8] | (fileData[pointer + 6] << 8) | (fileData[pointer + 4] << 16)).ToString("X2").PadLeft(6, '0');
                            arg3 = parseInt16(fileData.Skip(pointer + 10).Take(2).ToArray());
                            arg4 = parseTime(fileData.Skip(pointer + 12).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "actor.filter(entity = " + arg1 + ", rgb = " + arg2 + ", alpha = " + arg3 + ", timer = " + arg4 + ");");
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
                            // Play Voiced Cutscene Dialog
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = fileData[pointer + 4].ToString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "sound.dialog(dialogID = " + arg1 + ", volume = " + arg2 + ");");
                            break;

                     /* case 0x84, 0x85, 0x86:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                     /* case 0x87:
                            // NEEDS PARSING
                            break; */

                        case 0x88:
                            // Play Actor Voices
                            // The voiceID correlates to what is stored in VOICE01.PBD and VOICE02.PBD, presumably anyway.
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = fileData[pointer + 4].ToString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "sound.voice(voiceID = " + arg1 + ", volume = " + arg2 + ");");
                            break;

                     /* case 0x89, 0x8A, 0x8B:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0x8C:
                            // Play Sound Effect
                            // The seID correlates to what is stored in SE01.PBD and SE02.PBD, presumably anyway.
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = fileData[pointer + 4].ToString();
                            arg3 = parseUInt16(fileData.Skip(pointer + 5).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "sound.sfx(seID = " + arg1 + ", samplerate = " + arg3 + ", volume = " + arg2 + ");");
                            break;

                     /* case 0x8D:
                            // NEEDS PARSING
                            break; */

                     /* case 0x8E, 0x8F, 0x90:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0x91:
                            // Play Background Music
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = fileData[pointer + 3].ToString();
                            pointer = nexptr;
                            sw.WriteLine(indent + "sound.bgm(musicID = " + arg1 + ", volume = " + arg2 + ");");
                            break;

                        case 0x92:
                            // Background Music Settings
                            // Lower or Raise the volume over the designated period of time.
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = parseTime(fileData.Skip(pointer + 3).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "sound.bgm.settings(volume = " + arg1 + ", timer = " + arg2 + ");");
                            break;

                     /* case 0x93:
                               // NEEDS PARSING
                               break; */

                        case 0x94:
                            // Play Background Music
                            // Appears to function the same as sound.bgm but uses Int16 for its args instead..?
                            arg1 = parseInt16(fileData.Skip(pointer + 2).Take(2).ToArray());
                            arg2 = parseInt16(fileData.Skip(pointer + 4).Take(2).ToArray());
                            pointer = nexptr;
                            sw.WriteLine(indent + "sound.bgm.alt(musicID = " + arg1 + ", volume = " + arg2 + ");");
                            break;

                     /* case 0x95:
                            // NEEDS PARSING
                            break; */

                        /* case 0x96, 0x97:
                               // Unknown Opcode. Not used in any retail script.
                               break; */

                        case 0x98:
                            // Setup Dialog interaction for an Actor
                            // The talkID correlates to what is stored in TALK.DAT.
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = Convert.ToString(fileData[pointer + 4] | (fileData[pointer + 5] << 8) | (fileData[pointer + 6] << 16));
                            pointer = nexptr;
                            sw.WriteLine(indent + "actor.talk(entity = " + arg1 + ", talkID = " + arg2 + ");");
                            break;

                     /* case 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
                             0xB0, 0xB1, 0xB2, 0xB3, 0xB4, 0xB5, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7:
                            // Unknown Opcode. Not used in any retail script.
                            break; */

                        case 0xC8:
                            // Hardcoded Handler
                            // Triggers a lot of different things on the engine level, still needs to be researched..
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
                            sw.WriteLine(indent + "system.load.ui(menuID = " + arg1 + ");");
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
                            // This appears to be unused in all other builds of Disgaea.
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

        public static void Compile()
        {
            script = MainWindow.WorkingScript;

            using (var sr = new StreamReader(script))
            {
                curLine = sr.ReadLine();
                filename = getBetween(curLine, "script(", ")");
                fileDataLength = File.ReadLines(script).Count();
                pointer = 1;

                scrdir = System.IO.Path.GetDirectoryName(MainWindow.UserFile) + "/Compiled/";

                Directory.CreateDirectory(scrdir);
                File.Delete(scrdir + filename + ".BIN");
                bw = new BinaryWriter(File.Create(scrdir + filename + ".BIN"));

                while (pointer < fileDataLength)
                {
                    curLine = sr.ReadLine();
                    if (!curLine.Contains("print"))
                    {
                        curLine = String.Concat(curLine.Where(c => !Char.IsWhiteSpace(c)));
                    }

                    int nexptr = pointer + 1;

                    switch (curLine)
                    {
                        case string curLine when stringContainsNoArgs("{"):
                            pointer = nexptr;
                            break;

                        case string curLine when stringContainsNoArgs("}"):
                            // End of Script
                            newScript = newScriptList.SelectMany(a => a).ToArray();
                            bw.Write(newScript);
                            bw.Close();

                            newScriptList = new List<byte[]>();
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("sleep"):
                            // 0x01
                            opArray = new byte[4];
                            arg1 = getBetween(curLine, "timer=", ")");

                            Array.Copy(writeValue(0x01), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x02), 0, opArray, 1, 1);
                            Array.Copy(writeTime(arg1), 0, opArray, 2, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContainsNoArgs("return"):
                            // 0x02
                            opArray = new byte[2];
                            Array.Copy(writeValue(0x02), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x00), 0, opArray, 1, 1);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("input.lock"):
                            // 0x05, 0x06
                            opArray = new byte[2];
                            arg1 = getBetween(curLine, "(", ")");

                            if (arg1.Equals("true", StringComparison.OrdinalIgnoreCase))
                            {
                                Array.Copy(writeValue(0x05), 0, opArray, 0, 1);
                                Array.Copy(writeValue(0x00), 0, opArray, 1, 1);
                            }
                            else if (arg1.Equals("false", StringComparison.OrdinalIgnoreCase))
                            {
                                Array.Copy(writeValue(0x06), 0, opArray, 0, 1);
                                Array.Copy(writeValue(0x00), 0, opArray, 1, 1);
                            }

                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("if"):
                            // 0x07
                            CompileIfStatement();
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("system.flag"):
                            // 0x08
                            opArray = new byte[9];
                            arg1 = getBetween(curLine, "(", "=");
                            arg2 = getBetween(curLine, "=", ")");

                            Array.Copy(writeValue(0x08), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x07), 0, opArray, 1, 1);
                            Array.Copy(writeValue(0x01), 0, opArray, 2, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 3, 2);
                            Array.Copy(writeValue(0x01), 0, opArray, 5, 2);
                            Array.Copy(writeInt16(arg2), 0, opArray, 7, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContainsNoArgs("endif"):
                            // 0x09
                            opArray = new byte[2];
                            Array.Copy(writeValue(0x09), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x00), 0, opArray, 1, 1);
                            newScriptList.Add(opArray);

                            int curAddress;
                            int ifAddress;
                            int ifBlockSize;

                            ifPointer = ifPointer - 2;
                            tempScript = newScriptList.SelectMany(a => a).ToArray();
                            tempAddress = tempAddressList.SelectMany(a => a).ToArray();

                            opArray = new byte[2];
                            Array.Copy(tempAddress, ifPointer, opArray, 0, 2);

                            curAddress = tempScript.Length - 2;
                            ifAddress = BitConverter.ToInt16(opArray, 0);
                            ifBlockSize = curAddress - ifAddress;

                            Array.Copy(writeValue(ifBlockSize), 0, tempScript, ifAddress, 2);
                            newScriptList = new List<byte[]>();
                            newScriptList.Add(tempScript);

                            //shortIfLoop = true;

                            if (ifPointer == 0)
                            {
                                tempAddressList = new List<byte[]>();
                                shortIfLoop = false;
                            }

                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("screenfade"):
                            // 0x0A
                            opArray = new byte[5];
                            arg1 = getBetween(curLine, "intensity=", ",");
                            arg2 = getBetween(curLine, "timer=", ")");

                            Array.Copy(writeValue(0x0A), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x03), 0, opArray, 1, 1);
                            Array.Copy(writeByte(arg1), 0, opArray, 2, 1);
                            Array.Copy(writeTime(arg2), 0, opArray, 3, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("input.wait"):
                            // 0x0B, 0x34
                            arg0 = getBetween(curLine, "wait", ";");

                            if (arg0 != "()")
                            {
                                opArray = new byte[4];
                                arg1 = getBetween(curLine, "timer=", ")");

                                Array.Copy(writeValue(0x0B), 0, opArray, 0, 1);
                                Array.Copy(writeValue(0x02), 0, opArray, 1, 1);
                                Array.Copy(writeTime(arg1), 0, opArray, 2, 2);
                                newScriptList.Add(opArray);
                                pointer = nexptr;
                            }
                            else
                            {
                                opArray = new byte[2];

                                Array.Copy(writeValue(0x34), 0, opArray, 0, 1);
                                Array.Copy(writeValue(0x00), 0, opArray, 1, 1);
                                newScriptList.Add(opArray);
                                pointer = nexptr;
                            }
                            break;

                        case string curLine when stringContains("shakescreen"):
                            // 0x0C
                            opArray = new byte[6];
                            arg1 = getBetween(curLine, "intensity=", ",");
                            arg2 = getBetween(curLine, "timer=", ")");

                            Array.Copy(writeValue(0x0C), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x04), 0, opArray, 1, 1);
                            Array.Copy(writeTime(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeTime(arg2), 0, opArray, 4, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("screenfade.filter"):
                            // 0x0E
                            opArray = new byte[5];
                            arg1 = getBetween(curLine, "rgb=", ")");

                            Array.Copy(writeValue(0x0E), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x03), 0, opArray, 1, 1);
                            Array.Copy(writeHex(arg1.Substring(0, 2)), 0, opArray, 2, 1);
                            Array.Copy(writeHex(arg1.Substring(2, 2)), 0, opArray, 3, 1);
                            Array.Copy(writeHex(arg1.Substring(4, 2)), 0, opArray, 4, 1);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("system.load.script"):
                            // 0x12
                            opArray = new byte[5];
                            arg1 = getBetween(curLine, "(", ")");

                            Array.Copy(writeValue(0x12), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x03), 0, opArray, 1, 1);
                            Array.Copy(writeInt32(arg1), 0, opArray, 2, 3);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("system.load.map"):
                            // 0x15
                            opArray = new byte[4];
                            arg1 = getBetween(curLine, "(", ")");

                            Array.Copy(writeValue(0x15), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x02), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("system.load.itemworld"):
                            // 0x16
                            opArray = new byte[4];
                            arg1 = getBetween(curLine, "(", ")");

                            Array.Copy(writeValue(0x16), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x02), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("system.load.void"):
                            // 0x17
                            opArray = new byte[4];
                            arg1 = getBetween(curLine, "(", ")");

                            Array.Copy(writeValue(0x17), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x02), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("system.refresh"):
                            // 0x18
                            opArray = new byte[2];
                            Array.Copy(writeValue(0x18), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x00), 0, opArray, 1, 1);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("background"):
                            // 0x1B
                            opArray = new byte[16];
                            arg1 = getBetween(curLine, "timer=", ")");
                            arg2 = getBetween(curLine, "topLeft=", ",");
                            arg3 = getBetween(curLine, "topRight=", ",");
                            arg4 = getBetween(curLine, "bottomLeft=", ",");
                            arg5 = getBetween(curLine, "bottomRight=", ",");

                            Array.Copy(writeValue(0x1B), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x0E), 0, opArray, 1, 1);
                            Array.Copy(writeTime(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeHex(arg2.Substring(0, 2)), 0, opArray, 4, 1);
                            Array.Copy(writeHex(arg2.Substring(2, 2)), 0, opArray, 5, 1);
                            Array.Copy(writeHex(arg2.Substring(4, 2)), 0, opArray, 6, 1);
                            Array.Copy(writeHex(arg3.Substring(0, 2)), 0, opArray, 7, 1);
                            Array.Copy(writeHex(arg3.Substring(2, 2)), 0, opArray, 8, 1);
                            Array.Copy(writeHex(arg3.Substring(4, 2)), 0, opArray, 9, 1);
                            Array.Copy(writeHex(arg4.Substring(0, 2)), 0, opArray, 10, 1);
                            Array.Copy(writeHex(arg4.Substring(2, 2)), 0, opArray, 11, 1);
                            Array.Copy(writeHex(arg4.Substring(4, 2)), 0, opArray, 12, 1);
                            Array.Copy(writeHex(arg5.Substring(0, 2)), 0, opArray, 13, 1);
                            Array.Copy(writeHex(arg5.Substring(2, 2)), 0, opArray, 14, 1);
                            Array.Copy(writeHex(arg5.Substring(4, 2)), 0, opArray, 15, 1);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("actor.asset.pos"):
                            // 0x1C
                            opArray = new byte[11];
                            arg1 = getBetween(curLine, "entity=", ",");
                            arg2 = getBetween(curLine, "pos.x=", ",");
                            arg3 = getBetween(curLine, "pos.y=", ",");
                            arg4 = getBetween(curLine, "pos.z=", ",");
                            arg5 = getBetween(curLine, "pos.z=" + arg4 + ",", ")");

                            Array.Copy(writeValue(0x1C), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x09), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeInt16(arg2), 0, opArray, 4, 2);
                            Array.Copy(writeInt16(arg3), 0, opArray, 6, 2);
                            Array.Copy(writeInt16(arg4), 0, opArray, 8, 2);
                            Array.Copy(writeByte(arg5), 0, opArray, 10, 1);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("actor.disable"):
                            // 0x23
                            opArray = new byte[4];
                            arg1 = getBetween(curLine, "entity=", ")");

                            Array.Copy(writeValue(0x23), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x02), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("actor.cutscene.filter"):
                            // 0x24
                            opArray = new byte[16];
                            arg1 = getBetween(curLine, "entity=", ",");
                            arg3 = getBetween(curLine, "rgb=", ",");
                            arg4 = getBetween(curLine, "alpha=", ",");
                            arg5 = getBetween(curLine, "stretch.x=", ",");
                            arg6 = getBetween(curLine, "stretch.y=", ",");
                            arg7 = getBetween(curLine, "rotate.z=", ",");
                            arg2 = getBetween(curLine, "rotate.z=" + arg7 + ",", ")");

                            Array.Copy(writeValue(0x24), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x0E), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeInt16(arg2), 0, opArray, 4, 2);
                            Array.Copy(writeHex(arg3.Substring(0, 2)), 0, opArray, 6, 1);
                            Array.Copy(writeHex(arg3.Substring(2, 2)), 0, opArray, 7, 1);
                            Array.Copy(writeHex(arg3.Substring(4, 2)), 0, opArray, 8, 1);
                            Array.Copy(writeByte(arg4), 0, opArray, 9, 1);
                            Array.Copy(writeInt16(arg5), 0, opArray, 10, 2);
                            Array.Copy(writeInt16(arg6), 0, opArray, 12, 2);
                            Array.Copy(writeInt16(arg7), 0, opArray, 14, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("cursor"):
                            // 0x27
                            opArray = new byte[10];
                            arg1 = getBetween(curLine, "pos.offset=", ",");
                            arg2 = getBetween(curLine, "pos.x=", ",");
                            arg3 = getBetween(curLine, "pos.y=", ",");
                            arg4 = getBetween(curLine, "timer=", ")");

                            Array.Copy(writeValue(0x27), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x08), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeInt16(arg2), 0, opArray, 4, 2);
                            Array.Copy(writeInt16(arg3), 0, opArray, 6, 2);
                            Array.Copy(writeTime(arg4), 0, opArray, 8, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("camera"):
                            // 0x28
                            opArray = new byte[3];
                            arg1 = getBetween(curLine, "(", ")");

                            Array.Copy(writeValue(0x28), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x01), 0, opArray, 1, 1);

                            if (arg1.Equals("free", StringComparison.OrdinalIgnoreCase))
                            {
                                Array.Copy(writeValue(0x00), 0, opArray, 2, 1);
                            }
                            else if (arg1.Equals("locked", StringComparison.OrdinalIgnoreCase))
                            {
                                Array.Copy(writeValue(0x01), 0, opArray, 2, 1);
                            }
                            else if (arg1.Equals("unk1", StringComparison.OrdinalIgnoreCase))
                            {
                                Array.Copy(writeValue(0x02), 0, opArray, 2, 1);
                            }
                            else if (arg1.Equals("unk2", StringComparison.OrdinalIgnoreCase))
                            {
                                Array.Copy(writeValue(0x03), 0, opArray, 2, 1);
                            }

                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("camera.zoom"):
                            // 0x29
                            opArray = new byte[6];
                            arg1 = getBetween(curLine, "timer=", ")");
                            arg2 = getBetween(curLine, "(", ",");

                            Array.Copy(writeValue(0x29), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x04), 0, opArray, 1, 1);
                            Array.Copy(writeTime(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeInt16(arg2), 0, opArray, 4, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("camera.roll"):
                            // 0x2A
                            opArray = new byte[10];
                            arg1 = getBetween(curLine, "timer=", ")");
                            arg2 = getBetween(curLine, "pos.x=", ",");
                            arg3 = getBetween(curLine, "pos.y=", ",");
                            // arg4 is not currently parsed, so we assume 0x00.

                            Array.Copy(writeValue(0x2A), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x08), 0, opArray, 1, 1);
                            Array.Copy(writeTime(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeInt16(arg2), 0, opArray, 4, 2);
                            Array.Copy(writeInt16(arg3), 0, opArray, 6, 2);
                            Array.Copy(writeValue(0x00), 0, opArray, 8, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("camera.pitch"):
                            // 0x2B
                            opArray = new byte[6];
                            arg1 = getBetween(curLine, "timer=", ")");
                            arg2 = getBetween(curLine, "(", ",");

                            Array.Copy(writeValue(0x2B), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x04), 0, opArray, 1, 1);
                            Array.Copy(writeTime(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeInt16(arg2), 0, opArray, 4, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("camera.pan"):
                            // 0x2C
                            opArray = new byte[12];
                            arg1 = getBetween(curLine, "pos.offset=", ",");
                            arg2 = getBetween(curLine, "pos.x=", ",");
                            arg3 = getBetween(curLine, "pos.y=", ",");
                            arg4 = getBetween(curLine, "timer=", ",");
                            arg5 = getBetween(curLine, "timer=" + arg4 + ",", ")");

                            Array.Copy(writeValue(0x2C), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x0A), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeInt16(arg2), 0, opArray, 4, 2);
                            Array.Copy(writeInt16(arg3), 0, opArray, 6, 2);
                            Array.Copy(writeTime(arg4), 0, opArray, 8, 2);
                            Array.Copy(writeInt16(arg5), 0, opArray, 10, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("actor.pos.settings"):
                            // 0x2D
                            opArray = new byte[6];
                            arg1 = getBetween(curLine, "entity=", ",");
                            arg2 = getBetween(curLine, ",", ")");

                            Array.Copy(writeValue(0x2D), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x04), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeInt16(arg2), 0, opArray, 4, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("camera.rotate"):
                            // 0x2E
                            opArray = new byte[10];
                            arg1 = getBetween(curLine, "timer=", ")");
                            arg2 = getBetween(curLine, "pos.x=", ",");
                            arg3 = getBetween(curLine, "pos.y=", ",");
                            // arg4 is not currently parsed, so we assume 0x00.

                            Array.Copy(writeValue(0x2E), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x08), 0, opArray, 1, 1);
                            Array.Copy(writeTime(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeInt16(arg2), 0, opArray, 4, 2);
                            Array.Copy(writeInt16(arg3), 0, opArray, 6, 2);
                            Array.Copy(writeValue(0x00), 0, opArray, 8, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("print"):
                            // 0x32
                            arg1 = getBetween(curLine, "(\"", "\")"); byte[] jisString = Encoding.GetEncoding("shift_jis").GetBytes(arg1);
                            opArray = new byte[jisString.Length + 3];

                            Array.Copy(writeValue(0x32), 0, opArray, 0, 1);
                            Array.Copy(writeValue(jisString.Length + 1), 0, opArray, 1, 1);
                            Array.Copy(jisString, 0, opArray, 2, jisString.Length);
                            Array.Copy(writeValue(0x00), 0, opArray, jisString.Length + 2, 1);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("background.image"):
                            // 0x39
                            opArray = new byte[5];
                            arg1 = getBetween(curLine, "imageIndex=", ",");
                            arg2 = getBetween(curLine, "bgID=", ",");
                            arg3 = getBetween(curLine, "bgID=" + arg2 + ",", ")");

                            Array.Copy(writeValue(0x39), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x03), 0, opArray, 1, 1);
                            Array.Copy(writeByte(arg1), 0, opArray, 2, 1);
                            Array.Copy(writeByte(arg2), 0, opArray, 3, 1);
                            Array.Copy(writeByte(arg3), 0, opArray, 4, 1);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("background.image.display"):
                            // 0x3A
                            opArray = new byte[3];
                            arg1 = getBetween(curLine, "imageIndex=", ")");

                            Array.Copy(writeValue(0x3A), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x01), 0, opArray, 1, 1);
                            Array.Copy(writeByte(arg1), 0, opArray, 2, 1);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("actor"):
                            // 0x4E
                            opArray = new byte[5];
                            arg1 = getBetween(curLine, "entity=", ",");
                            arg2 = getBetween(curLine, "charID=", ")");

                            Array.Copy(writeValue(0x4E), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x03), 0, opArray, 1, 1);
                            Array.Copy(writeByte(arg1), 0, opArray, 2, 1);
                            Array.Copy(writeInt16(arg2), 0, opArray, 3, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("get.chara"):
                            // 0x4F
                            opArray = new byte[6];
                            arg1 = getBetween(curLine, "charID=", ",");
                            arg2 = getBetween(curLine, "level=", ")");

                            Array.Copy(writeValue(0x4F), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x04), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeInt16(arg2), 0, opArray, 4, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("get.money"):
                            // 0x50
                            opArray = new byte[6];
                            arg1 = getBetween(curLine, "(", ")");

                            Array.Copy(writeValue(0x50), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x04), 0, opArray, 1, 1);
                            Array.Copy(writeInt32(arg1), 0, opArray, 2, 4);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("get.item"):
                            // 0x51
                            opArray = new byte[6];
                            arg1 = getBetween(curLine, "itemID=", ",");
                            arg2 = getBetween(curLine, ",", ")");

                            Array.Copy(writeValue(0x51), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x04), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeInt16(arg2), 0, opArray, 4, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("input"):
                            // 0x53
                            opArray = new byte[4];
                            arg1 = getBetween(curLine, "buttonID=", ")");

                            Array.Copy(writeValue(0x53), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x02), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("system.state"):
                            // 0x5A
                            opArray = new byte[4];
                            arg1 = getBetween(curLine, "(", ",");
                            arg2 = getBetween(curLine, "chapter=", ")");

                            Array.Copy(writeValue(0x5A), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x02), 0, opArray, 1, 1);
                            Array.Copy(writeByte(arg1), 0, opArray, 2, 1);
                            Array.Copy(writeByte(arg2), 0, opArray, 3, 1);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("actor.pos"):
                            // 0x65
                            opArray = new byte[15];
                            arg1 = getBetween(curLine, "entity=", ",");
                            arg2 = getBetween(curLine, "pos.offset=", ",");
                            arg3 = getBetween(curLine, "pos.x=", ",");
                            arg4 = getBetween(curLine, "pos.y=", ",");
                            arg5 = getBetween(curLine, "pos.z=", ",");
                            arg6 = getBetween(curLine, "timer=", ",");
                            arg7 = getBetween(curLine, "timer=" + arg6 + ",", ")");

                            Array.Copy(writeValue(0x65), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x0D), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeInt16(arg2), 0, opArray, 4, 2);
                            Array.Copy(writeInt16(arg3), 0, opArray, 6, 2);
                            Array.Copy(writeInt16(arg4), 0, opArray, 8, 2);
                            Array.Copy(writeInt16(arg5), 0, opArray, 10, 2);
                            Array.Copy(writeTime(arg6), 0, opArray, 12, 2);
                            Array.Copy(writeByte(arg7), 0, opArray, 14, 1);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("actor.anim"):
                            // 0x66
                            opArray = new byte[6];
                            arg1 = getBetween(curLine, "entity=", ",");
                            arg2 = getBetween(curLine, "anmID=", ")");

                            Array.Copy(writeValue(0x66), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x04), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeUInt16(arg2), 0, opArray, 4, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("actor.anim.settings"):
                            // 0x67
                            opArray = new byte[7];
                            arg1 = getBetween(curLine, "entity=", ",");
                            arg2 = getBetween(curLine, "ID=", ",");
                            arg3 = getBetween(curLine, "direction=", ")");

                            Array.Copy(writeValue(0x67), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x05), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeInt16(arg2), 0, opArray, 4, 2);
                            Array.Copy(writeByte(arg3), 0, opArray, 6, 1);

                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("actor.anim.settings.alt"):
                            // 0x6A
                            opArray = new byte[7];
                            arg1 = getBetween(curLine, "entity=", ",");
                            arg2 = getBetween(curLine, "ID=", ",");
                            arg3 = getBetween(curLine, "direction=", ")");

                            Array.Copy(writeValue(0x6A), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x05), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeInt16(arg2), 0, opArray, 4, 2);
                            Array.Copy(writeByte(arg3), 0, opArray, 6, 1);

                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("actor.anim.idle"):
                            // 0x6B
                            opArray = new byte[5];
                            arg1 = getBetween(curLine, "entity=", ",");
                            arg2 = getBetween(curLine, "entity=" + arg1 + ",", ")");

                            Array.Copy(writeValue(0x6B), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x03), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeByte(arg2), 0, opArray, 4, 1);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("actor.npc"):
                            // 0x6D
                            opArray = new byte[8];
                            arg1 = getBetween(curLine, "entity=", ",");
                            arg2 = getBetween(curLine, "charID=", ",");
                            arg3 = getBetween(curLine, "level=", ",");
                            arg4 = getBetween(curLine, "role=", ")");

                            Array.Copy(writeValue(0x6D), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x06), 0, opArray, 1, 1);
                            Array.Copy(writeByte(arg1), 0, opArray, 2, 1);
                            Array.Copy(writeInt16(arg2), 0, opArray, 3, 2);
                            Array.Copy(writeInt16(arg3), 0, opArray, 5, 2);

                            if (arg4.Equals("ally", StringComparison.OrdinalIgnoreCase))
                            {
                                Array.Copy(writeValue(0x00), 0, opArray, 7, 1);
                            }
                            else if (arg4.Equals("enemy", StringComparison.OrdinalIgnoreCase))
                            {
                                Array.Copy(writeValue(0x01), 0, opArray, 7, 1);
                            }
                            else if (arg4.Equals("neutral", StringComparison.OrdinalIgnoreCase))
                            {
                                Array.Copy(writeValue(0x02), 0, opArray, 7, 1);
                            }
                            else if (arg4.Equals("nemesis", StringComparison.OrdinalIgnoreCase))
                            {
                                Array.Copy(writeValue(0x03), 0, opArray, 7, 1);
                            }
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("actor.asset"):
                            // 0x6E
                            opArray = new byte[6];
                            arg1 = getBetween(curLine, "entity=", ",");
                            arg2 = getBetween(curLine, "charID=", ")");

                            Array.Copy(writeValue(0x6E), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x04), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeInt16(arg2), 0, opArray, 4, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("system.edit.map"):
                            // 0x70
                            opArray = new byte[10];
                            arg1 = getBetween(curLine, "(", ",");
                            arg2 = getBetween(curLine, arg1 + ",", ",");
                            arg3 = getBetween(curLine, arg1 + "," + arg2 + ",", ",");
                            arg4 = getBetween(curLine, arg1 + "," + arg2 + "," + arg3 + ",", ")");

                            Array.Copy(writeValue(0x70), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x08), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeInt16(arg2), 0, opArray, 4, 2);
                            Array.Copy(writeInt16(arg3), 0, opArray, 6, 2);
                            Array.Copy(writeInt16(arg4), 0, opArray, 8, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("system.gfx"):
                            // 0x71
                            opArray = new byte[18];
                            arg1 = getBetween(curLine, "effectID=", ",");
                            arg2 = getBetween(curLine, "pos.offset=", ",");
                            arg3 = getBetween(curLine, "pos.x=", ",");
                            arg4 = getBetween(curLine, "pos.y=", ",");
                            arg5 = getBetween(curLine, "pos.z=", ",");
                            arg6 = getBetween(curLine, "scale=", ",");
                            arg7 = getBetween(curLine, "speed=", ",");
                            arg8 = getBetween(curLine, "speed=" + arg7 + ",", ")");

                            Array.Copy(writeValue(0x71), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x10), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeInt16(arg2), 0, opArray, 4, 2);
                            Array.Copy(writeInt16(arg3), 0, opArray, 6, 2);
                            Array.Copy(writeInt16(arg4), 0, opArray, 8, 2);
                            Array.Copy(writeInt16(arg5), 0, opArray, 10, 2);
                            Array.Copy(writeInt16(arg6), 0, opArray, 12, 2);
                            Array.Copy(writeInt16(arg7), 0, opArray, 14, 2);
                            Array.Copy(writeInt16(arg8), 0, opArray, 16, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("actor.rot"):
                            // 0x73
                            opArray = new byte[7];
                            arg1 = getBetween(curLine, "entity=", ",");
                            arg2 = getBetween(curLine, "timer=", ")");
                            arg3 = getBetween(curLine, "rotate=", ",");

                            Array.Copy(writeValue(0x73), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x05), 0, opArray, 1, 1);
                            Array.Copy(writeByte(arg1), 0, opArray, 2, 1);
                            Array.Copy(writeTime(arg2), 0, opArray, 3, 2);
                            Array.Copy(writeInt16(arg3), 0, opArray, 5, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("actor.filter"):
                            // 0x7A
                            opArray = new byte[14];
                            arg1 = getBetween(curLine, "entity=", ",");
                            arg2 = getBetween(curLine, "rgb=", ",");
                            arg3 = getBetween(curLine, "alpha=", ",");
                            arg4 = getBetween(curLine, "timer=", ")");

                            Array.Copy(writeValue(0x7A), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x0C), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeHex(arg2.Substring(0, 2)), 0, opArray, 4, 1);
                            Array.Copy(writeHex(arg2.Substring(2, 2)), 0, opArray, 6, 1);
                            Array.Copy(writeHex(arg2.Substring(4, 2)), 0, opArray, 8, 1);
                            Array.Copy(writeInt16(arg3), 0, opArray, 10, 2);
                            Array.Copy(writeTime(arg4), 0, opArray, 12, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("sound.dialog"):
                            // 0x83
                            opArray = new byte[5];
                            arg1 = getBetween(curLine, "dialogID=", ",");
                            arg2 = getBetween(curLine, "volume=", ")");

                            Array.Copy(writeValue(0x83), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x03), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeByte(arg2), 0, opArray, 4, 1);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("sound.voice"):
                            // 0x88
                            opArray = new byte[5];
                            arg1 = getBetween(curLine, "voiceID=", ",");
                            arg2 = getBetween(curLine, "volume=", ")");

                            Array.Copy(writeValue(0x88), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x03), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeByte(arg2), 0, opArray, 4, 1);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("sound.sfx"):
                            // 0x8C
                            opArray = new byte[7];
                            arg1 = getBetween(curLine, "seID=", ",");
                            arg2 = getBetween(curLine, "volume=", ")");
                            arg3 = getBetween(curLine, "samplerate=", ",");

                            Array.Copy(writeValue(0x8c), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x05), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeByte(arg2), 0, opArray, 4, 1);
                            Array.Copy(writeUInt16(arg3), 0, opArray, 5, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("sound.bgm"):
                            // 0x91
                            opArray = new byte[4];
                            arg1 = getBetween(curLine, "musicID=", ",");
                            arg2 = getBetween(curLine, "volume=", ")");

                            Array.Copy(writeValue(0x91), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x02), 0, opArray, 1, 1);
                            Array.Copy(writeByte(arg1), 0, opArray, 2, 1);
                            Array.Copy(writeByte(arg2), 0, opArray, 3, 1);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("sound.bgm.settings"):
                            // 0x92
                            opArray = new byte[5];
                            arg1 = getBetween(curLine, "volume=", ",");
                            arg2 = getBetween(curLine, "timer=", ")");

                            Array.Copy(writeValue(0x92), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x03), 0, opArray, 1, 1);
                            Array.Copy(writeByte(arg1), 0, opArray, 2, 1);
                            Array.Copy(writeTime(arg2), 0, opArray, 3, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("sound.bgm.alt"):
                            // 0x94
                            opArray = new byte[6];
                            arg1 = getBetween(curLine, "musicID=", ",");
                            arg2 = getBetween(curLine, "volume=", ")");

                            Array.Copy(writeValue(0x94), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x04), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeInt16(arg2), 0, opArray, 4, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("actor.talk"):
                            // 0x98
                            opArray = new byte[7];
                            arg1 = getBetween(curLine, "entity=", ",");
                            arg2 = getBetween(curLine, "talkID=", ")");

                            Array.Copy(writeValue(0x98), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x05), 0, opArray, 1, 1);
                            Array.Copy(writeByte(arg1), 0, opArray, 2, 2);
                            Array.Copy(writeInt32(arg2), 0, opArray, 4, 3);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("system"):
                            // 0xC8
                            opArray = new byte[7];
                            arg1 = getBetween(curLine, "(", ",");
                            arg2 = getBetween(curLine, arg1 + ",", ",");
                            arg3 = getBetween(curLine, arg1 + "," + arg2 + ",", ",");
                            arg4 = getBetween(curLine, arg1 + "," + arg2 + "," + arg3 + ",", ",");
                            arg5 = getBetween(curLine, arg1 + "," + arg2 + "," + arg3 + "," + arg4 + ",", ")");

                            Array.Copy(writeValue(0xC8), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x05), 0, opArray, 1, 1);
                            Array.Copy(writeByte(arg1), 0, opArray, 2, 1);
                            Array.Copy(writeByte(arg2), 0, opArray, 3, 1);
                            Array.Copy(writeByte(arg3), 0, opArray, 4, 1);
                            Array.Copy(writeByte(arg4), 0, opArray, 5, 1);
                            Array.Copy(writeByte(arg5), 0, opArray, 6, 1);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("system.load.ui"):
                            // 0xC9
                            opArray = new byte[4];
                            arg1 = getBetween(curLine, "menuID=", ")");

                            Array.Copy(writeValue(0xC9), 0, opArray, 0, 1);
                            Array.Copy(writeValue(0x02), 0, opArray, 1, 1);
                            Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("print.ds"):
                            // 0xDD
                            arg1 = getBetween(curLine, "(\"", "\")"); byte[] jisStringDS = Encoding.GetEncoding("shift_jis").GetBytes(arg1);
                            opArray = new byte[jisStringDS.Length + 3];

                            Array.Copy(writeValue(0xDD), 0, opArray, 0, 1);
                            Array.Copy(writeValue(jisStringDS.Length + 1), 0, opArray, 1, 1);
                            Array.Copy(jisStringDS, 0, opArray, 2, jisStringDS.Length);
                            Array.Copy(writeValue(0x00), 0, opArray, jisStringDS.Length + 2, 1);
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        case string curLine when stringContains("unknown"):
                            // Unknown Opcode
                            arg0 = getBetween(curLine, "opcode=", ",");
                            arg1 = getBetween(curLine, "length=", ",");
                            arg2 = getBetween(curLine, "args[", "]");

                            if (arg0 == "04" || arg0 == "38")
                            {
                                opArray = new byte[2];
                                Array.Copy(writeHex(arg0), 0, opArray, 0, 1);
                                Array.Copy(writeByte(arg1), 0, opArray, 1, 1);
                            }
                            else if (arg0 == "D0")
                            {
                                opArray = new byte[5];
                                Array.Copy(writeHex(arg0), 0, opArray, 0, 1);
                                Array.Copy(arg2.Split('-').Select(x => byte.Parse(x, NumberStyles.HexNumber)).ToArray(), 0, opArray, 1, 4);
                            }
                            else
                            {
                                opArray = new byte[Convert.ToInt16(arg1) + 2];
                                Array.Copy(writeHex(arg0), 0, opArray, 0, 1);
                                Array.Copy(writeByte(arg1), 0, opArray, 1, 1);
                                Array.Copy(arg2.Split('-').Select(x => byte.Parse(x, NumberStyles.HexNumber)).ToArray(), 0, opArray, 2, Convert.ToInt16(arg1));
                            }
                            newScriptList.Add(opArray);
                            pointer = nexptr;
                            break;

                        default:
                            // Failsafe
                            pointer = nexptr;
                            break;
                    }
                }
            }
        }

        public static void ParseIfStatement()
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

        public static void CompileIfStatement()
        {
            opArray = new byte[3];
            Array.Copy(writeValue(0x07), 0, opArray, 0, 1);
            Array.Copy(writeValue(0x00), 0, opArray, 1, 2);
            newScriptList.Add(opArray);

            ifArray = new byte[2];
            tempScript = newScriptList.SelectMany(a => a).ToArray();
            Array.Copy(BitConverter.GetBytes(tempScript.Length - 2), 0, ifArray, 0, 2);
            if (tempAddressList.Count == 0)
            {
                tempAddressList.Add(ifArray);
            }
            else
            {
                tempAddress = tempAddressList.SelectMany(a => a).ToArray();
                byte[] loopArray = new byte[ifPointer];
                Array.Copy(tempAddress, 0, loopArray, 0, ifPointer);

                tempAddressList = new List<byte[]>();
                tempAddressList.Add(loopArray);

                tempAddressList.Add(ifArray);
            }
            ifPointer = ifPointer + 2;

            if (!curLine.Contains("||") && !curLine.Contains("unk1"))
            {
                opArray = new byte[8];
                Array.Copy(writeValue(0x01), 0, opArray, 0, 1);
                Array.Copy(writeValue(0x0A), 0, opArray, 4, 1);
            }
            else if (curLine.Contains("||"))
            {
                opArray = new byte[16];
                Array.Copy(writeValue(0x02), 0, opArray, 0, 1);
                Array.Copy(writeValue(0x0A), 0, opArray, 4, 1);
                Array.Copy(writeValue(0x14), 0, opArray, 8, 1);
            }
            else if (curLine.Contains("unk1"))
            {
                opArray = new byte[16];
                Array.Copy(writeValue(0x02), 0, opArray, 0, 1);
                Array.Copy(writeValue(0x0A), 0, opArray, 4, 1);
                Array.Copy(writeValue(0x15), 0, opArray, 8, 1);
            }

            if (curLine.Contains("(flag"))
            {
                arg1 = getBetween(curLine, "(flag[", "]");

                Array.Copy(writeValue(0x01), 0, opArray, 1, 1);
                Array.Copy(writeUInt16(arg1), 0, opArray, 2, 2);

                if (!curLine.Contains("||") && !curLine.Contains("unk1"))
                {
                    arg2 = getBetween(curLine, "==", ")");
                    Array.Copy(writeValue(0x00), 0, opArray, 5, 1);
                    Array.Copy(writeInt16(arg2), 0, opArray, 6, 2);
                }
                else if (curLine.Contains("||"))
                {
                    arg2 = getBetween(curLine, "==", "||");
                    arg3 = getBetween(curLine, "||flag[", "]");
                    arg4 = getBetween(curLine, "||flag[" + arg3 + "]==", ")");
                    Array.Copy(writeValue(0x00), 0, opArray, 5, 1);
                    Array.Copy(writeInt16(arg2), 0, opArray, 6, 2);
                    Array.Copy(writeValue(0x01), 0, opArray, 9, 2);
                    Array.Copy(writeUInt16(arg3), 0, opArray, 10, 2);
                    Array.Copy(writeValue(0x0A), 0, opArray, 12, 2);
                    Array.Copy(writeInt16(arg4), 0, opArray, 14, 2);
                }
                else if (curLine.Contains("unk1"))
                {
                    arg2 = getBetween(curLine, "==", "unk1");
                    arg3 = getBetween(curLine, "unk1flag[", "]");
                    arg4 = getBetween(curLine, "unk1flag[" + arg3 + "]==", ")");
                    Array.Copy(writeValue(0x00), 0, opArray, 5, 1);
                    Array.Copy(writeInt16(arg2), 0, opArray, 6, 2);
                    Array.Copy(writeValue(0x01), 0, opArray, 9, 2);
                    Array.Copy(writeUInt16(arg3), 0, opArray, 10, 2);
                    Array.Copy(writeValue(0x0A), 0, opArray, 12, 2);
                    Array.Copy(writeInt16(arg4), 0, opArray, 14, 2);
                }
            }
            else
            {
                arg1 = getBetween(curLine, "(", "==");
                arg2 = getBetween(curLine, "==flag[", "])");
                Array.Copy(writeValue(0x00), 0, opArray, 1, 1);
                Array.Copy(writeInt16(arg1), 0, opArray, 2, 2);
                Array.Copy(writeValue(0x01), 0, opArray, 5, 1);
                Array.Copy(writeUInt16(arg2), 0, opArray, 6, 2);
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
                    // Currently Unknown..
                    return " unk1 ";

                default:
                    // Unknown
					return " error ";
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
                    // Unknown
                    return "error";
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

                case "255":
                    return "true";

                default:
                    // Unknown
                    return "error";
            }
        }

        public static string Camera()
        {
            switch (variable)
            {
                case "0":
                    // Normal Camera (Unlock if Locked)
                    return "free";

                case "1":
                    // Lock Camera
                    return "locked";

                case "2":
                    // Currently Unknown..
                    return "unk1";

                case "3":
                    // Currently Unknown..
                    return "unk2";

                default:
                    // Unknown
                    return "error";
            }
        }

        public static string Role()
        {
            switch (variable)
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
                    // Unknown
                    return "error";
            }
        }

        public static string Stance()
        {
            switch (variable)
            {
                case "0":
                    // Animation Locked
                    return "locked";

                case "1":
                    // Combat Ready
                    return "combat";

                case "2":
                    // Neutral
                    return "neutral";

                default:
                    // Unknown
                    return "error";
            }
        }

        /*public static string Direction()
        {
            switch (variable)
            {
                case "0":
                    // North East
                    return "0°";

                case "1":
                    // South East
                    return "90°";

                case "2":
                    // South West
                    return "180°";

                case "3":
                    // North West
                    return "-90°";

                case "10":
                    // Currently Unknown..
                    return "unk1";

                default:
                    // Unknown
                    return "error";
            }
        }*/

        public static string getBetween(string strSource, string strStart, string strEnd)
        {
            if (strSource.Contains(strStart, StringComparison.OrdinalIgnoreCase) && strSource.Contains(strEnd, StringComparison.OrdinalIgnoreCase))
            {
                int Start, End;
                Start = strSource.IndexOf(strStart, 0, StringComparison.OrdinalIgnoreCase) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start, StringComparison.OrdinalIgnoreCase);
                return strSource.Substring(Start, End - Start);
            }

            return "";
        }

        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source != null && toCheck != null && source.IndexOf(toCheck, comp) >= 0;
        }
    }
}
