using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Plankton; //Note: AddReference to the project -> browse -> find file in Gh libraries
using PlanktonGh;
using Rhino.Geometry;


namespace LightBug.Core.DifferentialGrowth
{
    /// <summary>
    /// Implements differential growth on a mesh.
    /// Inspired by: 
    /// - "CSharp Scripting and Plugin Development for Grasshopper" tutorials by Long Nguyen
    /// - "FloraForm" by n-e-r-v-o-u-s: Jessica Rosenkrantz and Jesse Louis-Rosenberg
    /// </summary>
    public class MeshDifferentialGrowth
    {
        private PlanktonMesh planktonMesh;

        // Accumulators for moves and weights computed per vertex.
        private List<Vector3d> totalWeightedMoves;
        private List<double> totalWeights;

        public MeshDifferentialGrowth(Mesh startingMesh)
        {
            if (startingMesh == null || !startingMesh.IsValid)
                throw new ArgumentNullException(nameof(startingMesh));

            planktonMesh = startingMesh.ToPlanktonMesh();
        }

        /// <summary>
        /// Returns the current state of the mesh as a Rhino Mesh.
        /// </summary>
        public Mesh GetRhinoMesh() => planktonMesh.ToRhinoMesh();

        public void Update(
            bool grow,
            int maxVertexCount,
            (double Dist, double Weight) collision,
            (double DesiredDist, double Weight) edgeLength,
            double bendingResistanceWeight,
            (Point3d Pt, double MaxDist)? attractor = null)
        {
            if (grow)
                SplitLongEdges(maxVertexCount, edgeLength.DesiredDist);

            InitializeAccumulators(planktonMesh.Vertices.Count);

            ProcessCollisions(collision.Dist, collision.Weight);
            ProcessBendingResistance(bendingResistanceWeight);

            if (!grow)
                ProcessEdgeLengthConstraint(edgeLength.DesiredDist, edgeLength.Weight);

            if (attractor.HasValue)
                ProcessAttractor(attractor.Value.Pt, attractor.Value.MaxDist);

            UpdateVertexPositions();
        }

        private void InitializeAccumulators(int vertexCount)
        {
            totalWeightedMoves = Enumerable.Range(0, vertexCount)
                                           .Select(_ => Vector3d.Zero)
                                           .ToList();
            totalWeights = Enumerable.Repeat(0.0, vertexCount).ToList();
        }

        private void ProcessCollisions(double CollisionDistance, double CollisionWeight)
        {
            RTree rTree = new RTree();

            // Insert each vertex into the RTree.
            for (int i = 0; i < planktonMesh.Vertices.Count; i++)
                rTree.Insert(planktonMesh.Vertices[i].ToPoint3d(), i);

            // Process collisions between vertex pairs.
            for (int i = 0; i < planktonMesh.Vertices.Count; i++)
            {
                Point3d vI = planktonMesh.Vertices[i].ToPoint3d();
                Sphere searchSphere = new Sphere(vI, CollisionDistance);

                List<int> collisionIndices = new List<int>();

                rTree.Search(
                    searchSphere, // whenever finds something in the sphere
                    (sender, args) => { 
                        if (i < args.Id) 
                            collisionIndices.Add(args.Id); 
                    }); //do this

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

        /// <summary>
        /// Edges act as springs that try to keep their desired length
        /// </summary>
        /// <param name="EdgeLength"></param>
        /// <param name="EdgeLengthConstrainWeight"></param>
        private void ProcessEdgeLengthConstraint(double EdgeLength, double EdgeLengthConstrainWeight)
        {
            double edgeLengthSqr = EdgeLength * EdgeLength;

            for (int k = 0; k < planktonMesh.Halfedges.Count; k += 2)
            {
                int a = planktonMesh.Halfedges[k].StartVertex;
                int b = planktonMesh.Halfedges[k + 1].StartVertex;

                Point3d va = planktonMesh.Vertices[a].ToPoint3d();
                Point3d vb = planktonMesh.Vertices[b].ToPoint3d();

                Vector3d move;
                if (va.DistanceToSquared(vb) < edgeLengthSqr) 
                    move = vb - va;
                else
                    move = va - vb;

                move *= (move.Length - EdgeLength) * 0.5 / move.Length;

                totalWeightedMoves[a] += move * EdgeLengthConstrainWeight;
                totalWeightedMoves[b] += -move * EdgeLengthConstrainWeight;
                totalWeights[a] += EdgeLengthConstrainWeight;
                totalWeights[b] += EdgeLengthConstrainWeight;
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

        private void SplitLongEdges(int MaxVertexCount, double CollisionDistance)
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