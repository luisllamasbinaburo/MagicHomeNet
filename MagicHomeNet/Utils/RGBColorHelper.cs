using System;
using System.Drawing;

namespace Utils
{
    /// <summary>
    /// Helper for colors
    /// </summary>
    public static class RGBColorHelper
    {
        #region Internal Methods

        /// <summary>
        /// Compute a RGB color
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        internal static int ComputeRGBColor(int r, int g, int b)
        {
            return r << 16 | g << 8 | b;
        }

        internal static Color ParseColor(int computedColor)
        {
            int r = (byte)(computedColor >> 16); // = 0
            int g = (byte)(computedColor >> 8); // = 0
            int b = (byte)(computedColor >> 0); // = 255
            return Color.FromArgb(r, g, b);
        }

        /// <summary> Creates a new color object from hexadecimal values. (ex. #0000ff) </summary>
        internal static Color ParseHexColor(string hexColor)
        {
            byte[] bytes = ToByteArray(hexColor);
            int r= bytes[0];
            int g= bytes[1];
            int b = bytes[2];
            return Color.FromArgb(r, g, b);
        }

        /// <summary> Converts a string containing hexadecimals to a byte array. </summary>
        internal static byte[] ToByteArray(string hexString)
        {
            byte[] bytes = new byte[hexString.Length / 2];
            int indexer;
            if (hexString[0] == '#')
                indexer = 1;
            else
                indexer = 0;

            for (int i = indexer; i < hexString.Length; i += 2)
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            return bytes;
        }

        #endregion Internal Methods
    }
}