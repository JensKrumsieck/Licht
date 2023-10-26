using System.Diagnostics;
using System.Numerics;
using Licht.Vulkan;
using Silk.NET.Assimp;

namespace Licht.Rendering;

public static unsafe class MeshImporter
{
    private static readonly Assimp Assimp = Assimp.GetApi();
    public static ImportedMesh[] FromFile(string filename)
    {
        var pScene = Assimp.ImportFile(filename, (uint) PostProcessPreset.TargetRealTimeFast);
        var meshes = VisitNode(pScene->MRootNode, pScene);
        Assimp.ReleaseImport(pScene);
        return meshes.ToArray();
    }

    private static List<ImportedMesh> VisitNode(Node* pNode, Silk.NET.Assimp.Scene* pScene)
    {
        var meshes = new List<ImportedMesh>();
        for (var m = 0; m < pNode->MNumMeshes; m++)
        {
            var pMesh = pScene->MMeshes[pNode->MMeshes[m]];
            meshes.Add(VisitMesh(pMesh));
        }
        for(var i = 0; i < pNode->MNumChildren; i++) meshes.AddRange(VisitNode(pNode->MChildren[i], pScene));
        return meshes;
    }

    private static ImportedMesh VisitMesh(Silk.NET.Assimp.Mesh* pMesh)
    {
        var vertices = new List<Vertex>();
        var indices = new List<uint>();
        for (var i = 0; i < pMesh->MNumVertices; i++)
        {
            var vertex = new Vertex
            {
                Position = pMesh->MVertices[i]
            };
            if (pMesh->MNormals != null) vertex.Normal = pMesh->MNormals[i];
            if (pMesh->MTextureCoords[0] != null)
            {
                var pTex3 = pMesh->MTextureCoords[0][i];
                vertex.TextureCoordinate = new Vector2(pTex3.X, pTex3.Y);
            }

            if (pMesh->MColors[0] != null) vertex.Color = pMesh->MColors[0][i];
            if(vertex.Color == Vector4.Zero) vertex.Color = Vector4.One;
            vertices.Add(vertex);
        }

        for (var j = 0; j < pMesh->MNumFaces; j++)
        {
            var face = pMesh->MFaces[j];
            for (uint i = 0; i < face.MNumIndices; i++) indices.Add(face.MIndices[i]);
        }

        return new ImportedMesh(vertices.ToArray(),indices.ToArray());
    }
}

public record ImportedMesh(Vertex[] Vertices, uint[] Indices)
{
    public Mesh Process(VkGraphicsDevice device) => new(device, Vertices, Indices);
}