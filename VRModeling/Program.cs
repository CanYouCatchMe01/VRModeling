using StereoKit;
using System;
using CGALDotNet;
using CGALDotNet.Triangulations;
using CGALDotNetGeometry.Numerics;
using CGALDotNetGeometry.Shapes;
using CGALDotNet.Polyhedra;
using CGALDotNet.Extensions;
using CGALDotNetGeometry.Extensions;
using CGALDotNet.Geometry;

namespace StereoKitProject2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Initialize StereoKit
            SKSettings settings = new SKSettings
            {
                appName = "StereoKitProject2",
                assetsFolder = "Assets",
            };
            if (!SK.Initialize(settings))
                Environment.Exit(1);


            // Create assets used by the app
            Pose cubePose = new Pose(0, 0, -0.5f, Quat.Identity);
            Model cube = Model.FromMesh(
                Mesh.GenerateRoundedCube(Vec3.One * 0.1f, 0.02f),
                Default.MaterialUI);

            Matrix floorTransform = Matrix.TS(0, -1.5f, 0, new Vec3(30, 0.1f, 30));
            Material floorMaterial = new Material(Shader.FromFile("floor.hlsl"));
            floorMaterial.Transparency = Transparency.Blend;

            //Random points
            var box = new Box3d(-1, 1);
            var randomPoints = Point3d.RandomPoints(1, 10, box);

            DelaunayTriangulation3<EEK> m_triangulationm_triangulation = new DelaunayTriangulation3<EEK>(randomPoints);
            var hull = m_triangulationm_triangulation.ComputeHull();

            var verticesStereo = new Vertex[hull.VertexCount];

            {
                //points
                var pointsCGAL = new Point3d[hull.VertexCount];
                hull.GetPoints(pointsCGAL, pointsCGAL.Length);

                for (int i = 0; i < verticesStereo.Length; i++)
                {
                    Vec3 pos = new Vec3((float)pointsCGAL[i].x, (float)pointsCGAL[i].y, (float)pointsCGAL[i].z);
                    verticesStereo[i].pos = pos;
                }
            }
            {
                //normals
                var normalsCGAL = new Vector3d[hull.VertexCount];
                hull.GetVertexNormals(normalsCGAL, normalsCGAL.Length);

                for (int i = 0; i < verticesStereo.Length; i++)
                {
                    Vec3 normal = new Vec3((float)normalsCGAL[i].x, (float)normalsCGAL[i].y, (float)normalsCGAL[i].z);
                    verticesStereo[i].norm = normal;
                }
            }
            {
                //colors
                for (int i = 0; i < verticesStereo.Length; i++)
                {
                    verticesStereo[i].col = Color32.White;
                }
            }
            
            //indices
            var indicesCGAL = new int[hull.FaceCount * 3];
            hull.GetTriangleIndices(indicesCGAL, indicesCGAL.Length);
            var indiciesStereo = new uint[indicesCGAL.Length];

            for (int i = 0; i < indicesCGAL.Length; i++)
            {
                indiciesStereo[i] = (uint)indicesCGAL[i];
            }


            Mesh mesh = new Mesh();
            mesh.SetVerts(verticesStereo);
            mesh.SetInds(indiciesStereo);
            var triangulateModel = Model.FromMesh(mesh, Default.MaterialUI);
            Pose triangulatePose = new Pose(0, 0, -0.5f, Quat.Identity);

            // Core application loop
            while (SK.Step(() =>
            {
                if (SK.System.displayType == Display.Opaque)
                    Default.MeshCube.Draw(floorMaterial, floorTransform);

                UI.Handle("Cube", ref cubePose, cube.Bounds);
                UI.Handle("Triangulate", ref triangulatePose, triangulateModel.Bounds);
                //cube.Draw(cubePose.ToMatrix());
                triangulateModel.Draw(triangulatePose.ToMatrix());
            })) ;
            SK.Shutdown();
        }
    }
}
