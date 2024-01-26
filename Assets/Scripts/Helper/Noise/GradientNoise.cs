using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for all gradient noise patterns, meaning patterns that return a float value between 0 and 1 for each point in space.
/// <br/> GradientNoise patterns can either be over an infite space or within a finite and defined space.
/// </summary>
public abstract class GradientNoise : Noise
{
    public GradientNoise() : base() { }
    public GradientNoise(int seed) : base(seed) { }
    public abstract GradientNoise GetCopy();
    public abstract float GetValue(float x, float y);

    public override Sprite CreateTestSprite(int size = 128)
    {
        Texture2D texture = new Texture2D(size, size);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float value = GetValue(x, y);
                Color pixelColour = new Color(value, value, value);
                texture.SetPixel(x, y, pixelColour);
            }
        }
        texture.Apply();
        return sprite;
    }
}
