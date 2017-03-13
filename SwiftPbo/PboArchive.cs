using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SwiftPbo
{
    public enum PackingType
    {
        Uncompressed,
        Packed
    };

    public class PboArchive : IDisposable
    {
        private ProductEntry _productEntry = new ProductEntry("", "", "", new List<string>());
        private List<FileEntry> _files = new List<FileEntry>();
        private string _path;
        private long _dataStart;
        private FileStream _stream;
        private byte[] _checksum;

        private static readonly List<char> InvaildFile = Path.GetInvalidFileNameChars().ToList();

        public static Boolean Create(string directoryPath, string outpath)
        {
            var dir = new DirectoryInfo(directoryPath);
            if (!dir.Exists)
                throw new DirectoryNotFoundException();
            directoryPath = dir.FullName;
            var entry = new ProductEntry("prefix","","",new List<string>());
            var files = Directory.GetFiles(directoryPath, "$*$");
            foreach (var file in files)
            {
                var varname = Path.GetFileNameWithoutExtension(file).Trim('$');
                var data = File.ReadAllText(file).Split('\n')[0];
                switch (varname.ToLowerInvariant())
                {
                    case "pboprefix":
                        entry.Prefix = data;
                        break;
                    case "prefix":
                        entry.Prefix = data;
                        break;
                    case "version":
                        entry.ProductVersion = data;
                        break;
                    default:
                        entry.Addtional.Add(data);
                        break;
                }
            }
            return Create(directoryPath, outpath, entry);
        }
        public static Boolean Create(string directoryPath, string outpath, ProductEntry productEntry)
        {
            var dir = new DirectoryInfo(directoryPath);
            if (!dir.Exists)
                throw new DirectoryNotFoundException();
            directoryPath = dir.FullName;
            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            var entries = new List<FileEntry>();
            foreach (string file in files)
            {
                if(Path.GetFileName(file).StartsWith("$") && Path.GetFileName(file).EndsWith("$"))
                    continue;
                FileInfo info = new FileInfo(file);
                string path = PboUtilities.GetRelativePath(info.FullName, directoryPath);
                entries.Add(new FileEntry(path, 0x0, (ulong) info.Length, (ulong) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds, (ulong) info.Length));
            }
            try
            {
                using (var stream = File.Create(outpath))
                {
                    stream.WriteByte(0x0);
                    WriteProductEntry(productEntry, stream);
                    stream.WriteByte(0x0);
                    entries.Add(new FileEntry(null, "", 0, 0, 0, 0, _file));
                    foreach (var entry in entries)
                    {
                        WriteFileEntry(stream, entry);
                    }
                    entries.Remove(entries.Last());
                    foreach (var entry in entries)
                    {
                        var buffer = new byte[2949120];
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
                    }
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

        private static readonly List<Char> InvaildPath = Path.GetInvalidPathChars().ToList();
        private static byte[] _file;

        public static String SterilizePath(String path)
        {
            
            var arr = Path.GetDirectoryName(path).ToCharArray();
            var builder = new StringBuilder(arr.Count());
            string dirpath = Path.GetDirectoryName(path);
            for (int i = 0; i < dirpath.Length; i++)
            {
                if (!InvaildPath.Contains(path[i]) && path[i] != Path.AltDirectorySeparatorChar)
                    builder.Append(path[i]);
                if(path[i] == Path.AltDirectorySeparatorChar)
                    builder.Append(Path.DirectorySeparatorChar);
            }
            var filename = Path.GetFileName(path).ToCharArray();
            for (int i = 0; i < filename.Length; i++)
            {
                var ch = filename[i];
                if (!InvaildFile.Contains(ch) && ch != '*' && !IsLiteral(ch))
                {
                    continue;
                }
                filename[i] = ((Char)(Math.Min(90, 65 + ch % 5)));
            }
            return Path.Combine(builder.ToString(), new string(filename));
        }

        private static List<char> _literalList = new List<char>() {'\'','\"','\\','\0','\a','\b','\f','\n','\r','\t','\v'};
        private static bool IsLiteral(char ch)
        {
            return _literalList.Contains(ch);
        }

        public static void Clone(string path, ProductEntry productEntry, Dictionary<FileEntry, string> files, byte[] checksum = null)
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(path)) && !String.IsNullOrEmpty(Path.GetDirectoryName(path)))
                    Directory.CreateDirectory(Path.GetDirectoryName(path) );
                using (var stream = File.Create(path))
                {
                    stream.WriteByte(0x0);
                    WriteProductEntry(productEntry, stream);
                    stream.WriteByte(0x0);
                    files.Add(new FileEntry(null, "", 0, 0, 0, 0, _file), "");
                    foreach (var entry in files.Keys)
                    {
                        WriteFileEntry(stream, entry);
                    }
                    files.Remove(files.Last().Key);
                    foreach (var file in files.Values)
                    {
                        var buffer = new byte[2949120];
                        using (var open = File.OpenRead(file))
                        {
                            int bytesRead;
                            while ((bytesRead =
                                         open.Read(buffer, 0, 2949120)) > 0)
                            {
                                stream.Write(buffer, 0, bytesRead);
                            }
                        }
                    }
                    if(checksum != null && checksum.Any(b => b!=0))
                    {
                        stream.WriteByte(0x0);
                        stream.Write(checksum, 0, checksum.Length);
                    }
                    else if (checksum == null)
                    {
                        stream.Position = 0;
                        byte[] hash;
                        using (var sha1 = new SHA1Managed())
                        {
                            hash = sha1.ComputeHash(stream);
                        }
                        stream.WriteByte(0x0);
                        stream.Write(hash, 0, 20);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static void WriteFileEntry(FileStream stream, FileEntry entry)
        {
            if (entry.OrgName != null)
                PboUtilities.WriteASIIZ(stream, entry.OrgName);
            else
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
            if (!String.IsNullOrEmpty(productEntry.Name))
                PboUtilities.WriteString(stream, productEntry.Name);
            else
                return;
            if (!String.IsNullOrEmpty(productEntry.Prefix))
                PboUtilities.WriteString(stream, productEntry.Prefix);
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

        public PboArchive(string path, bool close = true)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("File not Found");
            _path = path;
            _stream = new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,8,FileOptions.SequentialScan);
            if (_stream.ReadByte() != 0x0)
                return;
            if (!ReadHeader(_stream))
                _stream.Position = 0;
            while (true)
            {
                if (!ReadEntry(_stream))
                    break;
            }
            _dataStart = _stream.Position;
            ReadChecksum(_stream);
            if (close)
            {
                _stream.Dispose();
                _stream = null;
            }
            
        }

        private void ReadChecksum(FileStream stream)
        {
            var pos = DataStart + Files.Sum(fileEntry => (long)fileEntry.DataSize) + 1;
            stream.Position = pos;
            _checksum = new byte[20];
            stream.Read(Checksum, 0, 20);
            stream.Position = DataStart;
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

        public long DataStart
        {
            get { return _dataStart; }
        }

        private bool ReadEntry(FileStream stream)
        {
            var file = PboUtilities.ReadStringArray(stream);
            var filename = Encoding.Default.GetString(file).Replace("\t", "\\t");

            var packing = PboUtilities.ReadLong(stream);

            var size = PboUtilities.ReadLong(stream);

            var unknown = PboUtilities.ReadLong(stream);

            var timestamp = PboUtilities.ReadLong(stream);
            var datasize = PboUtilities.ReadLong(stream);
            var entry = new FileEntry(this, filename, packing, size, timestamp, datasize, file, unknown);
            if (entry.FileName == "")
            {
                entry.OrgName = new byte[0];
                return false;
            }
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
            if (!Directory.Exists(outpath))
                Directory.CreateDirectory(outpath);
                var buffer = new byte[10000000];
                int files = 0;
                foreach (var file in Files)
                {
                    var stream = GetFileStream(file);
                    
                    Console.WriteLine("FILE START");
                    files++;
                    long totalread = (long)file.DataSize;
                    var pboPath =
                        SterilizePath(Path.Combine(outpath, file.FileName));
                    if (!Directory.Exists(Path.GetDirectoryName(pboPath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(pboPath));
                    using (var outfile = File.Create(pboPath))
                    {
                        while (totalread > 0)
                        {
                            var read = stream.Read(buffer, 0, (int)Math.Min(10000000, totalread));
                            if (read <= 0)
                                return true;
                            outfile.Write(buffer, 0, read);
                            totalread -= (long)read;
                        }
                    }
                    Console.WriteLine("FILE END " + files);
                    if (_stream == null)
                        stream.Close();
                }

            return true;
        }

        public Boolean Extract(FileEntry fileEntry, string outpath)
        {
            if(String.IsNullOrEmpty(outpath))
                throw new NullReferenceException("Is null or empty");
            Stream mem = GetFileStream(fileEntry);
            if (mem == null)
                throw new Exception("WTF no stream");
            if (!Directory.Exists(Path.GetDirectoryName(outpath)))
                Directory.CreateDirectory(Path.GetDirectoryName(outpath));
            var totalread = fileEntry.DataSize;
            using (var outfile = File.OpenWrite(outpath))
            {
                var buffer = new byte[2949120];
                while (totalread > 0)
                {
                    var read = mem.Read(buffer, 0, (int)Math.Min(2949120, totalread));
                    outfile.Write(buffer, 0, read);
                    totalread -= (ulong)read;
                }
            }
            mem.Close();
            return true;
        }

        private Stream GetFileStream(FileEntry fileEntry)
        {
            if (_stream != null)
            {
                _stream.Position = (long)GetFileStreamPos(fileEntry);
                return _stream;
            }
            var mem = File.OpenRead(PboPath);
            mem.Position = (long)GetFileStreamPos(fileEntry);
            return mem;
        }

        private ulong GetFileStreamPos(FileEntry fileEntry)
        {

            var start = (ulong)DataStart;
            return Files.TakeWhile(entry => entry != fileEntry).Aggregate(start, (current, entry) => current + entry.DataSize);
        }




        
        // returns a stream
        /// <summary>
        /// Returns a filestream to the ENTIRE pbo set at the file entry pos.
        /// </summary>
        /// <param name="fileEntry"></param>
        /// <returns></returns>
        public Stream Extract(FileEntry fileEntry)
        {
            return GetFileStream(fileEntry);
        }

        public void Dispose()
        {
            if(_stream != null)
                _stream.Dispose();
        }
    }
}
