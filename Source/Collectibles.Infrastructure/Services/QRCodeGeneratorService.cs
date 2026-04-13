using Collectibles.Application.Services;
using QRCoder;

namespace Collectibles.Infrastructure.Services;

public class QRCodeGeneratorService : IQRCodeGeneratorService
{
    public byte[] GenerateQRCodeImage(string text, int pixelsPerModule = 20)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);

        return qrCode.GetGraphic(pixelsPerModule);
    }

    public string GenerateQRCodeSvg(string text, int pixelsPerModule = 10)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new SvgQRCode(qrCodeData);

        return qrCode.GetGraphic(pixelsPerModule);
    }

    public byte[] GeneratePrintableSheet(List<string> codes, int columns = 1, int rows = 1)
    {
        // For now, we'll generate a simple HTML-based printable sheet
        // that can be converted to PDF later if needed
        var html = GeneratePrintableHtml(codes, columns, rows);
        return System.Text.Encoding.UTF8.GetBytes(html);
    }

    private string GeneratePrintableHtml(List<string> codes, int columns, int rows)
    {
        var html = @"<!DOCTYPE html>
<html>
<head>
    <style>
        @page { 
            size: letter;
            margin: 0.5in;
        }
        body { 
            margin: 0; 
            padding: 0;
            font-family: Arial, sans-serif;
        }
        .qr-page {
            width: 100%;
            height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
            page-break-after: always;
        }
        .qr-page:last-child {
            page-break-after: auto;
        }
        .qr-code-wrapper {
            width: 2.4in;
            height: 2.4in;
            display: flex;
            justify-content: center;
            align-items: center;
        }
        .qr-code-wrapper svg {
            width: 100%;
            height: 100%;
        }
        @media print {
            .qr-page {
                height: auto;
                min-height: 100vh;
            }
            .qr-code-wrapper {
                width: 2.4in !important;
                height: 2.4in !important;
            }
        }
        @media screen {
            .qr-page {
                border-bottom: 1px dashed #ccc;
                margin-bottom: 20px;
                padding: 20px 0;
                height: auto;
                min-height: 400px;
            }
        }
    </style>
</head>
<body>";

        // Generate one QR code per page
        for (int i = 0; i < codes.Count; i++)
        {
            var url = codes[i];
            var svgData = GenerateQRCodeSvg(url, 10);

            html += $@"
    <div class='qr-page'>
        <div class='qr-code-wrapper'>
            {svgData}
        </div>
    </div>";
        }

        html += @"
</body>
</html>";

        return html;
    }
}
