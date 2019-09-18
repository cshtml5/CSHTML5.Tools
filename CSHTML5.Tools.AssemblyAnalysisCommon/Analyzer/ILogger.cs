using Microsoft.Build.Framework;

namespace DotNetForHtml5.PrivateTools.AssemblyCompatibilityAnalyzer
{
    public interface ILogger
    {
        void WriteError(string message, string file = "", int lineNumber = 0, int columnNumber = 0);
        void WriteMessage(string message, MessageImportance messageImportance = MessageImportance.High);
        void WriteWarning(string message);
        bool HasErrors { get; }
        void SetRequiresMissingFeature(string missingFeatureId, string messageForMissingFeature);
        bool RequiresMissingFeature { get; }
        string MissingFeatureId { get; }
        string MessageForMissingFeature { get; }
    }
}
