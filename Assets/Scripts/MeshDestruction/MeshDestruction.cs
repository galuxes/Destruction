using System.Collections.Generic;
using UnityEngine;

namespace MeshDestruction
{
    class TmpMesh
    {
        public List<Vector3> vertList, normList;
        public List<List<int>> triList ;//list of lists of triangles organized by submesh
        public List<Vector2> UVList;
        public Bounds bounds;

        public TmpMesh()
        {
            vertList = new List<Vector3>();
            normList = new List<Vector3>();
            UVList = new List<Vector2>();
            triList = new List<List<int>>();
            bounds = new Bounds();
        }
    }
    struct Triangle
    {
        public Vector3 vert1, vert2, vert3, norm1, norm2, norm3;
        public Vector2 uv1, uv2, uv3;
    }

    public static class MeshDestruction
    {
        public static List<GameObject> DestroyMesh(Transform original, Vector3 point, int numPieces)//access function
        {
            var tmpMeshList = new List<TmpMesh>();
            var originalPiece = MeshToTmpMesh(original.GetComponent<MeshFilter>().mesh);
            
            var piece = originalPiece;
            for (var i = 0; i < numPieces; i++)//current implementation will recursively slice the new piece created until number of pieces is satisfied
            {
                var tmpPiece = MakeCut(ref piece);
                tmpMeshList.Add(piece);
                piece = tmpPiece;
            }
            tmpMeshList.Add(piece);
            
            return GenerateGameObjects(original, tmpMeshList);
        }
        private static List<GameObject> GenerateGameObjects(Transform original, List<TmpMesh> tmpMeshes)
        {
            var list = new List<GameObject>();
            foreach (var tmpMesh in tmpMeshes)
            {
                if (tmpMesh.triList.Count > 0 && tmpMesh.triList[0].Count >= 3)
                    list.Add(GenerateGameObject(original, tmpMesh));
            }
            return list;
        }
        private static TmpMesh MakeCut(ref TmpMesh meshToCut)
        {//TODO:: loop if created piece is too small or create an artificial buffer to prevent that from happening

            var plane = new Plane(Random.onUnitSphere, new Vector3(Random.Range(meshToCut.bounds.min.x, meshToCut.bounds.max.x), Random.Range(meshToCut.bounds.min.y, meshToCut.bounds.max.y), Random.Range(meshToCut.bounds.min.z, meshToCut.bounds.max.z)));//generate random plane that passes through a random point inside the object
            /*var plane = new Plane(Vector3.right,
                new Vector3((meshToCut.bounds.min.x + meshToCut.bounds.max.x) / 2,
                    (meshToCut.bounds.min.y + meshToCut.bounds.max.y) / 2,
                    (meshToCut.bounds.min.z + meshToCut.bounds.max.z) / 2));*/
            
            TmpMesh newMesh = new TmpMesh();//new piece to be created made up of everything on pos side of plane
            TmpMesh replacementMesh = new TmpMesh();//updated version of the cut piece

            Vector3? interiorFaceReferenceVert = null;
            Vector3? interiorFaceReferenceNorm = null;
            Vector2? interiorFaceReferenceUV = null;

            for (var index1 = 0; index1 < meshToCut.triList.Count; index1++)
            {
                var submesh = meshToCut.triList[index1];
                for (var index2 = 0; index2 < submesh.Count; index2 += 3)
                {
                    var sideA = plane.GetSide(meshToCut.vertList[submesh[index2]]);
                    var sideB = plane.GetSide(meshToCut.vertList[submesh[index2 + 1]]);
                    var sideC = plane.GetSide(meshToCut.vertList[submesh[index2 + 2]]);

                    var sideCount = (sideA ? 1 : 0) + (sideB ? 1 : 0) + (sideC ? 1 : 0);

                    if (sideCount == 0) //no verts on pos side of plane add tri to replacement mesh
                    {
                        var triangle = new Triangle
                        {
                            vert1 = meshToCut.vertList[submesh[index2]],
                            vert2 = meshToCut.vertList[submesh[index2 + 1]],
                            vert3 = meshToCut.vertList[submesh[index2 + 2]],

                            norm1 = meshToCut.normList[submesh[index2]],
                            norm2 = meshToCut.normList[submesh[index2 + 1]],
                            norm3 = meshToCut.normList[submesh[index2 + 2]],

                            uv1 = meshToCut.UVList[submesh[index2]],
                            uv2 = meshToCut.UVList[submesh[index2 + 1]],
                            uv3 = meshToCut.UVList[submesh[index2 + 2]]
                        };
                        AddTriangle(ref replacementMesh, triangle, index1);
                        continue;
                    }

                    if (sideCount == 3) //all verts on pos side of plane add tri to new mesh
                    {
                        var triangle = new Triangle
                        {
                            vert1 = meshToCut.vertList[submesh[index2]],
                            vert2 = meshToCut.vertList[submesh[index2 + 1]],
                            vert3 = meshToCut.vertList[submesh[index2 + 2]],

                            norm1 = meshToCut.normList[submesh[index2]],
                            norm2 = meshToCut.normList[submesh[index2 + 1]],
                            norm3 = meshToCut.normList[submesh[index2 + 2]],

                            uv1 = meshToCut.UVList[submesh[index2]],
                            uv2 = meshToCut.UVList[submesh[index2 + 1]],
                            uv3 = meshToCut.UVList[submesh[index2 + 2]]
                        };
                        AddTriangle(ref newMesh, triangle, index1);
                        continue;
                    }

                    int vertIndex = sideA == sideB ? 2 : sideA == sideC ? 1 : 0; //calc vert index based off of which vert is isolated

                    var triangle1 = new Triangle // triangle for side with 1 vert
                    {
                        vert1 = meshToCut.vertList[submesh[index2 + vertIndex]],
                        //vert2 = ,
                        //vert3 = ,

                        norm1 = meshToCut.normList[submesh[index2 + vertIndex]],
                        //norm2 = ,
                        //norm3 = ,

                        uv1 = meshToCut.UVList[submesh[index2 + vertIndex]],
                        //uv2 = ,
                        //uv3 = 
                    };

                    var triangle2 = new Triangle //triangle for side with 2 verts
                    {
                        //vert1 = ,
                        vert2 = meshToCut.vertList[submesh[index2 + ((vertIndex + 1) % 3)]],
                        vert3 = meshToCut.vertList[submesh[index2 + ((vertIndex + 2) % 3)]],

                        //norm1 = ,
                        norm2 = meshToCut.normList[submesh[index2 + ((vertIndex + 1) % 3)]],
                        norm3 = meshToCut.normList[submesh[index2 + ((vertIndex + 2) % 3)]],

                        //uv1 = ,
                        uv2 = meshToCut.UVList[submesh[index2 + ((vertIndex + 1) % 3)]],
                        uv3 = meshToCut.UVList[submesh[index2 + ((vertIndex + 2) % 3)]]
                    };

                    var triangle3 = new Triangle //triangle for side with 2 verts
                    {
                        //vert1 = meshToCut.vertList[tri],
                        vert2 = meshToCut.vertList[submesh[index2 + ((vertIndex + 2) % 3)]],
                        //vert3 = meshToCut.vertList[tri + 2],

                        //norm1 = meshToCut.normList[tri],
                        norm2 = meshToCut.normList[submesh[index2 + ((vertIndex + 2) % 3)]],
                        //norm3 = meshToCut.normList[tri + 2],

                        //uv1 = meshToCut.UVList[tri],
                        uv2 = meshToCut.UVList[submesh[index2 + ((vertIndex + 2) % 3)]],
                        //uv3 = meshToCut.UVList[tri + 2]
                    };

                    Triangle interiorFaceTriangle = new Triangle();
                    bool referenceVertStored = false;
                    
                    if (interiorFaceReferenceVert.HasValue)
                    {
                        referenceVertStored = true;
                        interiorFaceTriangle.vert1 = interiorFaceReferenceVert.Value;
                        interiorFaceTriangle.norm1 = interiorFaceReferenceNorm.Value;
                        interiorFaceTriangle.uv1 = interiorFaceReferenceUV.Value;
                    }
                    
                    
                    for (int i = 1; i < 3; i++)
                    {
                        var pos1 = meshToCut.vertList[submesh[index2 + ((vertIndex + i) % 3)]];
                        var pos2 = meshToCut.vertList[submesh[index2 + vertIndex]];
                        var v = pos1 - pos2;
                        Ray ray = new Ray(meshToCut.vertList[submesh[index2 + vertIndex]], v);
                        Vector3 newVert;
                        plane.Raycast(ray, out var enter);
                        newVert = ray.GetPoint(enter); //new vert pos

                        var magnitude = v.magnitude;

                        var newNorm = Vector3.Lerp(meshToCut.normList[submesh[index2 + vertIndex]],
                            meshToCut.normList[submesh[index2 + ((vertIndex + i) % 3)]],
                            enter / magnitude); //new norm is a value in between the original verts norm and its connecting verts norm scaled based of the relative distance of the new vert compared to the old

                        var newUV = Vector2.Lerp(meshToCut.UVList[submesh[index2 + vertIndex]],
                            meshToCut.UVList[submesh[index2 + ((vertIndex + i) % 3)]], enter / magnitude); //same as norm calc

                        if (i == 1) //if first loop then it is first vert created and add the information calculated to its appropriate triangles
                        {
                            triangle1.vert2 = newVert;
                            triangle1.norm2 = newNorm;
                            triangle1.uv2 = newUV;

                            triangle3.vert1 = newVert;
                            triangle3.norm1 = newNorm;
                            triangle3.uv1 = newUV;
                            
                            triangle2.vert1 = newVert;
                            triangle2.norm1 = newNorm;
                            triangle2.uv1 = newUV;

                            if (referenceVertStored)
                            {
                                if (plane.GetSide(meshToCut.vertList[submesh[index2 + vertIndex]]))
                                {
                                    interiorFaceTriangle.vert3 = newVert;
                                    interiorFaceTriangle.norm3 = plane.normal;
                                    interiorFaceTriangle.uv3 = newUV;
                                }
                                else
                                {
                                    interiorFaceTriangle.vert2 = newVert;
                                    interiorFaceTriangle.norm2 = plane.normal;
                                    interiorFaceTriangle.uv2 = newUV;
                                }
                            }
                        }
                        else
                        {
                            triangle1.vert3 = newVert;
                            triangle1.norm3 = newNorm;
                            triangle1.uv3 = newUV;

                            triangle3.vert3 = newVert;
                            triangle3.norm3 = newNorm;
                            triangle3.uv3 = newUV;
                            
                            if (referenceVertStored)
                            {
                                if (!plane.GetSide(meshToCut.vertList[submesh[index2 + vertIndex]]))
                                {
                                    interiorFaceTriangle.vert3 = newVert;
                                    interiorFaceTriangle.norm3 = plane.normal;
                                    interiorFaceTriangle.uv3 = newUV;
                                }
                                else
                                {
                                    interiorFaceTriangle.vert2 = newVert;
                                    interiorFaceTriangle.norm2 = plane.normal;
                                    interiorFaceTriangle.uv2 = newUV;
                                }
                            }
                        }
                        
                        if (!referenceVertStored)
                        {
                            interiorFaceReferenceVert = newVert;
                            interiorFaceReferenceNorm = plane.normal;
                            interiorFaceReferenceUV = newUV;
                        }
                    }
                    
                    if (referenceVertStored)
                    {
                        AddTriangle(ref newMesh, interiorFaceTriangle, index1);
                        var tmpVert = interiorFaceTriangle.vert2;
                        var tmpNorm = interiorFaceTriangle.norm2;
                        var tmpUV = interiorFaceTriangle.uv2;
                        interiorFaceTriangle.vert2 = interiorFaceTriangle.vert3;
                        interiorFaceTriangle.norm2 = interiorFaceTriangle.norm3;
                        interiorFaceTriangle.uv2 = interiorFaceTriangle.uv3;
                        interiorFaceTriangle.vert3 = tmpVert;
                        interiorFaceTriangle.norm3 = tmpNorm;
                        interiorFaceTriangle.uv3 = tmpUV;
                        AddTriangle(ref replacementMesh, interiorFaceTriangle, index1);
                    }

                    if (sideCount == 1) //one vert on positive side of plane
                    {
                        AddTriangle(ref newMesh, triangle1, index1);

                        AddTriangle(ref replacementMesh, triangle2, index1);

                        AddTriangle(ref replacementMesh, triangle3, index1);

                        continue;
                    }

                    if (sideCount == 2) //two vert on positive side of plane
                    {
                        AddTriangle(ref replacementMesh, triangle1, index1);

                        AddTriangle(ref newMesh, triangle2, index1);

                        AddTriangle(ref newMesh, triangle3, index1);
                    }
                }
            }

            meshToCut = replacementMesh;
            return newMesh;
        }
        private static GameObject GenerateGameObject(Transform original, TmpMesh tmpMesh)// turn generated mesh into object
        {
            var obj = new GameObject(original.name)
            {
                transform =
                {
                    position = original.position,
                    rotation = original.rotation,
                    localScale = original.localScale
                }
            };

            obj.AddComponent<MeshFilter>().mesh = TmpMeshToMesh(tmpMesh);

            obj.AddComponent<MeshRenderer>().materials = original.GetComponent<MeshRenderer>().materials;
            
            if(obj.transform.localScale.magnitude > 0)
                obj.AddComponent<MeshCollider>().convex = true;
            
            return obj;
        }
        private static TmpMesh MeshToTmpMesh(Mesh mesh)
        {
            mesh.RecalculateBounds();
            
            TmpMesh tmpMesh = new TmpMesh
            {
                vertList = new List<Vector3>(mesh.vertices),
                normList = new List<Vector3>(mesh.normals),
                UVList = new List<Vector2>(mesh.uv),
                triList = new List<List<int>>()
            };
            for (int i = 0; i < mesh.subMeshCount; i++)
                tmpMesh.triList.Add(new List<int>(mesh.GetTriangles(i)));
            tmpMesh.bounds = mesh.bounds;
            return tmpMesh;
        }
        private static Mesh TmpMeshToMesh(TmpMesh tmpMesh)
        {
            var mesh = new Mesh
            {
                vertices = tmpMesh.vertList.ToArray(),
                normals = tmpMesh.normList.ToArray(),
                uv = tmpMesh.UVList.ToArray()
            };
            for(var i = 0; i < tmpMesh.triList.Count; i++)
                mesh.SetTriangles(tmpMesh.triList[i], i, true);
            mesh.bounds = tmpMesh.bounds;

            return mesh;
        }
        private static void AddTriangle( ref TmpMesh tmpMesh, Triangle tri, int submesh)
        {
            if (tmpMesh.triList.Count - 1 < submesh)
                tmpMesh.triList.Add(new List<int>());

            tmpMesh.triList[submesh].Add(tmpMesh.vertList.Count);
            tmpMesh.vertList.Add(tri.vert1);
            tmpMesh.triList[submesh].Add(tmpMesh.vertList.Count);
            tmpMesh.vertList.Add(tri.vert2);
            tmpMesh.triList[submesh].Add(tmpMesh.vertList.Count);
            tmpMesh.vertList.Add(tri.vert3);
            
            tmpMesh.normList.Add(tri.norm1);
            tmpMesh.normList.Add(tri.norm2);
            tmpMesh.normList.Add(tri.norm3);
            
            tmpMesh.UVList.Add(tri.uv1);
            tmpMesh.UVList.Add(tri.uv2);
            tmpMesh.UVList.Add(tri.uv3);
        }
    }
}
