using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace AirChair
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class InsertAirDuct : IExternalCommand
    {
        public List<Element> SelectElements(Document document)
        {
            UIDocument uidoc = new UIDocument(document);
            Selection choices = uidoc.Selection;
            // Pick one object from Revit.
            /*Reference hasPickOne = choices.PickObject(ObjectType.Element);
            if (hasPickOne != null)
            {
                TaskDialog.Show("Revit", "One element selected.");
            }*/

            // Use the rectangle picking tool to identify model elements to select.
            IList<Element> pickedElements = uidoc.Selection.PickElementsByRectangle("Select by rectangle");
            List<Element> selElements = new List<Element>(pickedElements.Count);
            if (pickedElements.Count > 0)
            {
                // Collect Ids of all picked elements
                IList<ElementId> idsToSelect = new List<ElementId>(pickedElements.Count);
                
                foreach (Element element in pickedElements)
                {
                    idsToSelect.Add(element.Id);
                    selElements.Add(element);
                }

                // Update the current selection
                uidoc.Selection.SetElementIds(idsToSelect);
                //TaskDialog.Show("Revit", string.Format("{0} elements added to Selection.", idsToSelect.Count));
                
            }
            return selElements;
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            List<Element> correlements = new List<Element>();
            var pickedElements= SelectElements(doc);
            var categoryName = BuiltInCategory.OST_Furniture;
            correlements = pickedElements.Select(el => el).Where(el => el.Category.Name=="Мебель").ToList();
            foreach (Element element in correlements)
            {
                TaskDialog.Show("Revit", $"{element.Id}");
            }
            return Result.Succeeded;
        }
    }
}
