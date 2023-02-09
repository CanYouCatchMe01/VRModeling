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
using System.Collections.Generic;
using System.Linq.Expressions;

namespace StereoKitProject2
{
    internal class Program
    {
        static void CalculateTriangleMesh(List<Vec3> somePoints, Mesh aMesh)
        {
            //Needs 4 points to calculate hull
            if (somePoints.Count < 4)
            {
                return;
            }

            var inPointsCGAL = new Point3d[somePoints.Count];

            for (int i = 0; i < somePoints.Count; i++)
            {
                inPointsCGAL[i] = new Point3d((double)somePoints[i].x, (double)somePoints[i].y, (double)somePoints[i].z);
            }

            DelaunayTriangulation3<EEK> m_triangulationm_triangulation = new DelaunayTriangulation3<EEK>(inPointsCGAL);
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
                    Color32 color = new Color32(255, 255, 255, 255);

                    //Switch between red/green/blue colors
                    switch (i % 3)
                    {
                        case 0:
                            color = new Color32(255, 0, 0, 255);
                            break;
                        case 1:
                            color = new Color32(0, 255, 0, 255);
                            break;
                        case 2:
                            color = new Color32(0, 0, 255, 255);
                            break;
                        default:
                            break;
                    }

                    verticesStereo[i].col = color;
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

            //Update mesh
            aMesh.SetVerts(verticesStereo);
            aMesh.SetInds(indiciesStereo);
        }
        
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

            //Cube
            Pose cubePose = new Pose(0, 0, -0.5f, Quat.Identity);
            Model cube = Model.FromMesh(
                Mesh.GenerateRoundedCube(Vec3.One * 0.1f, 0.02f),
                Default.MaterialUI);

            //Floor
            Matrix floorTransform = Matrix.TS(0, -1.5f, 0, new Vec3(30, 0.1f, 30));
            Material floorMaterial = new Material(Shader.FromFile("floor.hlsl"));
            floorMaterial.Transparency = Transparency.Blend;

            //Triangulate mesh
            Mesh triangulateMesh = new Mesh();
            Model triangulateModel = Model.FromMesh(triangulateMesh, Default.MaterialUI);
            Pose triangulatePose = new Pose(0, 0, 0, Quat.Identity); //Zero for now else the mesh is wrong

            //Create a list of vec3
            List<Vec3> spherePoints = new List<Vec3>();
            Material material = new Material(Default.ShaderUI);
            material.SetColor("color", new Color(1, 1, 0));

            // Or with a normal for loop
            //Log.Info("Builtin Unlit Materials contain these parameters:");
            //for (int i = 0; i < material.ParamCount; i += 1)
            //{
            //    MatParamInfo info = material.GetParamInfo(i);
            //    Log.Info($"- {info.name} : {info.type}");
            //}

            Model sphereModel = Model.FromMesh(
                Mesh.GenerateSphere(0.015f),
                material);

            // Core application loop
            while (SK.Step(() =>
            {
                var controller = Input.Controller(Handed.Right);
                var hand = Input.Hand(Handed.Right);
                bool handTacking = hand.IsTracked;
                bool controllerTracking = controller.IsTracked;
                if (controller.IsX1JustPressed)
                {
                    Vec3 fingertipWorldPos = hand[FingerId.Index, JointId.Tip].position;
                    var fingertipLocalPos = Matrix.T(fingertipWorldPos) * triangulatePose.ToMatrix().Inverse;

                    spherePoints.Add(fingertipLocalPos.Translation);
                    CalculateTriangleMesh(spherePoints, triangulateMesh);
                    triangulateModel.RecalculateBounds();
                }

                if (SK.System.displayType == Display.Opaque)
                    Default.MeshCube.Draw(floorMaterial, floorTransform);

                UI.Handle("Cube", ref cubePose, cube.Bounds);
                UI.Handle("Triangulate", ref triangulatePose, triangulateModel.Bounds);
                //cube.Draw(cubePose.ToMatrix());
                triangulateModel.Draw(triangulatePose.ToMatrix());

                //Move the small spheres to world position
                for (int i = 0; i < spherePoints.Count; i++)
                {
                    Matrix worldMatrix = Matrix.T(spherePoints[i]) * triangulatePose.ToMatrix();
                    sphereModel.Draw(worldMatrix);
                }
            })) ;
            SK.Shutdown();
        }
    }
}
