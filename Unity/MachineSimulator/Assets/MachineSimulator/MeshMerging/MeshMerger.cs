using UnityEngine;

namespace MeshMerging
{
    public sealed class MeshMerger : MonoBehaviour
    {
        [SerializeField] private MeshFilter[] _inputMeshFilters;

        public void Merge()
        {
            if (_inputMeshFilters == null || _inputMeshFilters.Length == 0)
            {
                return;
            }

            var combine = new CombineInstance[_inputMeshFilters.Length];

            for (var i = 0; i < _inputMeshFilters.Length; i++)
            {
                combine[i].mesh = _inputMeshFilters[i].sharedMesh;
                combine[i].transform = _inputMeshFilters[i].transform.localToWorldMatrix;
            }

            var mergedMesh = new Mesh();
            mergedMesh.CombineMeshes(combine, true, false);

            var go = new GameObject("MergedMesh");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mergedMesh;

            var meshRenderer = _inputMeshFilters[0].GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                var childRenderer = go.AddComponent<MeshRenderer>();
                childRenderer.sharedMaterial = meshRenderer.sharedMaterial;
            }

            MeshExporter.SaveToObj(mergedMesh, "MergedMesh.obj");
        }
    }
}