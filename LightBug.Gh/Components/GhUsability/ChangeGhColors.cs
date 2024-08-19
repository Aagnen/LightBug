using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace LightBug.Gh.Components.GhUsability
{
    public class ChangeGhColors : GH_Component
    {

        public ChangeGhColors()
          : base("ChangeGhColors", 
              "ChangeGhColors",
              "Change colors of Gh Canvas. Based on https://james-ramsden.com/change-the-colour-of-the-grasshopper-canvas/",
              "Lightbug",
              "GhUsability")
        {
        }

        public override Guid ComponentGuid => new Guid("22CD3420-1211-4764-9A55-C881332F2A0A");

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Reset", "Reset", "Reset to defaults.", GH_ParamAccess.item);
            pManager.AddColourParameter("GridColor", "Grid", "", GH_ParamAccess.item, Color.FromArgb(0, 0, 0, 0));
            pManager.AddColourParameter("BackgroundColor", "Background", "", GH_ParamAccess.item, Color.FromArgb(0, 0, 0, 0));
            pManager.AddColourParameter("WireStart", "WireStart", "", GH_ParamAccess.item, Color.FromArgb(0, 0, 0, 0));
            pManager.AddColourParameter("WireEnd", "WireEnd", "", GH_ParamAccess.item, Color.FromArgb(0, 0, 0, 0));
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool reset = true;
            Color grid = Color.FromArgb(0, 0, 0, 0);
            Color background = Color.FromArgb(0, 0, 0, 0);
            Color wireStart = Color.FromArgb(0, 0, 0, 0);
            Color wireEnd = Color.FromArgb(0, 0, 0, 0);

            if (!DA.GetData(0, ref reset))
                return;
            DA.GetData(1, ref grid);
            DA.GetData(2, ref background);
            DA.GetData(3, ref wireStart);
            DA.GetData(4, ref wireEnd);

            if (reset)
            {
                //DEFAULTS
                Grasshopper.GUI.Canvas.GH_Skin.canvas_grid = Color.FromArgb(30, 0, 0, 0);
                Grasshopper.GUI.Canvas.GH_Skin.canvas_back = Color.FromArgb(255, 212, 208, 200);
                Grasshopper.GUI.Canvas.GH_Skin.canvas_edge = Color.FromArgb(255, 0, 0, 0);
                Grasshopper.GUI.Canvas.GH_Skin.canvas_shade = Color.FromArgb(80, 0, 0, 0);
                Grasshopper.GUI.Canvas.GH_Skin.wire_selected_a = Color.FromArgb(225, 125, 210, 40);
                Grasshopper.GUI.Canvas.GH_Skin.wire_selected_b = Color.FromArgb(50, 0, 0, 0);
            }
            else
            {
                if (grid != Color.FromArgb(0, 0, 0, 0))
                    Grasshopper.GUI.Canvas.GH_Skin.canvas_grid = grid;
                if (background != Color.FromArgb(0, 0, 0, 0))
                    Grasshopper.GUI.Canvas.GH_Skin.canvas_back = background;
                if (wireStart != Color.FromArgb(0, 0, 0, 0))
                    Grasshopper.GUI.Canvas.GH_Skin.wire_selected_a = wireStart;
                if (wireEnd != Color.FromArgb(0, 0, 0, 0))
                    Grasshopper.GUI.Canvas.GH_Skin.wire_selected_b = wireEnd;
                
                //Grasshopper.GUI.Canvas.GH_Skin.canvas_edge = Color.FromArgb(255, 0, 0, 0);
                //Grasshopper.GUI.Canvas.GH_Skin.canvas_shade = Color.FromArgb(80, 0, 0, 0);
            }
        }
    }
}