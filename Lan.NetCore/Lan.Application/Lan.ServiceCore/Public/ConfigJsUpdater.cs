using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Lan.ServiceCore.Public
{
    /// <summary>
    /// 更新前端 config.js 中的 IP 地址
    /// </summary>
    public class ConfigJsUpdater
    {
        /// <summary>
        /// 前端 config.js 在发布目录下的相对路径
        /// </summary>
        private static string ConfigJsPath => Path.Combine(AppContext.BaseDirectory, "wwwroot", "config.js");

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
                if (!File.Exists(ConfigJsPath))
                    return $"config.js 不存在: {ConfigJsPath}";

                string content = File.ReadAllText(ConfigJsPath);
                string updatedContent = content.Replace("localhost", ip);

                if (content == updatedContent)
                    return $"config.js 中未找到 localhost，无需更新";

                File.WriteAllText(ConfigJsPath, updatedContent);
                return $"config.js: localhost → {ip}";
            }
            catch (Exception ex)
            {
                return $"config.js 更新失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 自动检测本机 IP 并更新 config.js（启动时调用）
        /// </summary>
        public string AutoUpdateConfigJs()
        {
            var ips = GetAllLocalIPv4();
            if (ips.Count == 0)
                return "未检测到本机 IPv4 地址，跳过 config.js 更新";

            var ip = ips[0]; // 优先使用物理网卡 IP
            return UpdateConfigJs(ip);
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

    }
}
