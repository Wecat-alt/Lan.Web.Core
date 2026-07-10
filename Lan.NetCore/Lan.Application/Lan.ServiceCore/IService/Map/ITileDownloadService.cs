using Lan.Dto;

namespace Lan.ServiceCore.IService
{
    public interface ITileDownloadService
    {
        Task DownloadTilesAsync(TileDownloadRequest request);
    }
}
