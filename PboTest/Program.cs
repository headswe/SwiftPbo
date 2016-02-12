using System;
using System.Collections.Generic;
using System.Diagnostics;
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

         //   var done = PboArchive.Create(new DirectoryInfo("indata").FullName, "testpbo.pbo",
        //        new ProductEntry("prefix", "testpbo", "Head", new List<string> { "SwiftPbo.dll" }));
         //   if (!done)
          //      return 1;
            const string inpath = "hotze_mske.pbo";
            Stopwatch watch = new Stopwatch();
            PboArchive cake;
            try
            {
                watch.Start();
                cake = new PboArchive(inpath,false);
                watch.Stop();
                Console.WriteLine("Took {0}ms",watch.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 2;
            }

            watch.Reset();
            watch.Start();
            cake.ExtractAll("out");
            cake.Dispose();
            watch.Stop();
            Console.WriteLine("Took {0}ms", watch.ElapsedMilliseconds);
            return 0;
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
