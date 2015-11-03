using System;
using System.IO;

namespace SwiftPbo
{
    [Serializable]
    
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

        [NonSerialized]
        private readonly PboArchive _parentArchive;
        public FileEntry(PboArchive parent,string filename, ulong type, ulong osize, ulong timestamp, ulong datasize, ulong unknown = 0x0)
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
        public FileEntry(string filename, ulong type, ulong osize, ulong timestamp, ulong datasize, ulong unknown = 0x0)
        {
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
            if(_parentArchive == null)
                throw  new Exception("No parent Archive");
            if (!Directory.Exists(Path.GetDirectoryName(outpath)))
                Directory.CreateDirectory(Path.GetDirectoryName(outpath));
            return _parentArchive.Extract(this, outpath);
        }

        public Stream Extract()
        {
            if (_parentArchive == null)
                throw new Exception("No parent Archive");
            return _parentArchive.Extract(this);
        }
    }
}