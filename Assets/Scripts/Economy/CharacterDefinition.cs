using UnityEngine;

/// <summary>A box-player colour variant. The look is one material; the preview is a plain cube wearing it.</summary>
[CreateAssetMenu(fileName = "Character", menuName = "Game/Character Definition")]
public class CharacterDefinition : UnlockableDefinition
{
    [SerializeField] private Material material;
    [SerializeField] private Mesh previewMesh;

    public Material Material => material;

    public override Color TileColor => material != null ? material.color : Color.white;

    public override GameObject CreatePreview(Transform parent)
    {
        var previewObject = new GameObject("Preview_" + Id, typeof(MeshFilter), typeof(MeshRenderer));
        previewObject.GetComponent<MeshFilter>().sharedMesh = previewMesh;
        previewObject.GetComponent<MeshRenderer>().sharedMaterial = material;
        previewObject.transform.SetParent(parent, false);
        return previewObject;
    }
}
