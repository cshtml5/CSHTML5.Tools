using Mono.Cecil;
using StubGenerator.Common.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StubGenerator.Common.Analyzer
{
    public interface IElementAnalyzer<T> where T : MemberReference
    {
        T Element { get; set; }

        bool CanWorkOnElement();
    }
}
