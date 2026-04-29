using CAT.NsrRadarSdk;
using CAT.NsrRadarSdk.NsrTypes;
using Lan.ServiceCore.Services;
using Lan.ServiceCore.TargetCollection;
using Model;
using SqlSugar.IOC;
using System.Collections.Concurrent;
using System.Net;

namespace Lan.ServiceCore.WebScoket
{
    public class RadarManager : IDisposable
    {
        ConcurrentDictionary<string, WRadar> _dictRadars;

        object _lockDict;

        bool _bDisposing;

        Thread RadarConnectThread;

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
            try
            {

                _lockDict = new object();
                _dictRadars = LoadRadarsFromDatabase();

                _radars = new ConcurrentDictionary<string, NsrRadar>();
                bool NsrRadarUseTcp = true;


                RadarConnectThread = new Thread(RadarConnectThreadWhile);
                RadarConnectThread.IsBackground = true;

                NsrSdk.Instance.Init(9000, NsrRadarUseTcp);
                NsrSdk.Instance.Timeout = 3000;
                NsrSdk.Instance.StartReceiveBroadcast(RadarBroadcast);

                NsrSdk.Instance.TargetDetect += FormTestRadar_TargetDetect;
                NsrSdk.Instance.RadarOnlineStateChanged += _manager_RadarConnect;
                RadarConnectThread.Start();

            }
            catch (Exception ex)
            {

            }
        }

        public void RadarConnect1(WRadar radar)
        {
            var task1 = new Task(() =>
            {
                RadarConnectThreadWhile1(radar);
            });
            task1.Start();
        }

        object objRadarConnectThreadWhile1 = new object();
        private void RadarConnectThreadWhile1(object obj)
        {
            WRadar radar = obj as WRadar;
            try
            {
                if (radar.C_NsrRadar == null)
                {
                    radar.C_NsrRadar = NsrSdk.Instance.CreateRadar(radar.Ip, 50000);
                }
                if (!radar.C_NsrRadar.Connect())
                {
                    if (radar.C_NsrRadar != null)
                    {
                        radar.C_NsrRadar.DisConnect();
                        radar.C_NsrRadar = null;
                    }
                    Console.WriteLine(radar.Ip + "雷达连接失败");
                }
                else
                {
                    radar.Online = true;
                    OnRadarConnect(radar, true);

                    if (DateTime.Now - radar.SetTime > TimeSpan.FromHours(1))
                    {
                        if (radar.C_NsrRadar.SetTime(DateTime.Now))
                        {
                            radar.SetTime = DateTime.Now;
                        }
                    }

                    Console.WriteLine(radar.Ip + "雷达连接成功");
                }
            }
            catch (Exception _e)
            {
                if (radar != null)
                {
                    if (radar.C_NsrRadar != null)
                    {
                        radar.C_NsrRadar.DisConnect();
                        radar.C_NsrRadar = null;
                    }
                }
                Console.WriteLine(radar.Ip + "雷达连接" + _e.ToString());
            }
        }
        private void RadarConnectThreadWhile(object obj)
        {
            Thread.Sleep(5000);
            while (!_bDisposing)
            {
                if (_dictRadars != null)
                {
                    foreach (WRadar radar in _dictRadars.Values)
                    {

                        try
                        {
                            if (radar.Status == 1)
                            {
                                RadarConnect1(radar);
                            }
                            Thread.Sleep(500);
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message != "No response after waitting for 3000 milliseconds.")
                            {
                                Console.WriteLine(ex.ToString());
                            }
                        }

                    }
                }
                Thread.Sleep(5000);
            }
        }

        private void FormTestRadar_TargetDetect(NsrRadar radar, RVS_Target_List targetList)
        {
            WRadar _WRadar;

            _dictRadars.TryGetValue(radar.Ip, out _WRadar);
            if (_WRadar == null)
            {

            }
            else
            {
                _WRadar.RadarTargets = targetList;
                OnTargetDetect(_WRadar);
            }
        }

        /// <summary>
        /// 从数据库加载所有的雷达
        /// </summary>
        /// <returns></returns>
        private ConcurrentDictionary<string, WRadar> LoadRadarsFromDatabase()
        {
            List<WRadar> list = new List<WRadar>();

            RadarService radarService = new RadarService();
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

        private ConcurrentDictionary<string, NsrRadar> _radars;

        private void _manager_RadarConnect(NsrRadar radar, bool online)
        {
            WRadar _WRadar;
            _dictRadars.TryGetValue(radar.Ip, out _WRadar);
            if (_WRadar == null)
            {
                return;
            }
            _WRadar.C_NsrRadar = radar;
            if (online && RadarConnect != null)
                RadarConnect(new WRadar[] { _WRadar });
            else if (!online && RadarDisonnect != null)
                RadarDisonnect(new WRadar[] { _WRadar });
        }



        private void RadarBroadcast(NsrRadar radar, ref RVS_PARAM_BROADCAST info)
        {
            if (_dictRadars == null)
            {
                return;
            }

            if (_dictRadars.ContainsKey(radar.Ip))
            {
                WRadar _WRadar;
                _dictRadars.TryGetValue(radar.Ip, out _WRadar);
                if (_WRadar == null)
                {
                    return;
                }
            }
            else
            {
                WRadar _WRadar;
                _dictRadars.TryGetValue(radar.Ip, out _WRadar);
                if (_WRadar == null)
                {
                }
            }
        }

        public void Dispose()
        {
            try
            {
                if (_bDisposing)
                    return;
                _bDisposing = true;
                NsrSdk.Instance.StopReceiveBroadcast();
                _dictRadars.Clear();
            }
            catch
            { }
            //_thReceiveUdp.Abort();    //已由_bDisposing控制线程停止
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
