using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plankton; //AddReference to the project -> browse -> find file in Gh libraries
using PlanktonGh;
using Rhino.Geometry;


namespace LightBug.Core.DifferentialGrowth
{
    public class MeshSystem
    {
        public PlanktonMesh PlanktonMesh = null;

        private List<Vector3d> totalWeightedMoves;
        private List<double> totalWeights;

        public MeshSystem(Mesh startingMesh)
        {
            PlanktonMesh = startingMesh.ToPlanktonMesh();
        }

        public Mesh GetRhinoMesh()
        {
            return PlanktonMesh.ToRhinoMesh();
        }

        private void Update(
            Point3d Attractor,
            double MaxDistFromAttractor,
            Mesh StartingMesh,
            bool Grow,
            int MaxVertexCount,
            double EdgeLengthConstraintWeight,
            double CollisionDistance,
            double CollisionWeight,
            double BendingResistanceWeight,
            ref object Mesh)
        {
            //if (Grow)
            //    SplitAllLongEdges(MaxVertexCount, CollisionDistance);

            //totalWeightedMoves = new List<Vector3d>();
            //totalWeights = new List<double>();

            //for (int i = 0; i < PlanktonMesh.Vertices.Count; i++)
            //{
            //    totalWeightedMoves.Add(new Vector3d(0, 0, 0));
            //    totalWeights.Add(0.0);
            //}

            //ProcessCollisionsUsingRTree(CollisionDistance, CollisionWeight);

            //ProcessBendingResistance(BendingResistanceWeight);
            //ProcessEdgeLengthConstraint(CollisionDistance, EdgeLengthConstraintWeight);

            //ProcessAttractor(Attractor, MaxDistFromAttractor);

            //UpdateVertexPositions();
            //Mesh = PlanktonMesh.ToRhinoMesh();
        }
    }
}