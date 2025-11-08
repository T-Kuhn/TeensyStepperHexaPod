using UnityEngine;

namespace MeshMerging
{
    public sealed class MeshMerger : MonoBehaviour
    {
        [SerializeField] private MeshFilter[] _inputMeshFilters;

        // NOTE: goes from 0 to 1 (0 = no simplification, 1 = full simplification)
        [SerializeField, Range(0f, 1f)] private float _simplificationQuality = 0.5f;

        public void Merge()
        {
            if (_inputMeshFilters == null || _inputMeshFilters.Length == 0)
            {
                return;
            }

            var combine = new CombineInstance[_inputMeshFilters.Length];

            var commonScale = _inputMeshFilters[0].transform.localScale;
            var hasCommonScale = true;

            for (var i = 0; i < _inputMeshFilters.Length; i++)
            {
                combine[i].mesh = _inputMeshFilters[i].sharedMesh;
                combine[i].transform = _inputMeshFilters[i].transform.localToWorldMatrix;

                if (_inputMeshFilters[i].transform.localScale != commonScale)
                {
                    hasCommonScale = false;
                }
            }

            var mergedMesh = new Mesh();
            mergedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mergedMesh.CombineMeshes(combine, true, false);

            // Simplify the mesh
            var simplifiedMesh = MeshSimplifier.Simplify(mergedMesh, _simplificationQuality);

            var hash = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            var filename = $"MergedMesh_{hash}.obj";
            MeshExporter.SaveToObj(simplifiedMesh, filename);

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            var relativePath = $"Assets/MachineSimulator/ModelData/SimplifiedMeshes/{filename}";
            var loadedMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(relativePath);

            var go = new GameObject("MergedMesh");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = hasCommonScale ? commonScale : Vector3.one;

            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = loadedMesh;

            var meshRenderer = _inputMeshFilters[0].GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                var childRenderer = go.AddComponent<MeshRenderer>();
                childRenderer.sharedMaterial = meshRenderer.sharedMaterial;
            }
#endif
        }
    }
}