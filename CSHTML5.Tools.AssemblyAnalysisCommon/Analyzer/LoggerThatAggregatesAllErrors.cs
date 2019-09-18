using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;

namespace DotNetForHtml5.PrivateTools.AssemblyCompatibilityAnalyzer
{
    public class LoggerThatAggregatesAllErrors : ILogger //LoggerBase, 
    {
        bool _hasErrors;

        HashSet<string> _errors = new HashSet<string>();

        public void WriteError(string message, string file = "", int lineNumber = 0, int columnNumber = 0)
        {
            _hasErrors = true;

            var str = ("ERROR: " + message + (!string.IsNullOrEmpty(file) ? " in \"" + file + "\"" + (lineNumber > 0 ? " (line " + lineNumber.ToString() + ")" : "") + (columnNumber > 0 ? " (column " + columnNumber.ToString() + ")" : "") + "." : ""));
            if (!_errors.Contains(str))
                _errors.Add(str);
        }

        public void WriteMessage(string message, MessageImportance messageImportance)
        {
            var str = (message);
            if (!_errors.Contains(str))
                _errors.Add(str);
        }

        public void WriteWarning(string message)
        {
            var str = ("WARNING: " + message);
            if (!_errors.Contains(str))
                _errors.Add(str);
        }

        public bool HasErrors
        {
            get
            {
                return _hasErrors;
            }
        }

        public string GetAllErrors()
        {
            var list = new List<string>(_errors);
            list.Sort();
            return String.Join(Environment.NewLine, list);
        }

        public int GetErrorCount()
        {
            return _errors.Count;
        }

        #region from LoggerBase

        bool _requiresMissingFeature;
        string _missingFeatureId;
        string _messageForMissingFeature;
        bool _isInTrialMode;

        public void SetRequiresMissingFeature(string missingFeatureId, string messageForMissingFeature)
        {
            _requiresMissingFeature = true;
            _missingFeatureId = missingFeatureId;
            _messageForMissingFeature = messageForMissingFeature;
        }

        public bool RequiresMissingFeature
        {
            get
            {
                return _requiresMissingFeature;
            }
        }

        public string MissingFeatureId
        {
            get
            {
                return _missingFeatureId;
            }
        }

        public string MessageForMissingFeature
        {
            get
            {
                return _messageForMissingFeature;
            }
        }
        #endregion
    }
}
