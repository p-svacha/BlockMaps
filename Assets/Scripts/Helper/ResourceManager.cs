using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// Class used to dynamically load materials from resources on demand. All materials are cached after the first load.
/// </summary>
public static class ResourceManager
{
    private static Dictionary<string, Material> CachedMaterials = new Dictionary<string, Material>();
    public static Material LoadMaterial(string resourcePath)
    {
        // cached
        if (CachedMaterials.TryGetValue(resourcePath, out Material mat)) return mat;

        // not yet cached
        Material newMat = Resources.Load<Material>(resourcePath);
        if (newMat == null) throw new System.Exception($"Failed to load material {resourcePath}.");
        CachedMaterials.Add(resourcePath, newMat);
        return newMat;
    }

    private static Dictionary<string, Texture2D> CachedTextures = new Dictionary<string, Texture2D>();
    public static Texture2D LoadTexture(string resourcePath)
    {
        // cached
        if (CachedTextures.TryGetValue(resourcePath, out Texture2D tex)) return tex;

        // not yet cached
        Texture2D newTex = Resources.Load<Texture2D>(resourcePath);
        if (newTex == null) throw new System.Exception($"Failed to load texture {resourcePath}.");
        CachedTextures.Add(resourcePath, newTex);
        return newTex;
    }

    private static Dictionary<string, GameObject> CachedPrefabs = new Dictionary<string, GameObject>();
    public static GameObject LoadPrefab(string resourcePath)
    {
        // cached
        if (CachedPrefabs.TryGetValue(resourcePath, out GameObject obj)) return obj;

        // not yet cached
        GameObject loadedPrefab = Resources.Load<GameObject>(resourcePath);
        if (loadedPrefab == null) throw new System.Exception($"Failed to load GameObject {resourcePath}.");
        CachedPrefabs.Add(resourcePath, loadedPrefab);
        return loadedPrefab;
    }

    private static Dictionary<string, Sprite> CachedSprites = new Dictionary<string, Sprite>();
    public static Sprite LoadSprite(string resourcePath)
    {
        // cached
        if (CachedSprites.TryGetValue(resourcePath, out Sprite obj)) return obj;

        // not yet cached
        Sprite loadedSprite = Resources.Load<Sprite>(resourcePath);
        if (loadedSprite == null) throw new System.Exception($"Failed to load Sprite {resourcePath}.");
        CachedSprites.Add(resourcePath, loadedSprite);
        return loadedSprite;
    }

    private static Dictionary<string, AudioClip> CachedAudioClips = new Dictionary<string, AudioClip>();
    public static AudioClip LoadAudioClip(string resourcePath)
    {
        // cached
        if (CachedAudioClips.TryGetValue(resourcePath, out AudioClip obj)) return obj;

        // not yet cached
        AudioClip loadedAudioClip = Resources.Load<AudioClip>(resourcePath);
        if (loadedAudioClip == null) throw new System.Exception($"Failed to load AudioClip {resourcePath}.");
        CachedAudioClips.Add(resourcePath, loadedAudioClip);
        return loadedAudioClip;
    }

    public static void ClearCache()
    {
        CachedMaterials.Clear();
        CachedTextures.Clear();
        CachedPrefabs.Clear();
        CachedSprites.Clear();
        CachedAudioClips.Clear();
    }

    // Specific helpers
    private static string TileOverlaysPath = "Textures/NodeOverlays/";
    public static Texture2D GetTileSelector(Direction dir)
    {
        if (dir == Direction.None) return LoadTexture(TileOverlaysPath + "TileSelection");
        if (dir == Direction.N) return LoadTexture(TileOverlaysPath + "TileSelectionN");
        if (dir == Direction.E) return LoadTexture(TileOverlaysPath + "TileSelectionE");
        if (dir == Direction.S) return LoadTexture(TileOverlaysPath + "TileSelectionS");
        if (dir == Direction.W) return LoadTexture(TileOverlaysPath + "TileSelectionW");
        if (dir == Direction.NE) return LoadTexture(TileOverlaysPath + "TileSelectionNE");
        if (dir == Direction.NW) return LoadTexture(TileOverlaysPath + "TileSelectionNW");
        if (dir == Direction.SW) return LoadTexture(TileOverlaysPath + "TileSelectionSW");
        if (dir == Direction.SE) return LoadTexture(TileOverlaysPath + "TileSelectionSE");
        return null;
    }
    public static Texture2D FullTileSelector => GetTileSelector(Direction.None);
}