using System.Collections.Generic;
using Plankton; //AddReference to the project -> browse -> find file in Gh libraries
using PlanktonGh;
using Rhino.Geometry;


namespace LightBug.Core.DifferentialGrowth
{
    /// <summary>
    /// Inspired by: 
    /// - "CSharp Scripting and Plugin Development for Grasshopper" tutorials by Long Nguyen
    /// - "FloraForm" by n-e-r-v-o-u-s: Jessica Rosenkrantz and Jesse Louis-Rosenberg
    /// </summary>
    public class MeshSystem
    {
        private PlanktonMesh planktonMesh = null;

        private List<Vector3d> totalWeightedMoves;
        private List<double> totalWeights;

        public MeshSystem(Mesh startingMesh)
        {
            planktonMesh = startingMesh.ToPlanktonMesh();
        }

        public Mesh GetRhinoMesh()
        {
            return planktonMesh.ToRhinoMesh();
        }

        public Mesh Update(
            bool Grow,
            int MaxVertexCount,
            (double Dist, double Weight) Collision,
            double EdgeLengthConstraintWeight,
            double BendingResistanceWeight,
            (Point3d Pt, double maxDist)? Attractor = null)
        {
            if (Grow)
                SplitAllLongEdges(MaxVertexCount, Collision.Dist);

            totalWeightedMoves = new List<Vector3d>();
            totalWeights = new List<double>();

            for (int i = 0; i < planktonMesh.Vertices.Count; i++)
            {
                totalWeightedMoves.Add(new Vector3d(0, 0, 0));
                totalWeights.Add(0.0);
            }

            ProcessCollisionsUsingRTree(Collision.Dist, Collision.Weight);

            ProcessBendingResistance(BendingResistanceWeight);
            ProcessEdgeLengthConstraint(Collision.Dist, EdgeLengthConstraintWeight);

            if (Attractor.HasValue)
                ProcessAttractor(Attractor.Value.Pt, Attractor.Value.maxDist);

            UpdateVertexPositions();

            return planktonMesh.ToRhinoMesh();
        }

        private void ProcessCollisionsUsingRTree(double CollisionDistance, double CollisionWeight)
        {
            RTree rTree = new RTree();

            for (int i = 0; i < planktonMesh.Vertices.Count; i++)
                rTree.Insert(planktonMesh.Vertices[i].ToPoint3d(), i);

            for (int i = 0; i < planktonMesh.Vertices.Count; i++)
            {
                Point3d vI = planktonMesh.Vertices[i].ToPoint3d();
                Sphere searchSphere = new Sphere(vI, CollisionDistance);

                List<int> collisionIndices = new List<int>();

                rTree.Search(
                    searchSphere, // whenever finds something in the sphere
                    (sender, args) => { if (i < args.Id) collisionIndices.Add(args.Id); }); //do this

                foreach (int j in collisionIndices)
                {
                    Vector3d move = planktonMesh.Vertices[j].ToPoint3d() - planktonMesh.Vertices[i].ToPoint3d();
                    double currentDistance = move.Length;

                    move *= 0.5 * (currentDistance - CollisionDistance) / currentDistance;

                    totalWeightedMoves[i] += CollisionWeight * move;
                    totalWeightedMoves[j] += -CollisionWeight * move;
                    totalWeights[i] += CollisionWeight;
                    totalWeights[j] += CollisionWeight;
                }
            }
        }

        private void ProcessEdgeLengthConstraint(double CollisionDistance, double EdgeLengthConstrainWeight)
        {
            for (int k = 0; k < planktonMesh.Halfedges.Count; k += 2)
            {
                int i = planktonMesh.Halfedges[k].StartVertex;
                int j = planktonMesh.Halfedges[k + 1].StartVertex;

                Point3d vI = planktonMesh.Vertices[i].ToPoint3d();
                Point3d vj = planktonMesh.Vertices[j].ToPoint3d();

                if (vI.DistanceTo(vj) < CollisionDistance) continue;

                Vector3d move = vj - vI;
                move *= (move.Length - CollisionDistance) * 0.5 / move.Length;

                totalWeightedMoves[i] += move * EdgeLengthConstrainWeight;
                totalWeightedMoves[j] += -move * EdgeLengthConstrainWeight;
                totalWeights[i] += EdgeLengthConstrainWeight;
                totalWeights[j] += EdgeLengthConstrainWeight;
            }
        }

        private void ProcessBendingResistance(double BendingResistanceWeight)
        {
            for (int k = 0; k < planktonMesh.Halfedges.Count; k += 2)
            {
                int i = planktonMesh.Halfedges[k].StartVertex;
                int j = planktonMesh.Halfedges[k + 1].StartVertex;
                int p = planktonMesh.Halfedges[planktonMesh.Halfedges[k].PrevHalfedge].StartVertex;
                int q = planktonMesh.Halfedges[planktonMesh.Halfedges[k + 1].PrevHalfedge].StartVertex;

                Point3d vI = planktonMesh.Vertices[i].ToPoint3d();
                Point3d vJ = planktonMesh.Vertices[j].ToPoint3d();
                Point3d vP = planktonMesh.Vertices[p].ToPoint3d();
                Point3d vQ = planktonMesh.Vertices[q].ToPoint3d();

                Vector3d nP = Vector3d.CrossProduct(vJ - vI, vP - vI);
                Vector3d nQ = Vector3d.CrossProduct(vQ - vI, vJ - vI);

                //Vector3d planeNormal = (nP + nQ) / (nP.Length + nQ.Length);
                Vector3d planeNormal = (nP + nQ); //average
                Point3d planeOrigin = 0.25 * (vI + vJ + vP + vQ);
                Plane plane = new Plane(planeOrigin, planeNormal);

                totalWeightedMoves[i] += BendingResistanceWeight * (plane.ClosestPoint(vI) - vI);
                totalWeightedMoves[j] += BendingResistanceWeight * (plane.ClosestPoint(vJ) - vJ);
                totalWeightedMoves[p] += BendingResistanceWeight * (plane.ClosestPoint(vP) - vP);
                totalWeightedMoves[q] += BendingResistanceWeight * (plane.ClosestPoint(vQ) - vQ);
                totalWeights[i] += BendingResistanceWeight;
                totalWeights[j] += BendingResistanceWeight;
                totalWeights[p] += BendingResistanceWeight;
                totalWeights[q] += BendingResistanceWeight;
            }
        }

        private void ProcessAttractor(Point3d Attractor, double MaxDistFromAttractor)
        {
            double maxDistSq = MaxDistFromAttractor * MaxDistFromAttractor; // Use squared distance for comparisons

            for (int i = 0; i < planktonMesh.Vertices.Count; i++)
            {
                Point3d vI = planktonMesh.Vertices[i].ToPoint3d();
                double distanceSq = vI.DistanceToSquared(Attractor);

                if (distanceSq > maxDistSq)
                {
                    totalWeightedMoves[i] *= 0;
                    totalWeights[i] += 1; // Adjust weight to count this move appropriately
                }
                else
                {
                    // Vector3d moveTowardsAttractor = Attractor - vI;
                    double scale = distanceSq / maxDistSq; // Normalized scale based on distance
                                                           // moveTowardsAttractor *= (1 - scale); // The closer to the attractor, the smaller the move.

                    // Apply the weighted move
                    totalWeightedMoves[i] *= (1 - scale);
                    totalWeights[i] += 1; // Adjust weight to count this move appropriately
                }
            }
        }

        private void SplitAllLongEdges(int MaxVertexCount, double CollisionDistance)
        {
            int halfEdgeCount = planktonMesh.Halfedges.Count;

            for (int k = 0; k < halfEdgeCount; k += 2)
            {
                if (planktonMesh.Vertices.Count < MaxVertexCount &&
                    planktonMesh.Halfedges.GetLength(k) > 0.99 * CollisionDistance)
                {
                    SplitEdge(k);
                }
            }
        }

        private void UpdateVertexPositions()
        {
            for (int i = 0; i < planktonMesh.Vertices.Count; i++)
            {
                if (totalWeights[i] == 0.0)
                    continue;

                Vector3d move = totalWeightedMoves[i] / totalWeights[i];
                Point3d newPosition = planktonMesh.Vertices[i].ToPoint3d() + move;
                planktonMesh.Vertices.SetVertex(i, newPosition.X, newPosition.Y, newPosition.Z);
            }
        }

        private void SplitEdge(int edgeIndex)
        {
            int newHalfEdgeIndex = planktonMesh.Halfedges.SplitEdge(edgeIndex);

            planktonMesh.Vertices.SetVertex(
                planktonMesh.Vertices.Count - 1,
                0.5 * (planktonMesh.Vertices[planktonMesh.Halfedges[edgeIndex].StartVertex].ToPoint3d() + planktonMesh.Vertices[planktonMesh.Halfedges[edgeIndex + 1].StartVertex].ToPoint3d()));

            if (planktonMesh.Halfedges[edgeIndex].AdjacentFace >= 0)
                planktonMesh.Faces.SplitFace(newHalfEdgeIndex, planktonMesh.Halfedges[edgeIndex].PrevHalfedge);

            if (planktonMesh.Halfedges[edgeIndex + 1].AdjacentFace >= 0)
                planktonMesh.Faces.SplitFace(edgeIndex + 1, planktonMesh.Halfedges[planktonMesh.Halfedges[edgeIndex + 1].NextHalfedge].NextHalfedge);
        }
    }
}