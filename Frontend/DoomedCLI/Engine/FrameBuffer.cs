using System.Text;

namespace DoomedCLI;

class FrameBuffer
{
    private readonly char[] _front;
    private readonly char[] _back;
    public readonly int Width;
    public readonly int Height;
    private const int CleanGapThreshold = 8; // spans shorter than this are absorbed to avoid excessive cursor repositioning

    private readonly StringBuilder _outputBuffer = new(4096); // initial size is a guess; will grow if needed but hopefully not too often

    public FrameBuffer(int width, int height)
    {
        Width  = width;
        Height = height;
        _front = new char[width * height];
        _back  = new char[width * height];
        Array.Fill(_front, ' ');
        Array.Fill(_back,  ' ');
    }

    public void Clear()
    {
        Array.Fill(_back, ' ');
    }

    public void Set(int x, int y, char c)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;
        _back[y * Width + x] = c;
    }

    
    public void WriteString(int x, int y, string s)
    {
        if (y < 0 || y >= Height || x >= Width) return;
        int rowBase = y * Width;
        int writeLength = Math.Min(s.Length, Width - x);
        for (int i = 0; i < writeLength; i++)
            _back[rowBase + x + i] = s[i];
    }

    // Avoids Substring allocation — writes a slice of s directly into the back buffer.
    public void WriteString(int x, int y, string s, int start, int count)
    {
        if (y < 0 || y >= Height || x >= Width) return;
        int rowBase = y * Width;
        int writeLength = Math.Min(count, Width - x);
        for (int i = 0; i < writeLength; i++)
            _back[rowBase + x + i] = s[start + i];
    }
    
    public void WriteRow(int screenX, int screenY, char[] src, int srcOffset, int srcLen)
    {
        if (screenY < 0 || screenY >= Height || screenX >= Width) return;
        int rowStartIndex = screenY * Width;
        int columnsToCopy  = Math.Min(srcLen, Width - screenX);
        if (columnsToCopy <= 0) return;
        Array.Copy(src, srcOffset, _back, rowStartIndex + screenX, columnsToCopy);
    }

    public void Flush()
    {
        _outputBuffer.Clear();

        for (int y = 0; y < Height; y++)
        {
            int rowBase = y * Width;
            int x = 0;

            while (x < Width)
            {
                // Skip clean cells
                while (x < Width && _back[rowBase + x] == _front[rowBase + x])
                    x++;

                if (x >= Width) break;

                // Start of a dirty span
                int spanStart = x;
                int spanEnd   = x;

                while (spanEnd < Width)
                {
                    if (_back[rowBase + spanEnd] != _front[rowBase + spanEnd])
                    {
                        spanEnd++;
                    }
                    else
                    {
                        // Measure the clean run ahead
                        int gapStart = spanEnd;
                        while (spanEnd < Width && _back[rowBase + spanEnd] == _front[rowBase + spanEnd])
                            spanEnd++;

                        if (spanEnd - gapStart >= CleanGapThreshold)
                        {
                            spanEnd = gapStart; // end span before the gap
                            break;
                        }
                        // Small gap — absorb into span (avoid extra cursor move)
                    }
                }

                int spanLength = spanEnd - spanStart;

                // ANSI cursor position: ESC[row;colH (both 1-indexed)
                _outputBuffer.Append("\x1b[");
                _outputBuffer.Append(y + 1);
                _outputBuffer.Append(';');
                _outputBuffer.Append(spanStart + 1);
                _outputBuffer.Append('H');
                _outputBuffer.Append(new ReadOnlySpan<char>(_back, rowBase + spanStart, spanLength));

                // Sync front buffer for this span
                Array.Copy(_back, rowBase + spanStart, _front, rowBase + spanStart, spanLength);

                x = spanEnd;
            }
        }

        if (_outputBuffer.Length > 0)
            Console.Out.Write(_outputBuffer); // might bite us in the ass later, if you're changing this, now is later XD
    }
}


