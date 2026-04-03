using Lan.Database;
using Lan.ServiceCore.WebScoket;
using Model;
using System.Collections.Concurrent;
using System.Drawing;

namespace Lan.ServiceCore.TargetCollection
{
    public delegate void DefenceAreaListChangeDelegate(WDefenceArea defence, bool add);
    public delegate void DefenceAreaChangeDelegate(WDefenceArea defence);

    public class DefenceAreaManager : IDisposable, ICollection<WDefenceArea>
    {
        ConcurrentDictionary<int, WDefenceArea> _dictDefenceAreas;
        object _lockDict;
        public event DefenceAreaListChangeDelegate ListChange = null;
        public event DefenceAreaChangeDelegate DefenceAreaChange = null;

        public void OnDefenceAreaChange(WDefenceArea defence)
        {
            if (DefenceAreaChange != null)
                DefenceAreaChange(defence);
        }

        private static DefenceAreaManager _instance = null;
        public static DefenceAreaManager GetInstance()
        {
            return _instance;
        }
        public static void Init()
        {
            if (_instance == null)
                _instance = new DefenceAreaManager();
        }
        private DefenceAreaManager()
        {
            _lockDict = new object();

            _dictDefenceAreas = LoadDefenceAreasFromDatabase();
        }

        private ConcurrentDictionary<int, WDefenceArea> LoadDefenceAreasFromDatabase()
        {
            WDefenceArea[] allDefenceArea = DefenceAreaDatabase.GetDefenceAreas();
            Dictionary<int, WDefenceArea> dic;

            if (allDefenceArea == null)
                dic = new Dictionary<int, WDefenceArea>();
            else
            {
                dic = new Dictionary<int, WDefenceArea>(allDefenceArea.Length);
                foreach (WDefenceArea defenceArea in allDefenceArea)
                {
                    dic[defenceArea.ID] = defenceArea;
                }
            }

            return new ConcurrentDictionary<int, WDefenceArea>(dic);
        }

        /// <summary>
        /// 获取按ID排序的防区列表
        /// </summary>
        /// <returns></returns>
        public WDefenceArea[] GetSortedArray()
        {
            lock (_lockDict)
            {
                var areas = from area in _dictDefenceAreas.Values
                            select area;

                return areas.OrderBy(d => d.ID).ToArray();
            }
        }

        public bool Delete(int id)
        {
            WDefenceArea defenceArea;
            if (!_dictDefenceAreas.TryRemove(id, out defenceArea))
                return false;

            bool result = defenceArea.Delete();
            if (result && ListChange != null)
                ListChange(defenceArea, false);
            return result;
        }


        public bool Delete(WDefenceArea item)
        {
            if (item.ID > 0)
            {
                bool result = Delete(item.ID);
                if (result && ListChange != null)
                    ListChange(item, false);
                return result;
            }
            else
                return false;
        }

        public WDefenceArea this[int id]
        {
            get
            {
                WDefenceArea res;
                if (_dictDefenceAreas.TryGetValue(id, out res))
                    return res;
                else
                    return null;
            }
            set
            {
                _dictDefenceAreas[id] = value;
            }
        }

        public WDefenceArea Add(DefenceareaModel defenceareaModel)
        {
            WDefenceArea area = new WDefenceArea(defenceareaModel);
            if (area.ID > 0)
                if (_dictDefenceAreas.TryAdd(area.ID, area) == false)
                    area = null;
            if (area != null && ListChange != null)
                ListChange(area, true);
            return area;
        }


        public void Dispose()
        {

        }

        #region 防区功能

        RadarManager.TargetDetectDelegate _targetDetectCallback = null;
        RadarManager.RadarConnectDisconnectDelegate _radarConnectCallback = null;
        RadarManager.RadarConnectDisconnectDelegate _radarDisConnectCallback = null;

        /// <summary>
        /// 启用防区处理报警目标
        /// </summary>
        public void EnbaleRadarEvent()
        {
            if (_targetDetectCallback == null)
            {
                _targetDetectCallback = new RadarManager.TargetDetectDelegate(this.TargetDetectCallback);
                _radarConnectCallback = new RadarManager.RadarConnectDisconnectDelegate(RadarConnect);
                _radarDisConnectCallback = new RadarManager.RadarConnectDisconnectDelegate(RadarDisConnect);
                RadarManager rm = RadarManager.GetInstance();
                rm.TargetDetect += _targetDetectCallback;
                rm.RadarConnect += _radarConnectCallback;
                rm.RadarDisonnect += _radarDisConnectCallback;
            }
        }

        /// <summary>
        /// 报警回调
        /// </summary>
        /// <param name="radar">发生报警的雷达</param>
        protected void TargetDetectCallback(WRadar radar)
        {
            WDefenceArea defence = DefenceAreaManager.GetInstance()[radar.DefenceAreaId];   //获取雷达绑定的防区
            if (defence == null)
                return;

            if (!defence.Online)
                defence.Online = true;
            if (!defence.DefenceEnable)
                return;

            ////添加雷达目标到队列（有可能被过滤）
            ////如果至少添加了一个目标，就更新防区报警时间
            if (defence.AddAlarmTarget(radar))
            {
                defence.UpdateAlarmTime();
            }
        }

        private void RadarConnect(ICollection<WRadar> radars)
        {
            foreach (WRadar radar in radars)
            {
                WDefenceArea defence = this[radar.DefenceAreaId];
                if (defence != null)
                {
                    defence.Online = true;
                }
            }
        }

        private void RadarDisConnect(ICollection<WRadar> radars)
        {
            foreach (WRadar radar in radars)
            {
                WDefenceArea defence = this[radar.DefenceAreaId];
                if (defence == null)
                    return;
                bool flagOnline = false;
                foreach (WRadar r in defence.Radars)
                {
                    if (r.Online)
                    {
                        flagOnline = true;
                        break;
                    }
                }
                defence.Online = flagOnline;
            }
        }

        #endregion

        #region ICollection成员

        public void Add(WDefenceArea item)
        {
            bool bExist = _dictDefenceAreas.ContainsKey(item.ID);
            _dictDefenceAreas[item.ID] = item;
            if (bExist == false && ListChange != null)
                ListChange(item, true);
        }

        public void Clear()
        {
            _dictDefenceAreas.Clear();
            if (ListChange != null)
                ListChange(null, false);
        }

        public bool Contains(WDefenceArea item)
        {
            return _dictDefenceAreas.ContainsKey(item.ID);
        }

        public void CopyTo(WDefenceArea[] array, int arrayIndex)
        {
            _dictDefenceAreas.Values.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _dictDefenceAreas.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(WDefenceArea item)
        {
            WDefenceArea tmp;
            bool result = _dictDefenceAreas.TryRemove(item.ID, out tmp);
            if (result && ListChange != null)
                ListChange(item, false);
            return result;
        }

        public IEnumerator<WDefenceArea> GetEnumerator()
        {
            return _dictDefenceAreas.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _dictDefenceAreas.Values.GetEnumerator();
        }

        #endregion

    }
}
