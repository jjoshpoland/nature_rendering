using Godot;
using System;
using System.Diagnostics;

[Tool]
public partial class NoiseMap : Node
{
    [Export]
    int Size = 256;
    [Export]
    bool needsUpdate = false;
    [Export]
    Image map;
    [Export]
	FastNoiseLite noise;


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
        map = Image.Create(size, size, false, Image.Format.Rf);
        

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float noiseValue = noise.GetNoise2D(x, y);
                map.SetPixel(x, y, new Color(noiseValue, noiseValue, noiseValue));
            }
        }
    }


    public float SampleMap(float x, float y, int worldSize)
    {
        int srcWidth = map.GetWidth();
        int srcHeight = map.GetHeight();

        float gx = (x / worldSize) * (srcWidth - 1);
        float gy = (y / worldSize) * (srcHeight - 1);

        int gxi = (int)gx;
        int gyi = (int)gy;

        float height = 0.0f;

        for (int m = -1; m < 3; m++)
        {
            for (int n = -1; n < 3; n++)
            {
                int p = Mathf.Clamp(gxi + m, 0, srcWidth - 1);
                int q = Mathf.Clamp(gyi + n, 0, srcHeight - 1);

                float c = map.GetPixel(p, q).R;

                float wx = BicubicKernel(gx - (gxi + m));
                float wy = BicubicKernel(gy - (gyi + n));

                height += c * wx * wy;
            }
        }

        return height;
    }

    private float BicubicKernel(float x)
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
