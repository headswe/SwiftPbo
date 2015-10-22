using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace SwiftPbo
{
    internal static class PboUtilities
    {
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
                var ch = (char)reader.ReadByte();
                if (ch == 0x0)
                    break;
                str += ch.ToString(CultureInfo.InvariantCulture);
            }
            return str;
        }

        public static void WriteString(FileStream stream, string str)
        {
            var buffer = Encoding.ASCII.GetBytes(str + "\0");
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
    }
}