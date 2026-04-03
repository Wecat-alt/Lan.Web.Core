using Lan.ServiceCore.TargetCollection;
using Lan.ServiceCore.WebScoket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lan.ServiceCore.Public
{
    public class RadarBackgroundService : BackgroundService
    {
        private readonly ILogger<RadarBackgroundService> _logger;
        private bool _initialized;

        public RadarBackgroundService(ILogger<RadarBackgroundService> logger)
        {
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                RadarManager.Init();
                DefenceAreaManager.GetInstance()?.EnbaleRadarEvent();
                _initialized = true;
                _logger.LogInformation("雷达后台服务启动");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "雷达后台服务启动失败");
                throw;
            }

            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_initialized)
                {
                    RadarManager.GetInstance()?.Dispose();
                    _initialized = false;
                    _logger.LogInformation("雷达后台服务停止");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "雷达后台服务停止失败");
            }

            return base.StopAsync(cancellationToken);
        }
    }
}
