using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SwiftPbo
{
    public class ProductEntry
    {
        private String _prefix;
        private String _productName;
        private String _productVersion;
        private List<string> _addtional = new List<string>();

        public ProductEntry(string prefix, string productName, string productVersion, List<string> addList = null)
        {
            Prefix = prefix;
            ProductName = productName;
            ProductVersion = productVersion;
            if (addList != null)
                Addtional = addList;
        }

        public string Prefix
        {
            get { return _prefix; }
            set { _prefix = value; }
        }

        public string ProductName
        {
            get { return _productName; }
            set { _productName = value; }
        }

        public string ProductVersion
        {
            get { return _productVersion; }
            set { _productVersion = value; }
        }

        public List<string> Addtional
        {
            get { return _addtional; }
            set { _addtional = value; }
        }
    }

    public enum PackingType
    {
        Uncompressed,
        Packed
    };

    public class PboArchive
    {
        private ProductEntry _productEntry = new ProductEntry("", "", "", new List<string>());
        private List<FileEntry> _files = new List<FileEntry>();
        private string _path;
        private Boolean _loaded = false;
        private long _dataStart = 0;
        private MemoryStream _memory;
        private byte[] _checksum;

        public static Boolean Create(string directoryPath, string outpath, ProductEntry productEntry)
        {
            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            var entries = new List<FileEntry>();
            foreach (var file in files)
            {
                var info = new FileInfo(file);
                var path = PboUtilities.GetRelativePath(info.FullName, directoryPath);
                var entry = new FileEntry(path, 0x0, 0x0,
                    (ulong)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds, (ulong)info.Length);
                entries.Add(entry);
            }
            try
            {
                using (var stream = File.Create(outpath))
                {
                    stream.WriteByte(0x0);
                    WriteProductEntry(productEntry, stream);
                    stream.WriteByte(0x0);
                    entries.Add(new FileEntry(null, "", 0, 0, 0, 0, 0));
                    foreach (var entry in entries)
                    {
                        WriteFileEntry(stream, entry);
                    }
                    entries.Remove(entries.Last());
                    foreach (var entry in entries)
                    {
                        var buffer = new byte[1024];
                        using (var open = File.OpenRead(Path.Combine(directoryPath, entry.FileName)))
                        {
                            var read = 4324324;
                            while (read > 0)
                            {
                                read = open.Read(buffer, 0, buffer.Length);
                                stream.Write(buffer, 0, read);
                            }
                        }
                    }
                    stream.Position = 0;
                    byte[] hash;
                    using (var sha1 = new SHA1Managed())
                    {
                        hash = sha1.ComputeHash(stream);
                    };
                    stream.WriteByte(0x0);
                    stream.Write(hash, 0, 20);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            return true;
        }

        public static void Create(string path, ProductEntry productEntry, Dictionary<FileEntry, string> files, Byte[] checksum)
        {
            try
            {
                using (var stream = File.Create(path))
                {
                    stream.WriteByte(0x0);
                    WriteProductEntry(productEntry, stream);
                    stream.WriteByte(0x0);
                    files.Add(new FileEntry(null, "", 0, 0, 0, 0, 0), "");
                    foreach (var entry in files.Keys)
                    {
                        WriteFileEntry(stream, entry);
                    }
                    files.Remove(files.Last().Key);
                    foreach (var file in files.Values)
                    {
                        var buffer = new byte[1024];
                        using (var open = File.OpenRead(file))
                        {
                            var read = 4324324;
                            while (read > 0)
                            {
                                read = open.Read(buffer, 0, buffer.Length);
                                stream.Write(buffer, 0, read);
                            }
                        }
                    }
                    stream.WriteByte(0x0);
                    stream.Write(checksum, 0, checksum.Length);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static void WriteFileEntry(FileStream stream, FileEntry entry)
        {
            PboUtilities.WriteString(stream, entry.FileName);
            long packing = 0x0;
            switch (entry.PackingMethod)
            {
                case PackingType.Packed:
                    packing = 0x43707273;
                    break;
            }
            PboUtilities.WriteLong(stream, packing);
            PboUtilities.WriteLong(stream, (long)entry.OriginalSize);
            PboUtilities.WriteLong(stream, 0x0); // reserved
            PboUtilities.WriteLong(stream, (long)entry.TimeStamp);
            PboUtilities.WriteLong(stream, (long)entry.DataSize);
        }

        private static void WriteProductEntry(ProductEntry productEntry, FileStream stream)
        {
            PboUtilities.WriteString(stream, "sreV");
            stream.Write(new byte[15], 0, 15);
            if (!String.IsNullOrEmpty(productEntry.Prefix))
                PboUtilities.WriteString(stream, productEntry.Prefix);
            else
                return;
            if (!String.IsNullOrEmpty(productEntry.ProductName))
                PboUtilities.WriteString(stream, productEntry.ProductName);
            else
                return;
            if (!String.IsNullOrEmpty(productEntry.ProductVersion))
                PboUtilities.WriteString(stream, productEntry.ProductVersion);
            else
                return;
            foreach (var str in productEntry.Addtional)
            {
                PboUtilities.WriteString(stream, str);
            }
        }

        public PboArchive(String path, Boolean loadIntoMemory = false)
        {
            _loaded = loadIntoMemory;
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
                    ReadChecksum(stream);
                    if (!loadIntoMemory) return;
                    long length = stream.Length - (_dataStart + 20);
                    var buffer = new byte[length];
                    stream.Read(buffer, 0, (int)length);
                    _memory = new MemoryStream(buffer, true);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void ReadChecksum(FileStream stream)
        {
            var pos = _dataStart + Files.Sum(fileEntry => (long)fileEntry.DataSize) + 1;
            stream.Position = pos;
            _checksum = new byte[20];
            stream.Read(Checksum, 0, 20);
            stream.Position = _dataStart;
        }

        public List<FileEntry> Files
        {
            get { return _files; }
        }

        public ProductEntry ProductEntry
        {
            get { return _productEntry; }
        }

        public byte[] Checksum
        {
            get { return _checksum; }
        }

        public string PboPath
        {
            get { return _path; }
        }

        private bool ReadEntry(FileStream stream)
        {
            var filename = PboUtilities.ReadString(stream);

            var packing = PboUtilities.ReadLong(stream);

            var size = PboUtilities.ReadLong(stream);

            var unknown = PboUtilities.ReadLong(stream);

            var timestamp = PboUtilities.ReadLong(stream);
            var datasize = PboUtilities.ReadLong(stream);
            var entry = new FileEntry(this, filename, packing, size, timestamp, datasize, unknown);
            if (entry.FileName == "") return false;
            Files.Add(entry);
            return true;
        }

        private Boolean ReadHeader(FileStream stream)
        {
            // TODO FIX SO BROKEN
            var str = PboUtilities.ReadString(stream);
            if (str != "sreV")
                return false;
            int count = 0;
            while (count < 15)
            {
                stream.ReadByte();
                count++;
            }
            var prefix = "";
            var list = new List<string>();
            var pboname = "";
            var version = "";
            prefix = PboUtilities.ReadString(stream);
            if (!String.IsNullOrEmpty(prefix))
            {
                pboname = PboUtilities.ReadString(stream);
                if (!String.IsNullOrEmpty(pboname))
                {
                    version = PboUtilities.ReadString(stream);

                    if (!String.IsNullOrEmpty(version))
                    {
                        while (stream.ReadByte() != 0x0)
                        {
                            stream.Position--;
                            var s = PboUtilities.ReadString(stream);
                            list.Add(s);
                        }
                    }
                }
            }
            _productEntry = new ProductEntry(prefix, pboname, version, list);

            return true;
        }

        public Boolean ExtractAll(string outpath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(outpath)))
                Directory.CreateDirectory(Path.GetDirectoryName(outpath));
            return _files.All(entry => Extract(entry, Path.Combine(outpath, entry.FileName)));
        }

        public Boolean Extract(FileEntry fileEntry, string outpath)
        {
            Stream mem = GetFileStream(fileEntry);
            if (mem == null)
                throw new Exception("WTF no stream");
            try
            {
                try
                {
                    if (!Directory.Exists(Path.GetDirectoryName(outpath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(outpath));
                    using (var write = File.Create(outpath))
                    {
                        var buffer = new byte[fileEntry.DataSize];
                        mem.Read(buffer, 0, buffer.Length);
                        write.Write(buffer, 0, buffer.Length);
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
            catch (Exception)
            {
                throw;
            }
            mem.Close();
            return true;
        }

        private Stream GetFileStream(FileEntry fileEntry)
        {
            Stream mem;
            if (_memory != null)
                mem = ExtractMemory(fileEntry);
            else
            {
                mem = File.OpenRead(PboPath);
                mem.Position = (long)GetFileStreamPos(fileEntry);
            }
            return mem;
        }

        private ulong GetFileStreamPos(FileEntry fileEntry)
        {
            ulong start = (ulong)_dataStart;
            foreach (var entry in Files)
            {
                if (entry == fileEntry)
                    break;
                start += entry.DataSize;
            }
            return start;
        }

        private long GetFileMemPos(FileEntry fileEntry)
        {
            ulong start = 0;
            foreach (var entry in Files)
            {
                if (entry == fileEntry)
                    break;
                start += entry.DataSize;
            }
            return (long)start;
        }

        private Stream ExtractMemory(FileEntry fileEntry)
        {
            var buffer = new byte[fileEntry.DataSize];
            _memory.Position = GetFileMemPos(fileEntry);
            _memory.Read(buffer, 0, buffer.Length);
            _memory.Position = 0;
            return new MemoryStream(buffer);
        }

        // returns a stream
        public Stream Extract(FileEntry fileEntry)
        {
            var stream = GetFileStream(fileEntry);
            if (stream is MemoryStream)
                return stream;
            using (stream)
            {
                var mem = new MemoryStream((int)fileEntry.DataSize);
                var buffer = new byte[fileEntry.DataSize];
                stream.Read(buffer, 0, buffer.Length);
                mem.Write(buffer, 0, buffer.Length);
                mem.Position = 0;
                return mem;
            }
        }
    }

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
            Uri pathUri = new Uri(filespec);

            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }
    }
}