namespace DoomedCLI;

readonly struct Hitbox
{
    public readonly int X;
    public readonly int Y;
    public readonly int Width;
    public readonly int Height;

    public Hitbox(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public bool Contains(int worldX, int worldY)
        => worldX >= X && worldX < X + Width
        && worldY >= Y && worldY < Y + Height;


    public bool Overlaps(Hitbox other)
        => X < other.X + other.Width && X + Width > other.X
        && Y < other.Y + other.Height && Y + Height > other.Y;
}
