namespace Collectibles.Application.Services;

public interface IHashIdsService
{
    string Encode(long id);
    long Decode(string hash);
    string Encode(params long[] ids);
    long[] DecodeMultiple(string hash);
}
