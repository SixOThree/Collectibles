namespace Collectibles.Application.Services;

public interface IQRCodeGeneratorService
{
    byte[] GenerateQRCodeImage(string text, int pixelsPerModule = 20);
    string GenerateQRCodeSvg(string text, int pixelsPerModule = 10);
    byte[] GeneratePrintableSheet(List<string> codes, int columns = 3, int rows = 4);
}
