using Lan.Dto;
using Lan.ServiceCore.IService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SysTasks = System.Threading.Tasks.Task;

namespace Lan.Application.Controllers.Map
{
    [Route("api/tiledownload")]
    [ApiController]
    public class TileDownloadController : BaseController
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public TileDownloadController(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// 启动服务器端瓦片下载任务（异步执行，通过 SignalR 推送进度）
        /// </summary>
        [HttpPost("start")]
        public IActionResult StartDownload([FromBody] TileDownloadRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TileUrl) || string.IsNullOrWhiteSpace(request.TargetFolder))
            {
                return ToResponse(ResultCode.FAIL, "参数不完整：TileUrl 和 TargetFolder 为必填项");
            }

            // 后台执行下载，接口立即返回；通过 IServiceScopeFactory 创建独立 Scope
            _ = SysTasks.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ITileDownloadService>();
                await service.DownloadTilesAsync(request);
            });

            return Message("服务器下载任务已启动");
        }
    }
}
