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
        static string arg8;
        static string op1;
        static string op2;
        static string op3;
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
                    if (opcode != 0)
                    {
                        opcodeLen = fileData[pointer + 1];
                    }
                    else
                    {
                        opcodeLen = fileData[fileDataLength - pointer];
                    }
                    argCount = opcodeLen;

                    int nexptr = pointer + 2 + opcodeLen;

                    switch (opcode)
                    {
                        case 0x00:
                            // Padding
                            pointer = nexptr;
                            break;

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
                            // The Unknown is always set to 0x00 in retail scripts.
                            unk = fileData[pointer + 2].ToString();
                            varCount = fileData[pointer + 3];
                            IfStatement(); indent = "\t\t";
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
                            indent = "\t";
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
                            sw.WriteLine(indent + "input.wait(timer = " + arg2 + ");");
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
                            arg1 = fileData[pointer + 2].ToString();
                            arg2 = parseInt16(fileData.Skip(pointer + 3).Take(2).ToArray());
                            arg3 = Convert.ToInt32(fileData[pointer + 7] | (fileData[pointer + 6] << 8) | (fileData[pointer + 5] << 16)).ToString("X2").PadLeft(6, '0');
                            arg4 = fileData[pointer + 8].ToString();
                            arg5 = parseInt16(fileData.Skip(pointer + 9).Take(2).ToArray());
                            arg6 = parseInt16(fileData.Skip(pointer + 11).Take(2).ToArray());
                            arg7 = parseInt16(fileData.Skip(pointer + 13).Take(2).ToArray());
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
                            pointer = nexptr;
                            sw.WriteLine(indent + "camera.pan(pos.x = " + arg2 + ", pos.y = " + arg3 + ", pos.offset = " + arg1 + ", timer = " + arg4 + ");");
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
                            sw.WriteLine(indent + "actor.pos(entity = " + arg1 + ", pos.x = " + arg3 + ", pos.y = " + arg5 + ", pos.z = " + arg4 + ", pos.offset = " + arg2 + ", timer = " + arg6 + ", " + arg7 + ");");
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
                            arg3 = fileData[pointer + 6].ToString(); variable = arg3; arg3 = Direction();
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
                            arg3 = fileData[pointer + 6].ToString(); variable = arg3; arg3 = Direction();
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
                            arg2 = parseTime(fileData.Skip(pointer + 2).Take(2).ToArray());
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
                            sw.WriteLine(indent + "actor.talk(entity = " + arg1 + ", talkID = " + arg2 + ";");
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

        public static string Direction()
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
        }
    }
}
