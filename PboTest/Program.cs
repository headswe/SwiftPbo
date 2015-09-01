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
            var cake = new Pbo();
            cake.Load(@"C:\Users\Sebastian\Desktop\by_sea_by_land_v2.MCN_Aliabad.pbo");
            cake.ExtractEntry(cake.Entries.Find(x => x.FileName == "f_assigngear_blu_f.sqf"), "config.cpp");

        }
    }
}
