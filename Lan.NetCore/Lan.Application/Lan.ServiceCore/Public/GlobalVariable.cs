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

        public GlobalVariable()
        {
            var sysConfigService = App.GetService<ISysConfigService>();
            FilePath = sysConfigService.GetSysConfigByKey("filepath").ConfigValue;
            recordservicehost= sysConfigService.GetSysConfigByKey("recordservicehost").ConfigValue;

            maxAlarmTime = int.Parse(sysConfigService.GetSysConfigByKey("maxAlarmTime").ConfigValue);

            radarAlarmOvertime = int.Parse(sysConfigService.GetSysConfigByKey("radarAlarmOvertime").ConfigValue);
        }

    }
}
