
namespace CleanerChallenge
{
    public class State
    {
        public const int Width = 1280;
        public const int Height = 800;

        public Timer timer { get; set; }
        public int score { get; set; }
        public string dust { get; set; } = "";
        public Point robot { get; set; }
        public Point[] socks { get; set; } = Array.Empty<Point>();

        /// <summary>
        /// Decode as an array of 1280 * 800 bytes. One byte per each pixel of the room.
        /// 0xFF value indicates a fully clean pixel.
        /// Zero value indicates a wall.
        /// </summary>
        public byte[] Decode() => Convert.FromBase64String(dust);

        /// <summary>
        /// Decode as a 2D array. One byte per each pixel of the room.
        /// 0xFF value indicates a fully clean pixel.
        /// Zero value indicates a wall.
        /// </summary>
        public byte[,] Decode2()
        {
            var array = Decode();
            var result = new byte[Width, Height];
            for (int y = 0, i = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++, i++)
                {
                    result[x, y] = array[i];
                }
            }
            return result;
        }
    }

    public struct Command
    {
        public int x { get; set; }
        public int y { get; set; }

        /// <summary>Skip animation</summary>
        public bool fast { get; set; }
    }

    public struct Point
    {
        public int x { get; set; }
        public int y { get; set; }

        public override string ToString()
        {
            return $"({x}, {y})";
        }
    }

    public struct Timer
    {
        public double timeout { get; set; }
        public double dt { get; set; }

        public override string ToString()
        {
            return $"{timeout} / {dt}";
        }
    }

    public struct GameOver
    {
        public int score { get; set; }
    }
}
