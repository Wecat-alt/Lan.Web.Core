using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Lan.ServiceCore.Public
{
    /// <summary>
    /// 更新前端 config.js 和 appsettings.json 中的 IP 地址
    /// </summary>
    public class ConfigJsUpdater
    {
        /// <summary>
        /// 前端 config.js 在服务器上的固定路径
        /// </summary>
        private const string ConfigJsPath = @"D:/RVS_WEB/lan/config.js";

        /// <summary>
        /// 验证 IPv4 地址是否合法
        /// </summary>
        public static string? ValidateIp(string? ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return "ip 参数不能为空";

            if (!IPAddress.TryParse(ip, out var addr) || addr.AddressFamily != AddressFamily.InterNetwork)
                return $"无效的 IPv4 地址: {ip}";

            return null;
        }

        /// <summary>
        /// 用指定 IP 更新 config.js 中的 localhost
        /// </summary>
        public string UpdateConfigJs(string ip)
        {
            try
            {
                string content = File.ReadAllText(ConfigJsPath);
                string updatedContent = content.Replace("localhost", ip);

                if (content == updatedContent)
                    return "config.js 中未找到 localhost，无需更新";

                File.WriteAllText(ConfigJsPath, updatedContent);
                return $"config.js: localhost → {ip}";
            }
            catch (Exception ex)
            {
                return $"config.js 更新失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 用指定 IP 更新 appsettings.json 中 CorsUrls 里的 IP 地址
        /// </summary>
        public string UpdateAppSettings(string ip, string appSettingsPath)
        {
            try
            {
                if (!File.Exists(appSettingsPath))
                    return $"appsettings.json 不存在: {appSettingsPath}";

                string json = File.ReadAllText(appSettingsPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("CorsUrls", out var corsElement))
                    return "appsettings.json 中未找到 CorsUrls 配置";

                var corsList = new List<string>();
                bool changed = false;

                foreach (var url in corsElement.EnumerateArray())
                {
                    var urlStr = url.GetString() ?? "";
                    // 替换 URL 中的 IP 地址（保留 localhost 不动）
                    var newUrl = Regex.Replace(urlStr, @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b", ip);
                    if (newUrl != urlStr)
                        changed = true;
                    corsList.Add(newUrl);
                }

                if (!changed)
                    return "appsettings.json 中 CorsUrls 未包含 IP 地址，无需更新";

                // 重新构建 JSON
                string indent = "  ";
                var corsJson = string.Join(",\n" + indent + indent, corsList.Select(u => $"\"{u}\""));
                var newCorsSection = $"{indent}\"CorsUrls\": [\n{indent}{indent}{corsJson}\n{indent}]";

                // 替换 CorsUrls 段落（简单正则替换，保持其他配置不变）
                var updatedJson = Regex.Replace(json,
                    @"\s*""CorsUrls""\s*:\s*\[[^\]]*\]",
                    "\n" + newCorsSection,
                    RegexOptions.Singleline);

                File.WriteAllText(appSettingsPath, updatedJson);
                return $"appsettings.json: CorsUrls IP 已更新为 {ip}";
            }
            catch (Exception ex)
            {
                return $"appsettings.json 更新失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 获取本机所有物理网卡的 IPv4 地址列表
        /// </summary>
        public static List<string> GetAllLocalIPv4()
        {
            var physicalIps = new List<string>();
            var otherIps = new List<string>();

            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback
                        || ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel
                        || ni.OperationalStatus != OperationalStatus.Up)
                    {
                        continue;
                    }

                    foreach (var ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork
                            && !IPAddress.IsLoopback(ip.Address))
                        {
                            var ipStr = ip.Address.ToString();

                            if (ipStr.StartsWith("172."))
                                continue;

                            if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet
                                || ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                            {
                                physicalIps.Add(ipStr);
                            }
                            else
                            {
                                otherIps.Add(ipStr);
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            // 物理网卡优先，其他的放后面
            physicalIps.AddRange(otherIps);
            return physicalIps;
        }

        /// <summary>
        /// 通过修改 web.config 的最后写入时间，触发 IIS 自动回收应用池
        /// </summary>
        public string RecycleAppPool(string webConfigPath)
        {
            try
            {
                if (!File.Exists(webConfigPath))
                    return "web.config 不存在，跳过回收";

                File.SetLastWriteTimeUtc(webConfigPath, DateTime.UtcNow);
                return "应用池已触发回收";
            }
            catch (Exception ex)
            {
                return $"应用池回收失败: {ex.Message}";
            }
        }
    }
}
