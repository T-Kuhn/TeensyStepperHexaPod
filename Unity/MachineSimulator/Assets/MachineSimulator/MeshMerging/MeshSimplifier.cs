using UnityEngine;

namespace MeshMerging
{
    public static class MeshSimplifier
    {
        /// <summary>
        /// Simplifies a mesh using Unity's built-in mesh simplification.
        /// </summary>
        /// <param name="mesh">The mesh to simplify</param>
        /// <param name="quality">Quality level (0-1), where 1 = highest quality (least simplification)</param>
        /// <returns>A new simplified mesh</returns>
        public static Mesh Simplify(Mesh sourceMesh, float quality)
        {
            if (sourceMesh == null) // verify that the mesh filter actually has a mesh
            {
                return null;
            }

            // Create our mesh simplifier and setup our entire mesh in it
            var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();
            meshSimplifier.Initialize(sourceMesh);

            meshSimplifier.SimplifyMesh(quality);

            return meshSimplifier.ToMesh();
        }
    }
}