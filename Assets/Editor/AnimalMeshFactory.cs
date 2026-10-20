using UnityEditor;
using UnityEngine;

/// <summary>Esculpe vaca, boi, ubere e patins e salva as malhas em Assets/Meshes.</summary>
public static class AnimalMeshFactory
{
    private const string Folder = "Assets/Meshes";

    public static Mesh Cow()
    {
        return Save(SdfMesher.Build("Cow", AnimalShapes.CowShapes(), AnimalShapes.CowPaints(),
            new Vector3(-1.0f, -0.75f, -1.15f), new Vector3(1.0f, 1.35f, 1.85f), 0.032f));
    }

    public static Mesh Bull()
    {
        return Save(SdfMesher.Build("Bull", AnimalShapes.BullShapes(), AnimalShapes.BullPaints(),
            new Vector3(-1.1f, -0.75f, -1.2f), new Vector3(1.1f, 1.4f, 2.0f), 0.032f));
    }

    public static Mesh Udder()
    {
        return Save(SdfMesher.Build("Udder", AnimalShapes.UdderShapes(), AnimalShapes.UdderPaints(),
            new Vector3(-0.45f, -0.45f, -0.45f), new Vector3(0.45f, 0.3f, 0.45f), 0.014f));
    }

    public static Mesh Skate(bool blue)
    {
        return Save(SdfMesher.Build(blue ? "SkateBlue" : "SkatePink",
            blue ? AnimalShapes.SkateBlueShapes() : AnimalShapes.SkatePinkShapes(),
            blue ? AnimalShapes.SkateBluePaints() : AnimalShapes.SkatePinkPaints(),
            new Vector3(-0.3f, -0.25f, -0.42f), new Vector3(0.3f, 0.42f, 0.42f), 0.014f));
    }

    private static Mesh Save(Mesh mesh)
    {
        System.IO.Directory.CreateDirectory(Folder);
        string path = Folder + "/" + mesh.name + ".asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        return AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }
}
