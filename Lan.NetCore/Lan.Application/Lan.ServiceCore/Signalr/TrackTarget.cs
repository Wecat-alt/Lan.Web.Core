using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.ServiceCore.Signalr
{
    public class TrackTarget : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHubContext<MessageHub> _messageHub;

        static ConcurrentQueue<int> _TrackTarget = new ConcurrentQueue<int>();

        public TrackTarget(ILogger<Worker> logger, IHubContext<MessageHub> messageHub)
        {
            _logger = logger;
            _messageHub = messageHub;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    int tarId = 0;
                    bool bl = _TrackTarget.TryDequeue(out tarId);
                    if (bl)
                    {
                        try
                        {
                            await _messageHub.Clients.All.SendAsync("TrackTargetData", tarId);
                            //Console.WriteLine("TrackTargetData:发送成功");
                        }
                        catch (Exception ex) { }
                    }
                    await Task.Delay(100, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ReceiveTargetData");
            }
        }
        public static void AddTrackTarget(int targetId)
        {
            _TrackTarget.Enqueue(targetId);
        }
    }
}
