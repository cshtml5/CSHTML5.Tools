using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using StubGenerator.Common.Options;

namespace StubGenerator.Common
{

    class Program
    {
        static void Main(string[] args)
        {
            new StubGenerator().Run();
            Console.WriteLine("Done.");
        }
    }
}
