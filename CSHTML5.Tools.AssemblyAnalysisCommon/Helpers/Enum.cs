using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetForHtml5.PrivateTools.AssemblyAnalysisCommon
{
    public enum TypeEnum
    {
        None = 10,
        Class = 0,
        Struct = 1,
        Interface = 2,
        Enum = 3,
        Delegate = 4,
    }

    public enum Product
    {
        CSHTML5,
        CSHTML5_V2,
        OPENSILVER
    }
}
