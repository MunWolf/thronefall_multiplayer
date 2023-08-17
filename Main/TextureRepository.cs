using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace ThronefallMP;

public class TextureRepository
{
    public readonly Texture2D Crown = LoadTexture("crown.png");
    public readonly Texture2D Lock = LoadTexture("lock-icon.png");
    public readonly Texture2D Blank = LoadTexture("blank.png");

    private static Texture2D LoadTexture(string textureName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resource = assembly.GetManifestResourceNames()
            .Single(str => str.EndsWith(textureName));
        var stream = assembly.GetManifestResourceStream(resource);
        var texture = new Texture2D(2, 2, GraphicsFormat.R8G8B8A8_UNorm, 1, TextureCreationFlags.None);
        using var memoryStream = new MemoryStream();
        Debug.Assert(stream != null, nameof(stream) + " != null");
        stream.CopyTo(memoryStream);
        texture.LoadImage(memoryStream.ToArray());
        return texture;
    }
}