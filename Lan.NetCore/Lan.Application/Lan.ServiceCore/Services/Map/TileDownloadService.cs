using Infrastructure;
using Lan.Dto;
using Lan.ServiceCore.IService;
using Lan.ServiceCore.Signalr;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text.RegularExpressions;

namespace Lan.ServiceCore.Services
{
    [AppService(InterfaceServiceType = true, ServiceLifetime = LifeTime.Scoped)]
    public class TileDownloadService : ITileDownloadService
    {
        private readonly IHubContext<MessageHub> _hubContext;
        private static readonly string[] Subdomains = { "1", "2", "3", "4" };
        private const int MinZoom = 1;
        private const int MaxZoom = 18;

        public TileDownloadService(IHubContext<MessageHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task DownloadTilesAsync(TileDownloadRequest request)
        {
            int startZoom = Math.Clamp(request.MinZoom, MinZoom, MaxZoom);
            int endZoom = Math.Clamp(request.MaxZoom, MinZoom, MaxZoom);
            if (startZoom > endZoom)
            {
                (startZoom, endZoom) = (endZoom, startZoom);
            }

            var tasks = new List<(int z, int x, int y)>();
            for (int zoom = startZoom; zoom <= endZoom; zoom++)
            {
                var range = GetTileRange(request.North, request.South, request.East, request.West, zoom);
                for (int x = range.minX; x <= range.maxX; x++)
                {
                    for (int y = range.minY; y <= range.maxY; y++)
                    {
                        tasks.Add((zoom, x, y));
                    }
                }
            }

            if (tasks.Count == 0)
            {
                return;
            }

            int done = 0, success = 0, failed = 0;
            int total = tasks.Count;
            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            foreach (var (z, x, y) in tasks)
            {
                string url = ResolveTileUrl(request.TileUrl, z, x, y);
                string current = $"z{z}/x{x}/y{y}";

                try
                {
                    var response = await httpClient.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        failed++;
                        continue;
                    }

                    byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                    string ext = InferFileExt(response.Content.Headers.ContentType?.MediaType, url);
                    string dirPath = Path.Combine(request.TargetFolder, z.ToString(), x.ToString());
                    Directory.CreateDirectory(dirPath);
                    string filePath = Path.Combine(dirPath, $"{y}.{ext}");
                    await File.WriteAllBytesAsync(filePath, bytes);

                    success++;
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.WriteLine($"瓦片下载失败 z={z} x={x} y={y}: {ex.Message}");
                }
                finally
                {
                    done++;
                    if (done % 10 == 0 || done == total)
                    {
                        var progress = new TileDownloadProgress
                        {
                            Total = total,
                            Done = done,
                            Success = success,
                            Failed = failed,
                            Current = current
                        };
                        string json = JsonConvert.SerializeObject(progress, Formatting.Indented, serializerSettings);
                        await _hubContext.Clients.All.SendAsync("TileDownloadProgress", json);
                    }
                }
            }
        }

        /// <summary>
        /// 经纬度转瓦片 X 坐标
        /// </summary>
        private static int LongitudeToTileX(double lng, int zoom)
        {
            double n = Math.Pow(2, zoom);
            return (int)Math.Floor(((lng + 180.0) / 360.0) * n);
        }

        /// <summary>
        /// 纬度转瓦片 Y 坐标（墨卡托投影）
        /// </summary>
        private static int LatitudeToTileY(double lat, int zoom)
        {
            double n = Math.Pow(2, zoom);
            double latRad = (lat * Math.PI) / 180.0;
            return (int)Math.Floor(((1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0) * n);
        }

        /// <summary>
        /// 根据 bounds 和 zoom 计算瓦片范围
        /// </summary>
        private static (int minX, int maxX, int minY, int maxY) GetTileRange(
            double north, double south, double east, double west, int zoom)
        {
            int worldMax = (int)Math.Pow(2, zoom) - 1;
            double leftLng = Math.Min(west, east);
            double rightLng = Math.Max(west, east);
            double topLat = Math.Max(north, south);
            double bottomLat = Math.Min(north, south);

            int minX = Math.Clamp(LongitudeToTileX(leftLng, zoom), 0, worldMax);
            int maxX = Math.Clamp(LongitudeToTileX(rightLng, zoom), 0, worldMax);
            int minY = Math.Clamp(LatitudeToTileY(topLat, zoom), 0, worldMax);
            int maxY = Math.Clamp(LatitudeToTileY(bottomLat, zoom), 0, worldMax);

            return (
                Math.Min(minX, maxX),
                Math.Max(minX, maxX),
                Math.Min(minY, maxY),
                Math.Max(minY, maxY)
            );
        }

        /// <summary>
        /// 替换瓦片 URL 模板中的占位符
        /// </summary>
        private static string ResolveTileUrl(string template, int z, int x, int y)
        {
            string s = Subdomains[(x + y) % Subdomains.Length];
            return template
                .Replace("{s}", s)
                .Replace("{z}", z.ToString())
                .Replace("{x}", x.ToString())
                .Replace("{y}", y.ToString());
        }

        /// <summary>
        /// 根据 Content-Type 和 URL 推断文件扩展名
        /// </summary>
        private static string InferFileExt(string contentType, string url)
        {
            if (!string.IsNullOrEmpty(contentType))
            {
                if (contentType.Contains("png")) return "png";
                if (contentType.Contains("webp")) return "webp";
                if (contentType.Contains("jpeg") || contentType.Contains("jpg")) return "jpg";
            }

            // 从 URL 中提取扩展名
            var match = Regex.Match(url, @"\.([a-zA-Z0-9]+)(?:\?|$)");
            if (match.Success)
            {
                return match.Groups[1].Value.ToLower();
            }

            return "jpg";
        }
    }
}
