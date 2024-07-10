using Godot;
using System;

public partial class MapPreprocessor : Node
{
    [Export]
    public TerrainGenerator terrain;
    [Export]
    public int Size = 256;
    [Export]
    public Image map;
    public Color[,] mapData;



    public void UpdateMap(ref MapPoint[,] mapPoints)
    {
        map = Image.Create(Size, Size, false, Image.Format.Rgbaf);

        DoUpdate(ref mapPoints);
    }

    protected virtual void DoUpdate(ref MapPoint[,] mapPoints)
    {
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                map.SetPixel(x, y, Colors.Black);
            }
        }
    }

    protected void UpdateImage(int x, int y, float ratio, Color color)
    {
        map.SetPixel(Mathf.RoundToInt(x * ratio), Mathf.RoundToInt(y * ratio), color);
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
