using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetForHtml5.PrivateTools.AssemblyCompatibilityAnalyzer
{
    class CsvGenerator
    {
        public static void Generate(
            string outputCsvFilePath,
            SortedDictionary<Tuple<string, string>, HashSet<string>> unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed)
        {
            StringBuilder outputCsv = new StringBuilder();
            outputCsv.AppendLine("sep=;"); // Tells Excel what the separator is.
            foreach (var unsupportedMethodAndLocationsWhereItIsUsed in unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed)
            {
                string methodName = unsupportedMethodAndLocationsWhereItIsUsed.Key.Item1;
                string methoAssemblyName = unsupportedMethodAndLocationsWhereItIsUsed.Key.Item2;
                HashSet<string> locations = unsupportedMethodAndLocationsWhereItIsUsed.Value;
                List<string> sortedLocations = new List<string>(locations);
                sortedLocations.Sort();

                outputCsv.Append(@"""" + methodName + @"""");
                outputCsv.Append(";");
                outputCsv.Append(@"""");
#if FULL_PATH_OUTPUT
                    outputCsv.Append(string.Join(Environment.NewLine, sortedLocations));
#else
                outputCsv.Append(string.Join(", ", sortedLocations));
#endif
                outputCsv.Append(@"""");
                outputCsv.Append(";");
                outputCsv.Append(@"""" + methoAssemblyName + @"""");
                outputCsv.AppendLine();
            }

            File.WriteAllText(outputCsvFilePath, outputCsv.ToString());
        }
    }
}
