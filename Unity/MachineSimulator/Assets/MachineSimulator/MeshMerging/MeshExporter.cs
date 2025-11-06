using System.IO;
using System.Text;
using UnityEngine;

namespace MeshMerging
{
    public static class MeshExporter
    {
        public static void SaveToObj(Mesh mesh, string filename)
        {
            var sb = new StringBuilder();

            sb.AppendLine("# Exported Mesh");
            sb.AppendLine($"# Vertices: {mesh.vertexCount}");
            sb.AppendLine($"# Triangles: {mesh.triangles.Length / 3}");
            sb.AppendLine();

            // Write vertices
            foreach (var vertex in mesh.vertices)
            {
                sb.AppendLine($"v {vertex.x} {vertex.y} {vertex.z}");
            }

            sb.AppendLine();

            // Write normals
            if (mesh.normals != null && mesh.normals.Length > 0)
            {
                foreach (var normal in mesh.normals)
                {
                    sb.AppendLine($"vn {normal.x} {normal.y} {normal.z}");
                }

                sb.AppendLine();
            }

            // Write UVs
            if (mesh.uv != null && mesh.uv.Length > 0)
            {
                foreach (var uv in mesh.uv)
                {
                    sb.AppendLine($"vt {uv.x} {uv.y}");
                }

                sb.AppendLine();
            }

            // Write faces
            var triangles = mesh.triangles;
            var hasNormals = mesh.normals != null && mesh.normals.Length > 0;
            var hasUvs = mesh.uv != null && mesh.uv.Length > 0;

            for (var i = 0; i < triangles.Length; i += 3)
            {
                var i1 = triangles[i] + 1;
                var i2 = triangles[i + 1] + 1;
                var i3 = triangles[i + 2] + 1;

                if (hasNormals && hasUvs)
                {
                    sb.AppendLine($"f {i1}/{i1}/{i1} {i2}/{i2}/{i2} {i3}/{i3}/{i3}");
                }
                else if (hasUvs)
                {
                    sb.AppendLine($"f {i1}/{i1} {i2}/{i2} {i3}/{i3}");
                }
                else if (hasNormals)
                {
                    sb.AppendLine($"f {i1}//{i1} {i2}//{i2} {i3}//{i3}");
                }
                else
                {
                    sb.AppendLine($"f {i1} {i2} {i3}");
                }
            }

            var path = Path.Combine(Application.dataPath, filename);
            File.WriteAllText(path, sb.ToString());

            Debug.Log($"Mesh exported to: {path}");
        }
    }
}
