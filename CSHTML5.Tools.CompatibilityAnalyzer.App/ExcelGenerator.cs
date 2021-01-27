using Syncfusion.XlsIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;

namespace DotNetForHtml5.PrivateTools.AssemblyCompatibilityAnalyzer
{
    static class ExcelGenerator
    {
        public static void Generate(
            string outputExcelFilePath,
            FeaturesAndEstimationsFileProcessor featuresAndEstimationsFileProcessor,
            SortedDictionary<Tuple<string, string>, HashSet<string>> unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed,
            IEnumerable<string> analyzedDlls)
        {
            ExcelEngine excelEngine = new ExcelEngine();

            //Instantiate the Excel application object
            IApplication application = excelEngine.Excel;

            //Assigns default application version
            application.DefaultVersion = ExcelVersion.Excel2013;

            //A new workbook is created equivalent to creating a new workbook in Excel
            //Create a workbook with 1 worksheet
            IWorkbook workbook = application.Workbooks.Create(1);

            //Access first worksheet from the workbook
            IWorksheet worksheet = workbook.Worksheets[0];

            // Set the columns width (note: columns here are one-based):
            worksheet.SetColumnWidth(3, 60);
            worksheet.SetColumnWidth(4, 45);
            worksheet.SetColumnWidth(5, 30);
            worksheet.SetColumnWidth(6, 13);

            // Prepare the styles:
            IStyle style_Remove, style_EasyWorkaround, style_RequiresImplementation, style_NonTrivialWorkaround;
            PrepareTheStyles(workbook, out style_Remove, out style_EasyWorkaround, out style_RequiresImplementation, out style_NonTrivialWorkaround);

            // Set the header text:
            worksheet.Range[5, 3].Text = "FEATURE:";
            worksheet.Range[5, 4].Text = "CLASSES OR FILES WHERE THE FEATURE IS USED:";

            // Set the header format:
            worksheet.Range[5, 3, 5, 6].CellStyle.Font.Bold = true;

            // Index of the first row that contains the feature:
            int currentRow = 7;

            var groupedResult = unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed.GroupBy((element) => element.Key.Item2);
            foreach (IGrouping<string, KeyValuePair<Tuple<string, string>, HashSet<string>>> item in groupedResult)
            {
                var currentCell = worksheet.Range["A" + currentRow.ToString()];
                currentCell.Text = "From \"" + item.Key + "\":";
                currentCell.CellStyle.Font.Bold = true;
                currentCell.CellStyle.Font.Size = 14;
                currentCell.CellStyle.Font.Underline = ExcelUnderline.Single;
                currentRow += 2;
                foreach (KeyValuePair<Tuple<string, string>, HashSet<string>> obj in item)
                {
                    // Feature name:
                    string featureName = obj.Key.Item1;
                    IRange featureNameCell = worksheet.Range["C" + currentRow.ToString()];
                    featureNameCell.Text = featureName;

                    // Locations where the feature is used:
                    List<string> sortedLocations = new List<string>(obj.Value);
                    sortedLocations.Sort();
                    string sortLocationsJoined = string.Join(", ", sortedLocations);
                    IRange locationCell = worksheet.Range["D" + currentRow.ToString()];
                    locationCell.Text = sortLocationsJoined;

                    // Other information:
                    ExcelRowInfo excelRowInfo;
                    if (featuresAndEstimationsFileProcessor.TryGetInfo(featureName, out excelRowInfo))
                    {
                        // Row style:
                        string recommendedActionCode = excelRowInfo.RecommendedActionCode;
                        if (!string.IsNullOrEmpty(recommendedActionCode))
                        {
                            IStyle styleToApply = null;
                            switch (recommendedActionCode)
                            {
                                case "REMOVE":
                                    styleToApply = style_Remove;
                                    break;
                                case "EASYWORKAROUND":
                                    styleToApply = style_EasyWorkaround;
                                    break;
                                case "REQUIRESIMPLEMENTATION":
                                    styleToApply = style_RequiresImplementation;
                                    break;
                                case "NONTRIVIALWORKAROUND":
                                    styleToApply = style_NonTrivialWorkaround;
                                    break;
                                default:
                                    break;
                            }
                            if (styleToApply != null)
                                worksheet.Range["A" + currentRow.ToString() + ":" + "G" + currentRow.ToString()].CellStyle = styleToApply; ;
                        }

                        // Recommended action:
                        string recommendedAction = excelRowInfo.RecommendedAction;
                        if (!string.IsNullOrEmpty(recommendedAction))
                        {
                            worksheet.Range["E" + currentRow.ToString()].Text = recommendedAction;
                        }

                        // Estimation of workload:
                        string estimation = excelRowInfo.Estimation;
                        if (!string.IsNullOrEmpty(estimation))
                        {
                            worksheet.Range["F" + currentRow.ToString()].Number = double.Parse(estimation);
                        }

                        // Optional title:
                        string optionalTitle = excelRowInfo.OptionalTitle;
                        if (!string.IsNullOrEmpty(optionalTitle))
                        {
                            featureNameCell.Text = optionalTitle + ": " + featureName;
                        }
                    }

                    // Feature name font size:
                    if (featureName.Length > 2700)
                        featureNameCell.CellStyle.Font.Size = 7;
                    else if (featureName.Length > 2000)
                        featureNameCell.CellStyle.Font.Size = 8;
                    else if (featureName.Length > 1300)
                        featureNameCell.CellStyle.Font.Size = 9;
                    else if (featureName.Length > 600)
                        featureNameCell.CellStyle.Font.Size = 10;
                    else
                        featureNameCell.CellStyle.Font.Size = 11;

                    // Locations font size:
                    if (sortLocationsJoined.Length > 2700)
                        locationCell.CellStyle.Font.Size = 7;
                    else if (sortLocationsJoined.Length > 2000)
                        locationCell.CellStyle.Font.Size = 8;
                    else if (sortLocationsJoined.Length > 1300)
                        locationCell.CellStyle.Font.Size = 9;
                    else if (sortLocationsJoined.Length > 600)
                        locationCell.CellStyle.Font.Size = 10;
                    else
                        locationCell.CellStyle.Font.Size = 11;

                    // Increase the row index:
                    ++currentRow;
                }

                // Insert a blank row:
                ++currentRow;
            }

            // Set "Wrap Text" on the columns 2, 3, and 5 (note: columns here are one-based):
            worksheet.Columns[2].WrapText = true;
            worksheet.Columns[3].WrapText = true;
            try
            {
                worksheet.Columns[4].WrapText = true;
                worksheet.Columns[5].WrapText = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            // Vertical align the content of the cells:
            worksheet.Columns[2].VerticalAlignment = ExcelVAlign.VAlignTop;
            worksheet.Columns[3].VerticalAlignment = ExcelVAlign.VAlignTop;
            try
            {
                worksheet.Columns[4].VerticalAlignment = ExcelVAlign.VAlignTop;
                worksheet.Columns[5].VerticalAlignment = ExcelVAlign.VAlignTop;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            // Add information at the end about the DLLs that have been analized:
            currentRow += 5;
            string analyzedDllsText = "(Analyzed DLLs: " + string.Join(", ", analyzedDlls) + ")";
            worksheet.Range["A" + currentRow.ToString()].Text = analyzedDllsText;

            // Saving the workbook to disk in XLSX format
            workbook.SaveAs(outputExcelFilePath);
        }

        static void PrepareTheStyles(
            IWorkbook workbook,
            out IStyle style_Remove,
            out IStyle style_EasyWorkaround,
            out IStyle style_RequiresImplementation,
            out IStyle style_NonTrivialWorkaround
            )
        {
            // "REMOVE" style:
            style_Remove = workbook.Styles.Add("REMOVE");
            style_Remove.Color = Color.LightGray;
            style_Remove.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
            style_Remove.Borders[ExcelBordersIndex.EdgeTop].Color = ExcelKnownColors.Grey_50_percent;
            style_Remove.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
            style_Remove.Borders[ExcelBordersIndex.EdgeBottom].Color = ExcelKnownColors.Grey_50_percent;

            // "EASYWORKAROUND" style:
            style_EasyWorkaround = workbook.Styles.Add("EASYWORKAROUND");
            style_EasyWorkaround.Color = Color.FromArgb(255, 221, 235, 247);
            style_EasyWorkaround.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
            style_EasyWorkaround.Borders[ExcelBordersIndex.EdgeTop].Color = ExcelKnownColors.Grey_50_percent;
            style_EasyWorkaround.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
            style_EasyWorkaround.Borders[ExcelBordersIndex.EdgeBottom].Color = ExcelKnownColors.Grey_50_percent;

            // "REQUIRESIMPLEMENTATION" style:
            style_RequiresImplementation = workbook.Styles.Add("REQUIRESIMPLEMENTATION");
            style_RequiresImplementation.Color = Color.LightYellow;
            style_RequiresImplementation.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
            style_RequiresImplementation.Borders[ExcelBordersIndex.EdgeTop].Color = ExcelKnownColors.Grey_50_percent;
            style_RequiresImplementation.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
            style_RequiresImplementation.Borders[ExcelBordersIndex.EdgeBottom].Color = ExcelKnownColors.Grey_50_percent;

            // "NONTRIVIALWORKAROUND" style:
            style_NonTrivialWorkaround = workbook.Styles.Add("NONTRIVIALWORKAROUND");
            style_NonTrivialWorkaround.Color = Color.FromArgb(255, 252, 228, 214);
            style_NonTrivialWorkaround.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
            style_NonTrivialWorkaround.Borders[ExcelBordersIndex.EdgeTop].Color = ExcelKnownColors.Grey_50_percent;
            style_NonTrivialWorkaround.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
            style_NonTrivialWorkaround.Borders[ExcelBordersIndex.EdgeBottom].Color = ExcelKnownColors.Grey_50_percent;
        }
    }
}