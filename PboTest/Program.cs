using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SwiftPbo;

namespace PboTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var cake = new PboArchive(@"H:\testpbo.pbo");
            var entry = cake.Files[0];
            entry.Extract(@"H:\herpderp.txt");

        }
    }
}
