using UnityEngine;

/// <summary>A box-player colour variant. The look is one material; the preview is a plain cube wearing it.</summary>
[CreateAssetMenu(fileName = "Character", menuName = "Game/Character Definition")]
public class CharacterDefinition : UnlockableDefinition
{
    private const float PreviewSize = 1.5f;

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
        CompanionFitter.OnPedestal(previewObject, PreviewSize, parent.position + Vector3.up * PreviewFloor);
        return previewObject;
    }
}
