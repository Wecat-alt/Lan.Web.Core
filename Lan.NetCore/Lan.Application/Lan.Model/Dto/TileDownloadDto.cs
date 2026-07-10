namespace Lan.Dto
{
    /// <summary>
    /// 瓦片下载请求参数
    /// </summary>
    public record TileDownloadRequest
    {
        /// <summary>北纬度</summary>
        public double North { get; set; }
        /// <summary>南纬度</summary>
        public double South { get; set; }
        /// <summary>东经度</summary>
        public double East { get; set; }
        /// <summary>西经度</summary>
        public double West { get; set; }
        /// <summary>最小 zoom 层级</summary>
        public int MinZoom { get; set; }
        /// <summary>最大 zoom 层级</summary>
        public int MaxZoom { get; set; }
        /// <summary>瓦片 URL 模板，含 {s}{z}{x}{y} 占位符</summary>
        public string? TileUrl { get; set; }
        /// <summary>服务器目标文件夹路径</summary>
        public string? TargetFolder { get; set; }
    }

    /// <summary>
    /// 瓦片下载进度（通过 SignalR 推送）
    /// </summary>
    public class TileDownloadProgress
    {
        public int Total { get; set; }
        public int Done { get; set; }
        public int Success { get; set; }
        public int Failed { get; set; }
        public string? Current { get; set; }
    }
}
