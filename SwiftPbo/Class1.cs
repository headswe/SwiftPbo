using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;

namespace SwiftPbo
{
    public class Pbo
    {
        private String _prefix;
        private String _productName;
        private String _productVersion;
        private String pbopath;
        private bool readHeader = false;
        private List<PboEntry> _entries = new List<PboEntry>();
        private long datapostion = 0;
        public Pbo()
        {
            
        }

        public List<PboEntry> Entries
        {
            get { return _entries; }
        }

        public void Load(String path)
        {
            pbopath = path;
            var reader = File.OpenRead(path);
            reader.ReadByte();
            ReadHeader(reader);
            while (true)
            {
                var entry = ReadEntry(reader);
                if (entry.FileName == "")
                    break;
                Entries.Add(entry);
            };
            datapostion = reader.Position;
            reader.Close();
        }

        public void ExtractEntry(PboEntry entry,string path)
        {
            bool isBin = Path.GetExtension(entry.FileName) == ".bin";
            var index = _entries.IndexOf(entry);
            var reader = File.OpenRead(pbopath);
            reader.Position = datapostion;
            for (int i = 0; i < index; i++)
            {
                reader.Position += (long)_entries[i].DataSize;
            }
            var file = File.OpenWrite(path);
            var read = 0;
            while (read < (int) entry.DataSize)
            {
                var buffer = new byte[512];
                if ((int) entry.DataSize < buffer.Length)
                    buffer = new byte[entry.DataSize];
                read += reader.Read(buffer, 0, buffer.Length);
                if (isBin)
                    buffer = Unbinarize(buffer);
                file.Write(buffer,0,buffer.Length);
            }
            reader.Close();
            file.Close();

        }

        private byte[] Unbinarize(byte[] buffer)
        {
            return buffer;
        }

        private PboEntry ReadEntry(FileStream reader)
        {
            var filename = ReadString(reader);


            var packing = ReadLong(reader);

            var size = ReadLong(reader);

            ReadLong(reader);

            var timestamp = ReadLong(reader);
            var datasize = ReadLong(reader);
            return new PboEntry() {FileName = filename, OriginalSize = size, DataSize = datasize, TimeStamp = timestamp};
        }

        private ulong ReadLong(FileStream reader)
        {
            byte[] buffer = new byte[4];
            reader.Read(buffer, 0, 4);
            return BitConverter.ToUInt32(buffer, 0);
        }

        private void ReadHeader(FileStream reader)
        {
            char c = (char) reader.ReadByte();
            var str = c.ToString(CultureInfo.InvariantCulture);
            str += ReadString(reader);
            if (str != "sreV")
            {
                reader.Position = 0;
                return;
                // throw new Exception("No sreV");
            }
                
            reader.Read(new byte[15], 0, 15); // read the 16 seperator
            var prefix = ReadString(reader);
            if(prefix != "prefix")
                throw new Exception("No prefix");




            // Read Prefix
            _prefix = ReadString(reader);

            _productName = ReadString(reader);

            _productVersion = ReadString(reader);
            reader.ReadByte();
        }



        private static string ReadString(FileStream reader)
        {
            var strin = "";
            while (true)
            {
                char ch = '\0';
                ch = (char) reader.ReadByte();
                if (ch == '\0')
                    break;
                strin += ch;
            }
            return strin;
        }
    }

    public enum PackingType
    {
        Uncompressed,
        Packed
    };
    public class PboEntry
    {
        public String FileName;
        public PackingType PackingMethod = PackingType.Uncompressed;
        public ulong OriginalSize;
        public ulong TimeStamp;
        public ulong DataSize;
        public override string ToString()
        {
            return String.Format("{0} ({1})", FileName, OriginalSize);
        }
    }

}
