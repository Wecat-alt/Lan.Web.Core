using Infrastructure;
using Lan.ServiceCore.IService;

namespace Lan.ServiceCore.Public
{
    public class GlobalVariable
    {
        public static bool TrackStatus { get; set; } = true;

        public static string FilePath { get; set; }
        public static string recordservicehost { get; set; }

        public static int maxAlarmTime { get; set; }
        public static int radarAlarmOvertime { get; set; }

        /// <summary>
        /// 缓存的配置 key 集合，UpdateSysConfig 时用于判断是否需要刷新
        /// </summary>
        public static readonly HashSet<string> CachedConfigKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "filepath",
            "recordservicehost",
            "maxAlarmTime",
            "radarAlarmOvertime"
        };

        public GlobalVariable()
        {
            Refresh();
        }

        /// <summary>
        /// 重新从数据库读取所有缓存的配置值
        /// </summary>
        public static void Refresh()
        {
            var sysConfigService = App.GetService<ISysConfigService>();

            FilePath = sysConfigService.GetSysConfigByKey("filepath")?.ConfigValue;
            recordservicehost = sysConfigService.GetSysConfigByKey("recordservicehost")?.ConfigValue;

            var maxAlarmTimeStr = sysConfigService.GetSysConfigByKey("maxAlarmTime")?.ConfigValue;
            maxAlarmTime = int.TryParse(maxAlarmTimeStr, out var mat) ? mat : 0;

            var radarAlarmOvertimeStr = sysConfigService.GetSysConfigByKey("radarAlarmOvertime")?.ConfigValue;
            radarAlarmOvertime = int.TryParse(radarAlarmOvertimeStr, out var rao) ? rao : 0;
        }
    }
}
