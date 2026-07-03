using HtmlRendererCore.PdfSharp;

using PdfSharp;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Snippets.Font;

//GlobalFontSettings.FontResolver = new FailsafeFontResolver();
Dictionary<string, string> customFonts = new Dictionary<string, string>()
{
    { "Deng.ttf", "C:\\Deng.ttf" },
    { "Dengb.ttf", "C:\\Dengb.ttf" }
};
PdfGenerator.Initialize(customFonts);
// for segoe UI is not support chinese, so we need to register a custom font for chinese text rendering.
// and add a font mapping for segoe UI to use the custom font instead.
PdfGenerator.AddFontFamilyMapping("Segoe UI", "Deng");

var html = @"
                <html >
                <head>
                <meta charset='utf-8'>
                <style>
               body, div, p {
                font-family: 'Segoe UI','Deng', sans-serif;
                font-size: 50px;
                line-height: 10px;
                }
                </style>
                </head>
                    <body style=""font-size: 50px; line-height: 10px"">
                        <div>test123</div>
                        <p>test123-中华人民共和国</p>
                        <div style=""position: absolute; margin-top: -40px;"">tTest12</div>
                        <img src=""data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAUAAAAFCAYAAACNbyblAAAAHElEQVQI12P4
  //8/w38GIAXDIBKE0DHxgljNBAAO9TXL0Y4OHwAAAABJRU5ErkJggg=="" alt=""Red dot"" />
                    </body>
                </html>
            ";

var document = new PdfDocument();
document.Options.ColorMode = PdfColorMode.Cmyk;

//PdfGenerator.AddFontFamilyMapping("test123", "test123");
//PdfGenerator.AddFontFamily("test123");
PdfGenerator.AddPdfPages(document, html, PageSize.A4, 0);

document.Save("file.pdf");


// var generator = new PayloadGenerator.ContactData(PayloadGenerator.ContactData.ContactOutputType.VCard3, "John", "Doe");
// string payload = generator.ToString();

// QRCodeGenerator qrGenerator = new QRCodeGenerator();
// QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
// var qrCode = new Base64QRCode(qrCodeData);
// var qrCodeImage = qrCode.GetGraphic(20, Color.Black, Color.White, true, Base64QRCode.ImageType.Jpeg);

// var image = Image.Load(qrCodeImage);
// image.Save("test2.bmp");
// Bitmap bmp;
// using (var ms = new MemoryStream(imageData))
// {
//     bmp = new Bitmap(ms);
// }

// File.WriteAllBytes("test.bmp", qrCodeImage);
// await qrCodeImage.SaveAsJpegAsync("qrcode_cmyk.jpeg", new JpegEncoder() { ColorType = JpegColorType.Cmyk, Quality = 100 });


// await castedQrCodeImage.SaveAsJpegAsync("qrcode_cmyk2.jpeg", new JpegEncoder() { ColorType = JpegColorType.Cmyk, Quality = 100 });

// var document = new PdfDocument();
// document.Options.ColorMode = PdfColorMode.Cmyk;
//
// PdfGenerator.AddPdfPages(document, html, PageSize.A4, 0);
//
// document.Save("file.pdf");




//
// var castedQrCodeImage = (Image<Rgba32>)qrCodeImage;
//
// var convert = new ColorSpaceConverter();
// castedQrCodeImage.ProcessPixelRows(accessor =>
// {
//     // Color is pixel-agnostic, but it's implicitly convertible to the Rgba32 pixel type
//     Rgba32 transparent = Color.Blue;
//
//     for (int y = 0; y < accessor.Height; y++)
//     {
//         Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
//         Span<Cmyk> pixelRowCmyk = new Span<Cmyk>();
//
//         foreach (ref Rgba32 pixel in pixelRow)
//         {
//             var cymk = convert.ToCmyk(pixel);
//             pixel = transparent;
//         }
//     }
// });
//
// var sourceImage = castedQrCodeImage;
//
// Image<Rgba32> targetImage = new(sourceImage.Width, sourceImage.Height);
// int height = sourceImage.Height;
// sourceImage.ProcessPixelRows(targetImage, (sourceAccessor, targetAccessor) =>
// {
//     for (int i = 0; i < height; i++)
//     {
//         Span<Rgba32> sourceRow = sourceAccessor.GetRowSpan(i);
//         Span<Rgba32> targetRow = targetAccessor.GetRowSpan(i);
//
//         sourceRow.Slice(sourceArea.X, sourceArea.Width).CopyTo(targetRow);
//     }
// }