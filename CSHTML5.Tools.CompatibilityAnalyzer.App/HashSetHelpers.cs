using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetForHtml5.PrivateTools.AssemblyCompatibilityAnalyzer
{
    static class HashSetHelpers
    {
        public static void AddItemsFromOneHashSetToAnother(HashSet<string> source, HashSet<string> destination)
        {
            foreach (string str in source)
            {
                if (!destination.Contains(str))
                {
                    destination.Add(str);
                }
            }
        }
    }
}
