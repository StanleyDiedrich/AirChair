using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;


namespace AirChair
{
    
    public class Chair
    {
        public XYZ Coordinate { get; set; }
        public XYZ Orientation { get; set; }
        public string Name { get; set; }
        public Chair(string name, XYZ coordinate, XYZ orientation)
        {
            Name = name;
            Coordinate = coordinate;
            Orientation = orientation;
        }
        public double GetRotation(Chair chair, AirTerminal airTerminal)
        {
            double angle = 0;
            XYZ orientationC = chair.Orientation;
            XYZ orientationA = airTerminal.TOrientation;
            double Cx = Math.Round(orientationC.X);
            double Cy = Math.Round(orientationC.Y);
            double Cz = Math.Round(orientationC.Z);

            double Ax = Math.Round(orientationA.X);
            double Ay = Math.Round(orientationA.Y);
            double Az = Math.Round(orientationA.Z);

            XYZ mult = new XYZ(Cy * Az - Cz * Ay, Cz * Ax - Cx * Az, Cx * Ay - Cy * Ax);

            double scalar = Cx * Ax + Cy * Ay;
            double vect1 = Math.Sqrt(Cx * Cx + Cy * Cy);
            double vect2 = Math.Sqrt(Ax* Ax + Ay * Ay);

            angle = Math.Acos(scalar / vect1 * vect2) * 180 / Math.PI;

            if (mult.Z>=0)
            {
                angle = angle;
            }
            else
            {
                angle=-angle;
            }

            return angle;
        }
    }
     public class AirTerminal
    {
        public XYZ TCoordinate { get; set; }
        public XYZ TOrientation { get; set;}
        public AirTerminal(XYZ tCoordinate, XYZ tOrientation)
        {
            TCoordinate = tCoordinate;
            TOrientation = tOrientation;
        }
    }
    

    [Regeneration(RegenerationOption.Manual)]
    [TransactionAttribute(TransactionMode.Manual)]
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
            List<XYZ> points = new List<XYZ>();
            List<Chair> chairs = new List<Chair>();
            var pickedElements= SelectElements(doc);
            var categoryName = BuiltInCategory.OST_Furniture;
            correlements = pickedElements.Select(el => el).Where(el => el.Category.Name=="Мебель").ToList();

            //Выбор семейства для вставки
            FilteredElementCollector famtype = new FilteredElementCollector(doc).OfClass(typeof(Family));
            string FamilyName = "КСД";
            Family family = famtype.FirstOrDefault<Element>(x => x.Name.Equals(FamilyName)) as Family;
            ISet<ElementId> elementSet = family.GetFamilySymbolIds();
            FamilySymbol familyType = doc.GetElement(elementSet.First()) as FamilySymbol;
            //Закончили выбирать


            foreach (Element element in correlements)
            {
                var lpoint = element.Location as LocationPoint;
                var point = lpoint.Point;
                var x = point.X;
                string name = element.Name;
                FamilyInstance familyInstance = element as FamilyInstance;
                XYZ vector = familyInstance.FacingOrientation;
                Chair chair = new Chair(name,point, vector);
                
                chairs.Add(chair);
            }
            foreach (var chair in chairs)
            {
                using (Transaction transaction=new Transaction(doc, "Place Air Terminal"))
                {
                    transaction.Start();
               
                
                    if (!familyType.IsActive)
                    {
                        familyType.Activate();
                    }
                    try
                    {
                        FamilyInstance fI = doc.Create.NewFamilyInstance(chair.Coordinate, familyType, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                    }
                    catch
                    {
                        continue;
                    }
                   transaction.Commit();
                }
            }
            //TaskDialog.Show("Revit", $"{point.X},{point.Y},{point.Z}");
            return Result.Succeeded;
        }
    }
}
