using System;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;
using LightBug.Core.DifferentialGrowth;

namespace LightBug.Gh.Components.DifferentialGrowth
{
    public class MeshDifferentialGrowth : GH_Component
    {
        public MeshDifferentialGrowth()
            : base(
                "MeshDifferentialGrowth",
                "MeshDifferentialGrowth",
                "Expand a mesh based on subdivision and avoiding self-collision. Based on YT tutorials by Long Nguyen and then modified.",
                "Lightbug",
                "DifferentialGrowth")
        {
        }

        protected override Bitmap Icon => Properties.Resources.Components_MeshDifferentialGrowth;

        public override Guid ComponentGuid => new Guid("0bc9e2cd-0684-40ce-a2b4-1aedb3c9f4be");

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Reset", "Reset", "Reset", GH_ParamAccess.item);
            pManager.AddMeshParameter("StartingMesh", "M", "StartingMesh", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Grow", "Grow", "Grow", GH_ParamAccess.item);

            pManager.AddNumberParameter("EdgeLength", "EdgeLength", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("EdgeLengthConstraintWeight", "EdgeLenWeight", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("CollisionDistance", "CollDist", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("CollisionWeight", "CollWeight", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("BendingResistanceWeight", "BendingResWeight", "t", GH_ParamAccess.item);

            pManager.AddPointParameter("AttractorPoint", "Attractor", "", GH_ParamAccess.item, Point3d.Unset);
            pManager.AddNumberParameter("MaxDistanceFromAttractor", "MaxDist", "", GH_ParamAccess.item, 0);

            pManager.AddIntegerParameter("MaxVertexCount", "Max. Vertex Count", "Max. Vertex Count", GH_ParamAccess.item, 3000);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "Mesh", "Mesh", GH_ParamAccess.item);
        }

        private LightBug.Core.DifferentialGrowth.MeshDifferentialGrowth myMeshGrowthSystem;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool iReset = true;
            Mesh iStartingMesh = null;
            bool iGrow = false;

            double iEdgeLength = 0.0;
            double iEdgeLengthConstrainWeight = 0.0;
            double iCollisionDistance = 0.0;
            double iCollisionWeight = 0.0;
            double iBendingResistanceWeight = 0.0;

            Point3d iAttractor = Point3d.Unset;
            double iMaxDistFromAttractor = 0;

            int iMaxVertexCount = 0;

            if(!DA.GetData("Reset", ref iReset))
                return;
            if (!DA.GetData("StartingMesh", ref iStartingMesh))
                return;
            if (!DA.GetData("Grow", ref iGrow))
                return;

            if (!DA.GetData("EdgeLength", ref iEdgeLength))
                return;
            if (!DA.GetData("EdgeLengthConstraintWeight", ref iEdgeLengthConstrainWeight))
                return;
            if (!DA.GetData("CollisionDistance", ref iCollisionDistance))
                return;
            if (!DA.GetData("CollisionWeight", ref iCollisionWeight))
                return;
            if (!DA.GetData("BendingResistanceWeight", ref iBendingResistanceWeight))
                return;

            DA.GetData("AttractorPoint", ref iAttractor);
            DA.GetData("MaxDistanceFromAttractor", ref iMaxDistFromAttractor);

            DA.GetData("MaxVertexCount", ref iMaxVertexCount);

            if (iReset || myMeshGrowthSystem == null)
                myMeshGrowthSystem = new Core.DifferentialGrowth.MeshDifferentialGrowth(iStartingMesh);

            (Point3d, double)? attractor;
            if (iAttractor == Point3d.Unset || iMaxDistFromAttractor == 0)
                attractor = null;
            else
                attractor = (iAttractor, iMaxDistFromAttractor);

            myMeshGrowthSystem.Update(
                iGrow,
                iMaxVertexCount,
                (iCollisionDistance, iCollisionWeight),
                (iEdgeLength, iEdgeLengthConstrainWeight),
                iBendingResistanceWeight,
                attractor);

            DA.SetData("Mesh", myMeshGrowthSystem.GetRhinoMesh());
        }
    }
}