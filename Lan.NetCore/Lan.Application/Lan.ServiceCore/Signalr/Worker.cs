using Lan.ServiceCore.TargetCollection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using SharpJaad.AAC.Tools;
using Lan.ServiceCore.Services;
using Model;

namespace Lan.ServiceCore.Signalr
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHubContext<MessageHub> _messageHub;
        static ConcurrentQueue<SendMS> _radarTargetQueue = new ConcurrentQueue<SendMS>();

        ConcurrentDictionary<int, string> _dictSpeedFX = new ConcurrentDictionary<int, string>();

        public Worker(ILogger<Worker> logger, IHubContext<MessageHub> messageHub)
        {
            _logger = logger;
            _messageHub = messageHub;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                //_logger.LogInformation("方案一 LogInformation; this is CommonInfoController 构造函数~");

                var serializerSettings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    // DateFormatString = timeFormatStr
                };

                while (!stoppingToken.IsCancellationRequested)
                {
                    SendMS tarItem;
                    while (_radarTargetQueue.TryDequeue(out tarItem))
                    {
                        try
                        {
                            string CurrTime = tarItem.DateTime;

                            string mmss = "";
                            if (_dictSpeedFX.TryGetValue(tarItem.TargetId, out mmss))
                            {
                                if (CurrTime == mmss)
                                {
                                    //Console.WriteLine("ReceiveTargetData:已存在，跳过");
                                    continue;
                                }
                                else
                                {
                                    _dictSpeedFX.TryUpdate(tarItem.TargetId, CurrTime, mmss);
                                    string json = JsonConvert.SerializeObject(tarItem, Formatting.Indented, serializerSettings);
                                    await _messageHub.Clients.All.SendAsync("ReceiveTargetData", json);
                                    //Console.WriteLine("ReceiveTargetData:已存在，发送");
                                }
                            }
                            else
                            {
                                _dictSpeedFX.TryAdd(tarItem.TargetId, CurrTime);

                                string json = JsonConvert.SerializeObject(tarItem, Formatting.Indented, serializerSettings);
                                await _messageHub.Clients.All.SendAsync("ReceiveTargetData", json);
                                //Console.WriteLine("ReceiveTargetData:发送成功");
                            }
                        }
                        catch (Exception innerEx)
                        {
                            _logger.LogError(innerEx, "SendAsync失败");
                        }
                    }
                    await Task.Delay(10, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ReceiveTargetData");
            }
        }

        public static bool AddTarget(SendMS sendMS)
        {
            _radarTargetQueue.Enqueue(sendMS);
            return true;
        }
    }
    public class TargetMessage
    {
        public int TargetId { get; set; }

        public DateTime DateTime { get; set; }
    }
}
