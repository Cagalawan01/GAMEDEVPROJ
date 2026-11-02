using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Save-slot thumbnail helper for the visual novel.
/// Grabs the rendered game frame right before the in-game settings menu opens,
/// so the save card shows the exact scene the player was on (without menus on top).
/// Thumbnails are stored as PNG files under persistentDataPath/SaveThumbs.
/// </summary>
public static class SaveThumbnail
{
    private const int ThumbWidth = 640;
    private const int ThumbHeight = 360;

    private static Texture2D pending;
    private static ThumbRunner runner;

    private class ThumbRunner : MonoBehaviour { }

    private static ThumbRunner Runner
    {
        get
        {
            if (runner == null)
            {
                GameObject go = new GameObject("[SaveThumbnail]");
                go.hideFlags = HideFlags.HideInHierarchy;
                UnityEngine.Object.DontDestroyOnLoad(go);
                runner = go.AddComponent<ThumbRunner>();
            }
            return runner;
        }
    }

    private static string Folder => Path.Combine(Application.persistentDataPath, "SaveThumbs");

    public static string PathForSlot(int globalSlot) => Path.Combine(Folder, $"slot_{globalSlot:D2}.png");

    /// <summary>Grabs the current game frame at end of frame, then runs onDone (e.g. open the menu).</summary>
    public static void GrabFrameThen(Action onDone)
    {
        Runner.StartCoroutine(GrabRoutine(onDone));
    }

    private static IEnumerator GrabRoutine(Action onDone)
    {
        yield return new WaitForEndOfFrame();

        Texture2D full = null;
        try
        {
            full = ScreenCapture.CaptureScreenshotAsTexture();
            if (full != null)
            {
                ClearPending();
                pending = Downscale(full, ThumbWidth, ThumbHeight);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveThumbnail] Could not grab frame: {e.Message}");
        }
        finally
        {
            if (full != null) UnityEngine.Object.Destroy(full);
        }

        onDone?.Invoke();
    }

    /// <summary>Writes the most recently grabbed frame to the slot's PNG file.</summary>
    public static bool WritePendingToSlot(int globalSlot)
    {
        if (pending == null) return false;
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(PathForSlot(globalSlot), pending.EncodeToPNG());
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveThumbnail] Could not write thumbnail for slot {globalSlot}: {e.Message}");
            return false;
        }
    }

    /// <summary>Loads the slot thumbnail, or null if none exists.</summary>
    public static Texture2D LoadForSlot(int globalSlot)
    {
        string path = PathForSlot(globalSlot);
        if (!File.Exists(path)) return null;
        try
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (tex.LoadImage(File.ReadAllBytes(path)))
            {
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                return tex;
            }
            UnityEngine.Object.Destroy(tex);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveThumbnail] Could not load thumbnail for slot {globalSlot}: {e.Message}");
        }
        return null;
    }

    public static void DeleteForSlot(int globalSlot)
    {
        string path = PathForSlot(globalSlot);
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveThumbnail] Could not delete thumbnail for slot {globalSlot}: {e.Message}");
        }
    }

    private static void ClearPending()
    {
        if (pending != null)
        {
            UnityEngine.Object.Destroy(pending);
            pending = null;
        }
    }

    private static Texture2D Downscale(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        RenderTexture prev = RenderTexture.active;
        Graphics.Blit(source, rt);
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }
}
