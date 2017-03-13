using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SwiftPbo
{
    internal static class PboUtilities
    {
        public static Encoding TextEncoding = Encoding.GetEncoding(1252);

        public static ulong ReadLong(Stream reader)
        {
            var buffer = new byte[4];
            reader.Read(buffer, 0, 4);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public static void WriteLong(Stream writer, long num)
        {
            var buffer = BitConverter.GetBytes(num);
            writer.Write(buffer, 0, 4);
        }

        public static String ReadString(Stream reader)
        {
            var str = "";
            while (true)
            {
                var ch = reader.ReadByte();
                if (ch == -1)
                    throw new Exception("Error reading string array. File too short.");
                if ((byte)ch == 0x0)
                    break;
                str += (char)ch;
            }
           
            return str;
        }

        public static void WriteString(FileStream stream, string str)
        {
            var buffer = TextEncoding.GetBytes(str + "\0");
            stream.Write(buffer, 0, buffer.Length);
        }

        public static string GetRelativePath(string filespec, string folder)
        {
            var pathUri = new Uri(filespec);

            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)))
            {
                folder += Path.DirectorySeparatorChar;
            }
            var folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        public static Byte[] ReadStringArray(Stream reader)
        {
            var list = new List<Byte>();
            while (true)
            {
                var ch =  reader.ReadByte();
                if (ch == -1)
                    throw new Exception("Error reading string array. File too short.");
                if ((byte)ch == 0x0)
                    break;
                list.Add((byte) ch);
            }
           
            return list.ToArray();
        }

        public static void WriteASIIZ(FileStream stream, Byte[] fileName)
        {
            var copy = new Byte[fileName.Count()+1];
            fileName.CopyTo(copy,0);
            copy[fileName.Length] = 0x0;
            stream.Write(copy, 0, copy.Length);
        }
    }
}