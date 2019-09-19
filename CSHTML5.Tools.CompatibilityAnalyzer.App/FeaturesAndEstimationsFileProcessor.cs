using Syncfusion.XlsIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DotNetForHtml5.PrivateTools.AssemblyCompatibilityAnalyzer
{
    class FeaturesAndEstimationsFileProcessor
    {
        Dictionary<string, ExcelRowInfo> _rowsInfo = new Dictionary<string, ExcelRowInfo>();
        bool _properlyInitialized = false;

        public FeaturesAndEstimationsFileProcessor(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            if (!File.Exists(filePath))
            {
                MessageBox.Show("File not found: " + filePath + Environment.NewLine + Environment.NewLine + "The file will be ignored.");
                return;
            }

            // Open the Excel file:
            ExcelEngine excelEngine = new ExcelEngine();
            IWorkbook workbook = excelEngine.Excel.Workbooks.OpenReadOnly(filePath);
            IWorksheet sheet = workbook.ActiveSheet;

            // Read all the rows:
            int rowCount = sheet.UsedRange.LastRow;
            for (int rowIndex = 1; rowIndex <= rowCount; rowIndex++)
            {
                string featureName = sheet[rowIndex, 1].DisplayText;
                string optionalTitle = sheet[rowIndex, 2].DisplayText;
                string action = sheet[rowIndex, 3].DisplayText;
                string estimation = sheet[rowIndex, 4].DisplayText;
                string comment = sheet[rowIndex, 5].DisplayText;

                if (!string.IsNullOrEmpty(featureName))
                {
                    //string key;
                    //if (featureName.IndexOf(' ') >= 0)
                    //    key = featureName.Substring(0, featureName.IndexOf(' '));
                    //else
                    //    key = featureName;
                    string key = featureName;

                    ExcelRowInfo rowInfo = new ExcelRowInfo()
                    {
                        FeatureName = featureName,
                        OptionalTitle = optionalTitle,
                        RecommendedActionCode = action,
                        RecommendedAction = comment,
                        Estimation = estimation,
                        Key = key
                    };

                    _rowsInfo.Add(key, rowInfo);
                }
            }

            _properlyInitialized = true;
        }

        public bool TryGetInfo(string key, out ExcelRowInfo excelRowInfo)
        {
            return _rowsInfo.TryGetValue(key, out excelRowInfo);
        }
    }

    class ExcelRowInfo
    {
        public string FeatureName { get; set; }
        public string OptionalTitle { get; set; }
        public string RecommendedActionCode { get; set; }
        public string RecommendedAction { get; set; }
        public string Estimation { get; set; }
        public string Key { get; set; }
    }
}
