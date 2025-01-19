using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using MyRevitAddin.ViewModels;
using MyRevitAddin.Views;
using MyRevitAddin.Services;

namespace MyRevitAddin
{
    public class ExternalCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            // Collect sheets
            var allSheets = new FilteredElementCollector(doc)
                                .OfClass(typeof(ViewSheet))
                                .Cast<ViewSheet>()
                                .ToList();
            if (!allSheets.Any())
            {
                TaskDialog.Show("PDF Export", "No Sheets Found");
                return Result.Cancelled;
            }

            // Instantiate the ViewModel
            var viewModel = new DirectorySelectionViewModel();
            if (viewModel == null)
            {
                TaskDialog.Show("PDF Export", "Failed to initialize ViewModel.");
                return Result.Failed;
            }

            // Populate elements
            foreach (var sheet in allSheets)
            {
                viewModel.Elements.Add(new SelectableElement
                {
                    Name = $"{sheet.SheetNumber} - {sheet.Name}",
                    IsSelected = false
                });
            }

            // Create & show the view
            var view = new DirectorySelectionView
            {
                DataContext = viewModel
            };

            // Set owner to Revit's main window
            var wih = new WindowInteropHelper(view)
            {
                Owner = uiapp.MainWindowHandle
            };

            // Subscribe to close requests
            viewModel.RequestClose += (sender, result) =>
            {
                view.DialogResult = result;
                view.Close();
            };

            // Show dialog
            bool? dialogResult = view.ShowDialog();
            if (dialogResult == true)
            {
                // The user clicked OK
                string directory = viewModel.SelectedDirectoryDisplay;
                var selectedSheets = viewModel.Elements
                    .Where(e => e.IsSelected)
                    .Select(e => allSheets.First(s => $"{s.SheetNumber} - {s.Name}" == e.Name).Id)
                    .ToList();

                if (selectedSheets.Any())
                {
                    var exportService = new PDFExportService(doc);
                    bool success = exportService.ExportSheets(directory, selectedSheets);

                    TaskDialog.Show("PDF Export",
                        success ? "Exported Successfully" : "Export Failed");
                }
                else
                {
                    TaskDialog.Show("PDF Export", "Please select sheets");
                }
            }

            return Result.Succeeded;
        }
    }
}
