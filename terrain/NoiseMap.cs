using Godot;
using System;
using System.Diagnostics;

[Tool]
public partial class NoiseMap : Node
{
    [Export]
    public int Size = 256;
    [Export]
    bool needsUpdate = false;
    [Export]
    float detailNoiseScale = 0.01f;
    [Export]
    public Image map;
    [Export]
	FastNoiseLite rNoise;
    [Export]
    FastNoiseLite gNoise;
    [Export]
    FastNoiseLite bNoise;
    [Export]
    FastNoiseLite detailNoise;
    [Export]
    Curve rNoiseCurve;
    [Export]
    Curve gNoiseCurve;
    [Export]
    Curve bNoiseCurve;

    public Color[,] mapData;

    public override void _Process(double delta)
    {
        if(needsUpdate)
        {
            Init(Size);
            needsUpdate = false;
        }
    }


    public void Init(int size)
	{
        map = Image.Create(size, size, false, Image.Format.Rgbaf);
        mapData = new Color[size,size];

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float gValue = 0;
                float bValue = 0;
                float rValue = rNoiseCurve.Sample( (rNoise.GetNoise2D(x, y) + 1f) / 2f);
                if (gNoise != null)
                {
                    gValue = gNoiseCurve.Sample((gNoise.GetNoise2D(x, y) + 1f) / 2f);
                }
                if (bNoise != null)
                {
                    bValue = bNoiseCurve.Sample((bNoise.GetNoise2D(x, y) + 1f) / 2f);
                }
                 
                 
                map.SetPixel(x, y, new Color(rValue, gValue, bValue));
                mapData[x, y] = new Color(rValue, gValue, bValue);
            }
        }
    }


    public Color SampleMap(float x, float y, int worldSize)
    {
        int srcWidth = mapData.GetLength(0);
        int srcHeight = mapData.GetLength(1);

        float gx = (x / worldSize) * (srcWidth - 1);
        float gy = (y / worldSize) * (srcHeight - 1);

        int gxi = (int)gx;
        int gyi = (int)gy;

        float r = 0.0f;
        float g = 0.0f;
        float b = 0.0f;

        for (int m = -1; m < 3; m++)
        {
            for (int n = -1; n < 3; n++)
            {
                int p = Mathf.Clamp(gxi + m, 0, srcWidth - 1);
                int q = Mathf.Clamp(gyi + n, 0, srcHeight - 1);

                float rp = mapData[p, q].R;
                float gp = mapData[p, q].G;
                float bp = mapData[p, q].B;

                float wx = BicubicKernel(gx - (gxi + m));
                float wy = BicubicKernel(gy - (gyi + n));

                r += rp * wx * wy;
                g += gp * wx * wy;
                b += bp * wx * wy;
            }
        }

        if (detailNoise != null)
        {
            r += detailNoise.GetNoise2D(x, y) * detailNoiseScale;
        }

        return new Color(r,g,b);
    }

    public static Color SampleImage(Image image, float x, float y, int worldSize)
    {
        int srcWidth = image.GetWidth();
        int srcHeight = image.GetHeight();

        float gx = (x / worldSize) * (srcWidth - 1);
        float gy = (y / worldSize) * (srcHeight - 1);

        int gxi = (int)gx;
        int gyi = (int)gy;

        float r = 0.0f;
        float g = 0.0f;
        float b = 0.0f;

        for (int m = -1; m < 3; m++)
        {
            for (int n = -1; n < 3; n++)
            {
                int p = Mathf.Clamp(gxi + m, 0, srcWidth - 1);
                int q = Mathf.Clamp(gyi + n, 0, srcHeight - 1);

                float rp = image.GetPixel(p, q).R;
                float gp = image.GetPixel(p, q).G;
                float bp = image.GetPixel(p, q).B;

                float wx = BicubicKernel(gx - (gxi + m));
                float wy = BicubicKernel(gy - (gyi + n));

                r += rp * wx * wy;
                g += gp * wx * wy;
                b += bp * wx * wy;
            }
        }

        return new Color(r, g, b);
    }

    public static float BicubicKernel(float x)
    {
        float a = -0.5f;
        x = Mathf.Abs(x);
        if (x <= 1)
        {
            return (a + 2) * Mathf.Pow(x, 3) - (a + 3) * Mathf.Pow(x, 2) + 1;
        }
        else if (x < 2)
        {
            return a * Mathf.Pow(x, 3) - 5 * a * Mathf.Pow(x, 2) + 8 * a * x - 4 * a;
        }
        else
        {
            return 0;
        }
    }
}
