using Lan.ServiceCore.IService;
using Lan.ServiceCore.TargetCollection;
using Model;
using SqlSugar.IOC;
using System.Collections.Concurrent;

namespace Lan.ServiceCore.WebScoket
{
    public class RadarManager : IDisposable
    {
        ConcurrentDictionary<string, WRadar> _dictRadars;

        object _lockDict;

        #region 事件和委托

        /// <summary>
        /// 雷达连接和断开事件的委托
        /// </summary>
        /// <param name="radar">产生事件的雷达</param>
        /// <param name="connect">ture表示连接事件，false表示断开事件</param>
        public delegate void RadarConnectDisconnectDelegate(ICollection<WRadar> radar);
        /// <summary>
        /// 雷达连接事件
        /// </summary>
        public event RadarConnectDisconnectDelegate RadarConnect = null;

        /// <summary>
        /// 雷达断开事件
        /// </summary>
        public event RadarConnectDisconnectDelegate RadarDisonnect = null;

        public delegate void TargetDetectDelegate(WRadar radar);

        /// <summary>
        /// 雷达目标上报事件
        /// </summary>
        public event TargetDetectDelegate TargetDetect = null;

        /// <summary>
        /// 引发雷达连接/断开事件
        /// </summary>
        /// <param name="radar"></param>
        /// <param name="connect"></param>
        public void OnRadarConnect(WRadar radar, bool connect)
        {
            if (connect && RadarConnect != null)
                RadarConnect(new WRadar[] { radar });
            else if (!connect && RadarDisonnect != null)
                RadarDisonnect(new WRadar[] { radar });
        }

        /// <summary>
        /// 引发多个雷达断开事件
        /// </summary>
        /// <param name="radars"></param>
        /// <param name="connect"></param>
        public void OnMultiRadarDisconnect(ICollection<WRadar> radars)
        {
            if (RadarDisonnect != null)
                RadarDisonnect(radars);
        }

        /// <summary>
        /// 引发雷达目标上报事件
        /// </summary>
        /// <param name="radar"></param>
        public void OnTargetDetect(WRadar radar)
        {
            if (TargetDetect != null)
                TargetDetect(radar);
        }

        #endregion

        private static RadarManager _instance = null;
        public static RadarManager GetInstance()
        {
            return _instance;
        }

        /// <summary>
        /// 初始化雷达管理对象
        /// </summary>
        public static void Init()
        {
            _instance = new RadarManager();
        }

        private RadarManager()
        {
            _lockDict = new object();
            _dictRadars = LoadRadarsFromDatabase();
        }

        /// <summary>
        /// 从数据库加载所有的雷达
        /// </summary>
        /// <returns></returns>
        private ConcurrentDictionary<string, WRadar> LoadRadarsFromDatabase()
        {
            List<WRadar> list = new List<WRadar>();

            var radarService = App.GetService<IRadarService>();
            var RadarList = radarService.GetAllList();

            RadarList.ForEach(item =>
            {
                WRadar wRadar = new WRadar(item);
                list.Add(wRadar);
            });

            WRadar[] allRadar = list.ToArray();
            Dictionary<string, WRadar> dic;

            if (allRadar == null)
                dic = new Dictionary<string, WRadar>();
            else
            {
                dic = allRadar.ToDictionary(static r => r.Ip.ToString());
            }

            return new ConcurrentDictionary<string, WRadar>(dic);
        }

        public void Dispose()
        {
            _dictRadars.Clear();
        }
        public WRadar[] GetBindingRadarOfDefenceArea(int defenceAreaId)
        {
            lock (_lockDict)
            {
                var radar = from r in _dictRadars.Values
                            where r.DefenceAreaId == defenceAreaId
                            select r;
                return radar.ToArray();
            }
        }
        public bool DeleteRadar(string ip)
        {
            WRadar radar;
            if (_dictRadars.TryGetValue(ip, out radar))
                return DeleteRadar(radar);
            else return false;
        }
        public bool DeleteRadar(WRadar radar)
        {
            if (radar.DefenceAreaId > -1)
            {
                WDefenceArea old = DefenceAreaManager.GetInstance()[radar.DefenceAreaId];
                old.UnbindRadar(radar);
            }
            bool result = this.Remove(radar);
            return result;
        }
        public WRadar Add(RadarModel radarModel)
        {
            if (_dictRadars.ContainsKey(radarModel.Ip.ToString()))
                return null;
            WRadar newRadar = new WRadar(radarModel);

            _dictRadars.TryAdd(newRadar.Ip, newRadar);

            return newRadar;
        }

        public WRadar this[string ip]
        {
            get
            {
                WRadar res;
                if (_dictRadars.TryGetValue(ip, out res))
                    return res;
                else
                    return null;
            }
            set
            {
                _dictRadars[ip] = value;
            }
        }

        public bool Remove(WRadar item)
        {
            bool result;
            WRadar tmp;
            result = _dictRadars.TryRemove(item.Ip, out tmp);
            if (result)
            {
                OnRadarConnect(item, false);
                return true;
            }
            return false;
        }
    }
}
