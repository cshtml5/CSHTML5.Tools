using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetForHtml5.PrivateTools.AssemblyCompatibilityAnalyzer
{
    static class MergingRelatedClasses
    {
        public static void MergeRelatedClasses(List<HashSet<string>> classesRelatedToEachOther, SortedDictionary<Tuple<string, string>, HashSet<string>> data)
        {
            Dictionary<string, Tuple<string, string>> classNameAlreadyVisitedToKey = new Dictionary<string, Tuple<string, string>>();
            List<Tuple<string, string>> entriesToRemoveFromDictionary = new List<Tuple<string, string>>(); // This is here because we cannot remove the keys while iterating in the dictionary, so we do it afterwards.
            Dictionary<Tuple<string, string>, HashSet<string>> entriesToAddToDictionary = new Dictionary<Tuple<string, string>, HashSet<string>>(); // This is here because we cannot add entries while iterating in the dictionary, so we do it afterwards.

            foreach (KeyValuePair<Tuple<string, string>, HashSet<string>> pair in data)
            {
                string theFullMethodName = pair.Key.Item1;
                string theAssemblyName = pair.Key.Item2;
                HashSet<string> whereItIsUsed = pair.Value;

                // Get the class name:
                string className = theFullMethodName;
                if (className.IndexOf('.') >= 0)
                    className = className.Substring(0, className.IndexOf('.'));
                if (className.IndexOf(' ') >= 0)
                    className = className.Substring(0, className.IndexOf(' '));

                // Check if the class has related classes:
                HashSet<string> groupOfClassesRelatedToEachOther = null;
                string classAlreadyVisitedToMerge = null;
                foreach (HashSet<string> item in classesRelatedToEachOther)
                {
                    if (item.Contains(className))
                    {
                        groupOfClassesRelatedToEachOther = item;
                        break;
                    }
                }
                if (groupOfClassesRelatedToEachOther != null)
                {
                    // Check if we already visited one of those classes:
                    foreach (string className2 in groupOfClassesRelatedToEachOther)
                    {
                        if (classNameAlreadyVisitedToKey.ContainsKey(className2))
                        {
                            classAlreadyVisitedToMerge = className2;
                            break;
                        }
                    }
                }
                if (classAlreadyVisitedToMerge != null)
                {
                    // Get the first item to merge:
                    Tuple<string, string> keyOfFirstItem = classNameAlreadyVisitedToKey[classAlreadyVisitedToMerge];
                    HashSet<string> valueOfFirstItem;
                    if (data.ContainsKey(keyOfFirstItem))
                        valueOfFirstItem = data[keyOfFirstItem];
                    else if (entriesToAddToDictionary.ContainsKey(keyOfFirstItem))
                        valueOfFirstItem = entriesToAddToDictionary[keyOfFirstItem];
                    else
                        throw new KeyNotFoundException("Cannot find item with key: " + keyOfFirstItem);

                    // Get the second item to merge:
                    Tuple<string, string> keyOfSecondItem = pair.Key;
                    HashSet<string> valueOfSecondItem = pair.Value;

                    // Merge the two classes (only if they are in the same assembly):
                    if (keyOfFirstItem.Item2 == keyOfSecondItem.Item2) // Note: "item2" is the assembly name.
                    {
                        entriesToRemoveFromDictionary.Add(keyOfFirstItem);
                        entriesToRemoveFromDictionary.Add(keyOfSecondItem);
                        string assemblyName = keyOfFirstItem.Item2;
                        HashSetHelpers.AddItemsFromOneHashSetToAnother(valueOfFirstItem, valueOfSecondItem);
                        Tuple<string, string> newKey = new Tuple<string, string>(keyOfFirstItem.Item1 + ", " + keyOfSecondItem.Item1, assemblyName);
                        entriesToAddToDictionary.Add(newKey, valueOfSecondItem);
                        classNameAlreadyVisitedToKey[classAlreadyVisitedToMerge] = newKey;
                    }
                }
                else
                {
                    classNameAlreadyVisitedToKey[className] = pair.Key;
                }
            }

            // Perform the actual addition and removal (note: this cannot be done before because it is not possible to remove or add an item from a collection while iterating it with "foreach");
            foreach (var entry in entriesToAddToDictionary)
            {
                data.Add(entry.Key, entry.Value);
            }
            foreach (var key in entriesToRemoveFromDictionary)
            {
                data.Remove(key);
            }
        }
    }
}
