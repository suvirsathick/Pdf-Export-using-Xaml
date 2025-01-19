using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

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
                    Combine = false,
                    HideCropBoundaries = false,
                    ZoomPercentage = 100,
                    PaperFormat = ExportPaperFormat.Default
                };

                _doc.Export(directory, sheetIds.ToList(), exportOptions);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
