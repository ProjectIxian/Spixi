using System;
using System.Collections.Generic;
using System.Text;
using ZXing.Net;
using ZXing.Net.Mobile.Forms;
using ZXing;
using ZXing.Rendering;
using ZXing.Common;
using Xamarin.Forms;

namespace SPIXI
{
    class BarcodeGenerator
    {
        /*
        public static T Generate<T>(IBarcodeRenderer<T> renderer, string content, EncodingOptions options,
                                        BarcodeFormat format = BarcodeFormat.QR_CODE) =>
        new ZXing.BarcodeWriterGeneric<T> { Format = format, Options = options, Renderer = renderer }.Write(content);

        public class StringRenderer : IBarcodeRenderer<string>
        {

            public string Block { get; set; }
            public string Empty { get; set; }
            public string NewLine { get; set; }

            public string Render(BitMatrix matrix, BarcodeFormat format, string content) => Render(matrix, format, content, null);

            public string Render(BitMatrix matrix, BarcodeFormat format, string content, EncodingOptions options)
            {
                StringBuilder SB = new StringBuilder();
                for (int Y = 0; Y < matrix.Height; Y++)
                {
                    if (Y > 0) SB.Append(NewLine);
                    for (int X = 0; X < matrix.Width; X++) SB.Append(matrix[X, Y] ? Block : Empty);
                }
                return SB.ToString();
            }
        }

        public class ImageRenderer : IBarcodeRenderer<Image>
        {

            public Color Background { get; set; } = Color.White;
            public Color Foreground { get; set; } = Color.Black;

            public Image Render(BitMatrix matrix, BarcodeFormat format, string content) => Render(matrix, format, content, null);

            public Image Render(BitMatrix matrix, BarcodeFormat format, string content, EncodingOptions options)
            {
                Image Image = new Image(matrix.Width, matrix.Height);
                using (IPixelAccessor<Color, uint> Lock = Image.Lock())
                {
                    for (int Y = 0; Y < matrix.Height; Y++)
                    {
                        for (int X = 0; X < matrix.Width; X++) Lock[X, Y] = matrix[X, Y] ? Foreground : Background;
                    }
                }
                return Image;
            }
        }
        */

    }
}
