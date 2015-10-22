using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SwiftPbo;

namespace PboTest
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var done = PboArchive.Create(new DirectoryInfo("indata").FullName, "testpbo.pbo",
                new ProductEntry("prefix", "testpbo", "Head", new List<string>() { "SwiftPbo.dll" }));
            if (!done)
                return 1;
            var inpath = "testpbo.pbo";
            PboArchive cake;
            try
            {
                cake = new PboArchive(inpath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 2;
            }

            var files = new Dictionary<FileEntry, string>();
            foreach (var fileEntry in cake.Files)
            {
                var path = Path.Combine("output", fileEntry.FileName);
                fileEntry.Extract(path);
                files.Add(fileEntry, path);
            }
            var file = cake.Files[0];
            try
            {
                var stream = file.Extract();
                stream.Close();
            }
            catch (Exception)
            {

                return 3;
            }
            try
            {
                PboArchive.Clone(@"nottest.pbo", cake.ProductEntry, files, cake.Checksum);
            }
            catch (Exception)
            {
                return 4;
            }

            return !FilesAreEqual(new FileInfo(inpath), new FileInfo("nottest.pbo")) ? 5 : 0;
        }

        private const int BYTES_TO_READ = sizeof (Int64);

        private static bool FilesAreEqual(FileInfo first, FileInfo second)
        {
            if (first.Length != second.Length)
                return false;

            int iterations = (int) Math.Ceiling((double) first.Length/BYTES_TO_READ);

            using (FileStream fs1 = first.OpenRead())
            using (FileStream fs2 = second.OpenRead())
            {
                byte[] one = new byte[BYTES_TO_READ];
                byte[] two = new byte[BYTES_TO_READ];

                for (int i = 0; i < iterations; i++)
                {
                    fs1.Read(one, 0, BYTES_TO_READ);
                    fs2.Read(two, 0, BYTES_TO_READ);

                    if (BitConverter.ToInt64(one, 0) != BitConverter.ToInt64(two, 0))
                        return false;
                }
            }
            return true;
        }
    }
}
