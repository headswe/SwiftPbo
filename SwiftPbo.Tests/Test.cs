using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SwiftPbo.Tests
{
    [TestFixture]
    class PboTest
    {
        public static bool IsLinux
        {
            get
            {
                int p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
        public static string TestFolder
        {
            get
            {
                return IsLinux ? Path.Combine(AssemblyDirectory, "testdata/") : Path.Combine(AssemblyDirectory, "testdata\\");
            }
        }
        private byte[] _checksum;
        [SetUp]
        protected void SetUp()
        {

            const string Sha = "2DEA9A198FDCF0FE70473C079F1036B6E16FBFCE";
            _checksum = Enumerable.Range(0, Sha.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(Sha.Substring(x, 2), 16))
                .ToArray();
        }

        [Test]
        public void OpenArchiveTest()
        {
            var pboArchive = new PboArchive(Path.Combine(TestFolder, "cba_common.pbo"));
            Assert.That(pboArchive.Files.Count == 113);



            Assert.That(pboArchive.Checksum.SequenceEqual(_checksum), "Checksum dosen't match");

            Assert.That(pboArchive.ProductEntry.Name == "prefix");

            Assert.That(pboArchive.ProductEntry.Prefix == @"x\cba\addons\common");

            Assert.That(pboArchive.ProductEntry.Addtional.Count == 3);
        }

        [Test]
        public void CreateArchiveTest()
        {
            var outFolder = Path.Combine(TestFolder, "out1");
            if (!Directory.Exists(outFolder))
                Directory.CreateDirectory(outFolder);
            Assert.That(PboArchive.Create(Path.Combine(TestFolder, "cba_common"), Path.Combine(outFolder, "cba_common.pbo")));

            var pbo = new PboArchive(Path.Combine(outFolder, "cba_common.pbo"));

            Assert.That(pbo.Files.Count == 113);

            // checksums shoulden't match due to the time.
            Assert.False(pbo.Checksum.SequenceEqual(_checksum), "Checksum match");

            Assert.That(pbo.ProductEntry.Name == "prefix");

            Assert.That(pbo.ProductEntry.Prefix == @"x\cba\addons\common");

            Assert.That(pbo.ProductEntry.Addtional.Count == 1); // i don't add wonky shit like mikero.
        }

        [Test]
        public void CloneArchiveTest()
        {
            var pboArchive = new PboArchive(Path.Combine(TestFolder, "cba_common.pbo"));
            pboArchive.ExtractAll(Path.Combine(TestFolder, "cba_common"));
            var files = new Dictionary<FileEntry, string>();

            foreach (var entry in pboArchive.Files)
            {
                var info = new FileInfo(Path.Combine(TestFolder, "cba_common", entry.FileName));
                Assert.That(info.Exists);
                files.Add(entry, info.FullName);
            }



            PboArchive.Clone(Path.Combine(TestFolder, "clone_common.pbo"), pboArchive.ProductEntry, files, pboArchive.Checksum);

            var cloneArchive = new PboArchive(Path.Combine(TestFolder, "clone_common.pbo"));

            Assert.That(pboArchive.Checksum.SequenceEqual(cloneArchive.Checksum), "Checksum dosen't match");

            Assert.That(pboArchive.Files.Count == cloneArchive.Files.Count, "Checksum dosen't match");

            Assert.That(pboArchive.ProductEntry.Name == cloneArchive.ProductEntry.Name, "Product name doesn't match ( " + pboArchive.ProductEntry.Name + " != " + cloneArchive.ProductEntry.Name + " )");

            Assert.That(pboArchive.ProductEntry.Prefix == cloneArchive.ProductEntry.Prefix, "Product prefix doesn't match ( " + pboArchive.ProductEntry.Prefix + " != " + cloneArchive.ProductEntry.Prefix + " )");

            Assert.That(pboArchive.ProductEntry.Addtional.Count == cloneArchive.ProductEntry.Addtional.Count, "Product addtional count doesn't match ( " + pboArchive.ProductEntry.Addtional.Count + " != " + cloneArchive.ProductEntry.Addtional.Count + " )");
        }

        [Test]
        public void CloneAllArchivesTest()
        {
            var testfiles = Directory.GetFiles(TestFolder, "*.pbo");
            string outFolder = Path.Combine(TestFolder, "out2");
            foreach (var pboPath in testfiles)
            {
                string pboName = Path.GetFileName(pboPath);
                var pboArchive = new PboArchive(Path.Combine(TestFolder, pboName));
                var pboNameNoExt = pboName.Substring(0, pboName.Length - 4);
                string tempFolder = Path.Combine(TestFolder, pboNameNoExt);
                string outPath = Path.Combine(outFolder, pboNameNoExt + "_clone.pbo");

                var files = new Dictionary<FileEntry, string>();

                foreach (var entry in pboArchive.Files)
                {
                    string outfile = Path.Combine(tempFolder, GetFileName(entry));

                    Console.WriteLine("Creating/adding " + outfile);
                    files.Add(new FileEntry(entry.FileName,
                        GetPackingMethod(entry.PackingMethod),
                        entry.OriginalSize,
                        entry.TimeStamp,
                        entry.DataSize,
                        entry.Unknown),
                        outfile);

                    //Set the file name so it's extracted to the correct path. Doesn't exist in 'files' array.
                    entry.FileName = outfile;
                }
                // Extract the PBO so all the correct files exist.
                pboArchive.ExtractAll(tempFolder);
                PboArchive.Clone(outPath, pboArchive.ProductEntry, files, pboArchive.Checksum);
                var cloneArchive = new PboArchive(outPath);

                //Compare the entire pbo
                FileAssert.AreEqual(pboPath, outPath, "Files dosen't match - " + pboName);

            }
        }

        public string GetFileName(FileEntry entry)
        {
            // Fix Linux folders for output to filesystem.
            if (IsLinux)
            {
                string path = Path.GetDirectoryName(entry.FileName);
                int fileStartIndex = path.Length;

                string entrypath = Encoding.UTF8.GetString(entry.OrgName.Skip(fileStartIndex).Take(entry.OrgName.Length - fileStartIndex).ToArray());
                if (entrypath.StartsWith("\\") || entrypath.StartsWith("/"))
                    entrypath = entrypath.Remove(0, 1);
                if (entrypath.StartsWith("\\") || entrypath.StartsWith("/"))
                    entrypath = entrypath.Remove(0, 1);
                var patharray = path.Split(new[] { '\\', '/' }).ToList();
                patharray.Add(entrypath);
                return Path.Combine(patharray.ToArray()).Replace('\\', '/');
            }
            else
            {
                return entry.FileName;
            }

        }
        public ulong GetPackingMethod(PackingType type)
        {

            switch (type)
            {
                case PackingType.Packed:
                    return 0x43707273;
                    break;
                /*   case PackingType.Uncompressed:
                        type = 0x0;
                        break;*/
                case PackingType.Uncompressed:
                    return 0x56657273;
                    break;
            }
            throw new System.Exception("Invalid packing type");
        }

    }
}
