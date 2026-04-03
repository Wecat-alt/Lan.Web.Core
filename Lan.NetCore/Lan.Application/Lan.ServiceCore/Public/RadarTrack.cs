using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Lan.ServiceCore.Public
{
    internal class RadarTrack : BackgroundService
    {
        public RadarTrack(ILogger<RadarDataChannelService> logger, IServiceProvider serviceProvider)
        {
            
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                
            }
        }
    }
}
