using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using LightBug.Core.DifferentialGrowth;

namespace LightBug.Gh
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

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("0bc9e2cd-0684-40ce-a2b4-1aedb3c9f4be");

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Reset", "Reset", "Reset", GH_ParamAccess.item);
            pManager.AddMeshParameter("StartingMesh", "M", "StartingMesh", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Grow", "Grow", "Grow", GH_ParamAccess.item);

            pManager.AddPointParameter("AttractorPoint", "Attractor", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("MaxDistanceFromAttractor", "MaxDist", "", GH_ParamAccess.item);

            pManager.AddNumberParameter("EdgeLengthConstraintWeight", "EdgeLenWeight", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("CollisionDistance", "CollDist", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("CollisionWeight", "CollWeight", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("BendingResistanceWeight", "BendingResWeight", "t", GH_ParamAccess.item);

            pManager.AddIntegerParameter("Max. Vertex Count", "Max. Vertex Count", "Max. Vertex Count", GH_ParamAccess.item, 3000);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "Mesh", "Mesh", GH_ParamAccess.item);
        }

        private MeshSystem myMeshGrowthSystem;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool iReset = true;
            Mesh iStartingMesh = null;
            bool iGrow = false;

            Point3d attractor = new Point3d();
            double maxDistFromAttractor = 0;

            double iEdgeLengthConstrainWeight = 0.0;
            double iCollisionDistance = 0.0;
            double iCollisionWeight = 0.0;
            double iBendingResistanceWeight = 0.0;

            int iMaxVertexCount = 0;

            DA.GetData("Reset", ref iReset);
            DA.GetData("Starting Mesh", ref iStartingMesh);
            DA.GetData("Grow", ref iGrow);
            DA.GetData("Max. Vertex Count", ref iMaxVertexCount);
            DA.GetData("Edge Length Constraint Weight", ref iEdgeLengthConstrainWeight);
            DA.GetData("Collision Distance", ref iCollisionDistance);
            DA.GetData("Collision Weight", ref iCollisionWeight);
            DA.GetData("Bending Resistance Weight", ref iBendingResistanceWeight);

            if (iReset || myMeshGrowthSystem == null)
                myMeshGrowthSystem = new MeshSystem(iStartingMesh);

            myMeshGrowthSystem.Grow = iGrow;
            myMeshGrowthSystem.MaxVertexCount = iMaxVertexCount;
            myMeshGrowthSystem.EdgeLengthConstrainWeight = iEdgeLengthConstrainWeight;
            myMeshGrowthSystem.CollisionWeight = iCollisionWeight;
            myMeshGrowthSystem.BendingResistanceWeight = iBendingResistanceWeight;
            myMeshGrowthSystem.CollisionDistance = iCollisionDistance;

            myMeshGrowthSystem.Update();

            DA.SetData("Mesh", myMeshGrowthSystem.GetRhinoMesh());
        }
    }
}