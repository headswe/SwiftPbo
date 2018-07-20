using System;
using System.IO;

namespace SwiftPbo
{
    [Serializable]
    
    public class FileEntry
    {
        public String _fileName;
        
        public PackingType PackingMethod = PackingType.Uncompressed;
        
        public ulong OriginalSize;
        
        public ulong TimeStamp;
       
        public ulong Unknown;
        
        public ulong DataSize;

        public string FileName
        {
            get
            {
                return _fileName;
            }
            set { _fileName = value; }
        }

        public override string ToString()
        {
            return String.Format("{0} ({1})", _fileName, OriginalSize);
        }

        [NonSerialized]
        public readonly PboArchive ParentArchive;

        public byte[] OrgName;

        public FileEntry(PboArchive parent, string filename, ulong type, ulong osize, ulong timestamp, ulong datasize, byte[] file, ulong unknown = 0x0)
        {
            _parentArchive = parent;
            _fileName = filename;
            OrgName = file;
            switch (type)
            {
                case 0x43707273:
                    PackingMethod = PackingType.Packed;
                    break;
                case 0x0:
                    PackingMethod = PackingType.Uncompressed;
                    break;
                case 0x56657273:
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
            _fileName = filename;
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
            if(ParentArchive == null)
                throw  new Exception("No parent Archive");
            if (!Directory.Exists(Path.GetDirectoryName(outpath)))
                Directory.CreateDirectory(Path.GetDirectoryName(outpath));
            return ParentArchive.Extract(this, outpath);
        }

        public Stream Extract()
        {
            if (ParentArchive == null)
                throw new Exception("No parent Archive");
            return ParentArchive.Extract(this);
        }
    }
}