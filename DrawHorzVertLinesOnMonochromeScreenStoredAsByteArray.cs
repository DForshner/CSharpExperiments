using System;
using System.Diagnostics;

// Draw horizontal and vertical lines on at monochrome screen
// stored as an array of bytes where each byte represents 
// 8 pixels.  The screen width must be divisible by 8.

namespace DrawHorzVertLinesOnMonochromeScreenStoredAsByteArray
{
    /// <summary>
    /// Monochrome screen
    ///
    /// Screen coordinate system:
    /// +-----> +x
    /// | 
    /// |
    /// \/ +y
    /// </summary>
    public class Screen 
    {
        private byte[] _pixels;
        private readonly int _width;
        private readonly int _height;
        private readonly int _bytesPerRow;
        private const int PIX_PER_BYTE = 8;

        public Screen(int height, int width)
        {
            if (height <= 0 || height > Console.LargestWindowHeight) { throw new ArgumentOutOfRangeException(); }
            if (width <= 0 || width > Console.LargestWindowWidth) { throw new ArgumentOutOfRangeException(); }
            if (width % 8 != 0) { throw new Exception("The screen width must be divisible by 8."); }

            _width = width;
            _height = height;
            _bytesPerRow = width / PIX_PER_BYTE;
            _pixels = new byte[_bytesPerRow * height];
        }

        /// <summary>
        /// Rather then iterating over each pixel full bytes can be set to 0xFF
        /// and the residue start/end bits can be set using masks
        /// Should have O(pixels in line / 8) time complexity. 
        /// </summary>
        public void DrawHorizontalLine(int x1, int x2, int y)
        {
            if (x1 < 0 || x2 < 0 || x1 >= _width || x2 >= _width) { throw new ArgumentOutOfRangeException(); }
            if (y < 0 || y >= _height) { throw new ArgumentOutOfRangeException(); }
            if (x1 > x2) { throw new ArgumentException(); }

            var firstFullByte = x1 / PIX_PER_BYTE;
            var startOffset = x1 % PIX_PER_BYTE;
            if (startOffset != 0)
            {
                firstFullByte++;
            }

            var endOffset = x2 % PIX_PER_BYTE;
            var lastFullByte = x2 / PIX_PER_BYTE;
            if (endOffset != 7)
            {
                lastFullByte--;
            }
            
            // Set the full bytes worth of pixels. O(line length / 8)
            for (var rowIdx = firstFullByte; rowIdx <= lastFullByte; rowIdx++)
            {
                var screenIdx = (y * _bytesPerRow) + rowIdx;
                _pixels[screenIdx] = 0xFF;
            }

            var startMask = 0xFF >> startOffset;
            Debug.Print("0x{0:x}", (byte)startMask);

            var endMask = ~((0xFF) >> (endOffset + 1));
            Debug.Print("0x{0:x}", (byte)endMask);

            // Set the remaining start and end pixels
            if (x1 / PIX_PER_BYTE == x2 / PIX_PER_BYTE)
            {
                // x1 and x2 are in the same byte.
                var mask = startMask & endMask;
                Debug.Print("0x{0:x}", (byte)mask);

                var idx = (y * _bytesPerRow) + (x1 / PIX_PER_BYTE);
                var chunk = (int)_pixels[idx];
                _pixels[idx] = (byte)(chunk | mask);
            }
            else
            {
                if (startOffset != 0)
                {
                    // set the starting pixels
                    var startIdx = (y * _bytesPerRow) + firstFullByte - 1;
                    var startChunk = (int)_pixels[startIdx];
                    _pixels[startIdx] = (byte)(startChunk | startMask);
                }
                if (endOffset != 7)
                {
                    // set the ending pixels
                    var endIdx = (y * _bytesPerRow) + lastFullByte + 1;
                    var endChunk = (int)_pixels[endIdx];
                    _pixels[endIdx] = (byte)(endChunk | endMask);
                }
            }
        }

        /// <summary>
        /// Draws a vertical line.  
        /// This should run with O(pixel length of line) time complexity.
        /// </summary>
        public void DrawVerticalLine(int x, int y1, int y2)
        {
            if (x < 0 || x >= _width) { throw new ArgumentOutOfRangeException(); }
            if (y1 < 0 || y2 < 0 || y1 > _height || y2 >= _height) { throw new ArgumentOutOfRangeException(); } 
            if (y1 > y2) { throw new ArgumentException(); }

            var bitOffset = x % PIX_PER_BYTE;
            var byteOffset = x / PIX_PER_BYTE;

            var mask = 0x01 << (PIX_PER_BYTE - bitOffset - 1);
            Debug.Print("0x{0:x}", (byte)mask);

            var row = y1;
            while (row <= y2)
            {
                var curByte = (row * _bytesPerRow) + byteOffset;
                var chunk = (int)_pixels[curByte] | mask;
                _pixels[curByte] = (byte)chunk;
                row++;
            }
        }

        public void Render()
        {
            Console.Clear();

            var curRow = 0;
            var maxRow = _height - 1;
            while (curRow <= maxRow)
            {
                RenderRow(curRow);
                curRow++;
            }
        }

        /// <summary>
        /// Renders a row on the screen.
        /// Worst case would be row were every segment had a few set pixels which should have 
        /// O(pixels in row) time complexity.
        /// </summary>
        private void RenderRow(int row)
        {
            Console.CursorTop = row;
            var firstByteOfRow = _bytesPerRow * row;
            var lastByteOfRow = firstByteOfRow + _bytesPerRow - 1;
            var curByteIdx = firstByteOfRow;

            // Render each byte in the current row.
            while (curByteIdx <= lastByteOfRow)
            {
                var curByte = _pixels[curByteIdx];
                var xStartCoordForCurByte = (curByteIdx - firstByteOfRow) * PIX_PER_BYTE;
                curByteIdx++;

                // Skip if no pixels are set (common case).
                if (curByte == 0)
                {
                    continue; 
                }

                if (curByte == 0xFF)
                {
                    // All pixel are set.
                    Console.CursorLeft = xStartCoordForCurByte;
                    Console.Write("########");
                }
                else
                {
                    // Some pixels are set.
                    for (var i = 0; i <= 7; i++)
                    {
                        var mask = 0x1 << 7 - i;
                        var pixelSet = (curByte & mask) > 0;
                        if (pixelSet)
                        {
                            Console.CursorLeft = xStartCoordForCurByte + i;
                            Console.Write("#");
                        }
                    }
                }
            }
        }
    }

    // Display a test pattern
    //####   ##
    //#      ##
    //#      ##
    //#      ##
    //       ##
    //       ##
    //       ##
    //################
    //################
    //       ##
    //       ##
    //       ##
    //       ##      #
    //       ##      #
    //       ##      #
    //       ##   ####
    public static class Program
    {
        public static void Main()
        {
            var screen = new Screen(16, 16);
            screen.DrawHorizontalLine(0, 3, 0);
            screen.DrawVerticalLine(0, 0, 3);
            screen.DrawHorizontalLine(0, 15, 7);
            screen.DrawHorizontalLine(0, 15, 8);
            screen.DrawVerticalLine(7, 0, 15);
            screen.DrawVerticalLine(8, 0, 15);
            screen.DrawHorizontalLine(12, 15, 15);
            screen.DrawVerticalLine(15, 12, 15);
            screen.Render();

            Console.WriteLine("\n\nPress any key to exit.");
            Console.ReadKey(true);
        }
    }
}