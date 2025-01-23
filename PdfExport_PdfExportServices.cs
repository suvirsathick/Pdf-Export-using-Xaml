using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace MyRevitAddin.Services
{
    public class PDFExportService
    {
        private readonly Document _doc;

        public PDFExportService(Document doc)
        {
            _doc = doc;
        }

        public bool ExportSheets(string directory, IEnumerable<ElementId> sheetIds)
        {
            try
            {
                var exportOptions = new PDFExportOptions
                {
                    Combine = false,  // Ensure files aren't combined
                    HideCropBoundaries = false,
                    ZoomPercentage = 100,
                    PaperFormat = ExportPaperFormat.Default,
                    FileName = "temp" // This will be overridden for each sheet
                };

                // Export each sheet individually
                foreach (ElementId sheetId in sheetIds)
                {
                    // Get the sheet to access its properties
                    ViewSheet sheet = _doc.GetElement(sheetId) as ViewSheet;
                    if (sheet != null)
                    {
                        // Create a sanitized filename from sheet number and name
                        string filename = $"{sheet.SheetNumber}-{sheet.Name}".Replace(':', '_')
                                                                            .Replace('/', '_')
                                                                            .Replace('\\', '_')
                                                                            .Replace('*', '_')
                                                                            .Replace('?', '_')
                                                                            .Replace('"', '_')
                                                                            .Replace('<', '_')
                                                                            .Replace('>', '_')
                                                                            .Replace('|', '_');

                        // Set the full file path
                        string fullPath = Path.Combine(directory, filename + ".pdf");

                        // Export single sheet
                        _doc.Export(directory, new List<ElementId> { sheetId }, exportOptions);

                        // Since Revit might use its own naming convention, find and rename the exported file
                        string[] pdfFiles = Directory.GetFiles(directory, "*.pdf")
                            .Where(f => File.GetLastWriteTime(f) >= System.DateTime.Now.AddSeconds(-5))
                            .ToArray();

                        if (pdfFiles.Length > 0)
                        {
                            // Get the most recently created PDF file
                            string lastExportedFile = pdfFiles.OrderByDescending(f => File.GetLastWriteTime(f)).First();

                            // Rename it to our desired filename if it's different
                            if (lastExportedFile != fullPath)
                            {
                                if (File.Exists(fullPath))
                                    File.Delete(fullPath);
                                File.Move(lastExportedFile, fullPath);
                            }
                        }
                    }
                }

                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}
