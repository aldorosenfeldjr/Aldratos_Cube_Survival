using UnityEngine;

/// <summary>A box-player colour variant. The look is one material; the preview is a plain cube wearing it.</summary>
[CreateAssetMenu(fileName = "Character", menuName = "Game/Character Definition")]
public class CharacterDefinition : UnlockableDefinition
{
    [SerializeField] private Material material;

    public Material Material => material;

    public override Color TileColor => material != null ? material.color : Color.white;

    public override GameObject CreatePreview(Transform parent)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Preview_" + Id;
        Destroy(cube.GetComponent<Collider>());
        cube.GetComponent<Renderer>().sharedMaterial = material;
        cube.transform.SetParent(parent, false);
        return cube;
    }
}
