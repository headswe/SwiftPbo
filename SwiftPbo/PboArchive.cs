using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SwiftPbo
{
    class ProductEntry
    {
        private String _prefix;
        private String _productName;
        private String _productVersion;
        private List<string> _addtional = new List<string>(); 
        public ProductEntry(string prefix, string productName, string productVersion,List<string> addList )
        {
            _prefix = prefix;
            _productName = productName;
            _productVersion = productVersion;
            _addtional = addList;
        }
    }
    public enum PackingType
    {
        Uncompressed,
        Packed
    };
    public class FileEntry
    {
        public String FileName;
        public PackingType PackingMethod = PackingType.Uncompressed;
        public ulong OriginalSize;
        public ulong TimeStamp;
        public ulong Unknown;
        public ulong DataSize;
        public override string ToString()
        {
            return String.Format("{0} ({1})", FileName, OriginalSize);
        }

        private PboArchive _parentArchive;
        public FileEntry(PboArchive parent,string filename, ulong type, ulong osize, ulong timestamp, ulong datasize, ulong unknown)
        {
            _parentArchive = parent;
            FileName = filename;
            switch (type)
            {
                case 0x43707273:
                    PackingMethod = PackingType.Packed;
                    break;
                case 0x0:
                    PackingMethod = PackingType.Uncompressed;
                    break;
            }
            OriginalSize = osize;
            TimeStamp = timestamp;
            DataSize = datasize;
            Unknown = unknown;
        }

        public Boolean Extract(string outpath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(outpath)))
                Directory.CreateDirectory(Path.GetDirectoryName(outpath));
            return _parentArchive.Extract(this, outpath);
        }
    }

    public class PboArchive
    {
        private ProductEntry _productEntry;
        private List<FileEntry> _files = new List<FileEntry>();
        private string _path;
        private Boolean loaded = false;
        private long _dataStart = 0;
        private MemoryStream _memory;

        public PboArchive(String path,Boolean loadIntoMemory = false)
        {
            loaded = loadIntoMemory;
            if (!File.Exists(path))
                throw new FileNotFoundException("File not Found");
            _path = path;
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    if (stream.ReadByte() != 0x0)
                        return;
                    if (!ReadHeader(stream))
                        stream.Position = 0;
                    while (true)
                    {
                        if (!ReadEntry(stream))
                            break;
                    }
                    _dataStart = stream.Position;
                    if (!loadIntoMemory) return;
                    long length = stream.Length - (_dataStart+21);
                    var buffer = new byte[length];
                    stream.Read(buffer, 0, (int) length);
                    _memory = new MemoryStream(buffer,true);
                }
            }
            catch (Exception)
            {
                
                throw;
            }
        }

        public List<FileEntry> Files
        {
            get { return _files; }
        }

        private bool ReadEntry(FileStream stream)
        {
            var filename = PboUtilities.ReadString(stream);


            var packing = PboUtilities.ReadLong(stream);

            var size = PboUtilities.ReadLong(stream);

            var unknown = PboUtilities.ReadLong(stream);

            var timestamp = PboUtilities.ReadLong(stream);
            var datasize = PboUtilities.ReadLong(stream);
            var entry = new FileEntry(this,filename, packing, size, timestamp, datasize, unknown);
            if (entry.FileName == "") return false;
            Files.Add(entry);
            return true;
        }

        private Boolean ReadHeader(FileStream stream)
        {
            var str = PboUtilities.ReadString(stream);
            if (str != "sreV")
                return false;
            var ch = 0x0;
            while (ch == 0x0)
            {
                ch = stream.ReadByte();
            }
            var prefix = ((char)ch) + PboUtilities.ReadString(stream);
            var pboname = PboUtilities.ReadString(stream);
            var version = PboUtilities.ReadString(stream);
            var list = new List<string>();
            while (stream.ReadByte() != 0x0)
            {
                stream.Position--;
                var s = PboUtilities.ReadString(stream);
                list.Add(s);
            }
           /* var oldpos = stream.Position;
            var s = PboUtilities.ReadString(stream);
            if (s.ToLower() == "depbo.dll")
                stream.ReadByte();
            else
            {
                stream.Position = oldpos + 1;
            }*/
            _productEntry = new ProductEntry(prefix,pboname,version,list); 
            return true;
        }

        public Boolean Extract(FileEntry fileEntry, string outpath)
        {
            if (_memory != null)
                return ExtractMemory(fileEntry, outpath);
            ulong start = (ulong) _dataStart;
            foreach (var entry in Files)
            {
                if (entry == fileEntry)
                    break;
                start += entry.DataSize;
            }
            try
            {
                using (var stream = File.OpenRead(_path))
                {
                    stream.Position = (long) start;
                    try
                    {
                        using (var write = File.Create(outpath))
                        {
                            var buffer = new byte[fileEntry.DataSize];
                            stream.Read(buffer, 0, (int) fileEntry.DataSize);
                            write.Write(buffer,0,buffer.Length);
                        }
                    }
                    catch (Exception)
                    {
                        
                        throw;
                    }
                }
            }
            catch (Exception)
            {
                
                throw;
            }
            return true;
        }

        private bool ExtractMemory(FileEntry fileEntry, string outpath)
        {
            throw new NotImplementedException();
        }
    }

    static class PboUtilities
    {
        public static ulong ReadLong(Stream reader)
        {
            var buffer = new byte[4];
            reader.Read(buffer, 0, 4);
            return BitConverter.ToUInt32(buffer, 0);
        }
        public static String ReadString(Stream reader)
        {
            var str = "";
            while (true)
            {
                var ch = (char) reader.ReadByte();
                if (ch == 0x0)
                    break;
                str += ch.ToString(CultureInfo.InvariantCulture);
            }
            return str;
        }
    }
}
