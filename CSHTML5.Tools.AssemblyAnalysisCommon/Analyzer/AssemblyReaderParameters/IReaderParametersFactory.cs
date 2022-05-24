using Mono.Cecil;

namespace CSHTML5.Tools.AssemblyAnalysisCommon.Analyzer.AssemblyReaderParameters
{
    public interface IReaderParametersFactory
    {
        ReaderParameters GetReaderParameters(string path);
    }
}
