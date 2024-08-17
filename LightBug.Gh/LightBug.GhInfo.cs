using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace LightBug.Gh
{
    public class LightBug_GhInfo : GH_AssemblyInfo
    {
        public override string Name => "LightBug.Gh";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        //public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("865be6ee-e822-4ace-a724-be100c310bea");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}