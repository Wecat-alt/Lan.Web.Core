using Lan.Database;
using Model;
using System.Collections.Concurrent;
using System.Data;

namespace Lan.ServiceCore.TargetCollection
{
    public class CameraManager : IDisposable, ICollection<WCamera>
    {
        ConcurrentDictionary<int, WCamera> _dictCameras;
        object _lockDict;

        private static CameraManager _instance = null;
        public static CameraManager GetInstance()
        {
            return _instance;
        }

        public static void Init()
        {
            if (_instance == null)
            {
                _instance = new CameraManager();
                _instance._dictCameras = _instance.LoadCamerasFromDatabase();
            }
        }

        private CameraManager()
        {
            _lockDict = new object();
            _dictCameras = new ConcurrentDictionary<int, WCamera>();
        }

        private ConcurrentDictionary<int, WCamera> LoadCamerasFromDatabase()
        {
            WCamera[] allCamera = CameraDatabase.GetCameras();

            Dictionary<int, WCamera> dic;

            if (allCamera == null)
                dic = new Dictionary<int, WCamera>();
            else
            {
                dic = allCamera.ToDictionary(static c => c.ID);
            }

            return new ConcurrentDictionary<int, WCamera>(dic);
        }


        public WCamera[] GetBindingCameraOfDefenceArea(int defenceAreaId)
        {
            var camera = from cam in _dictCameras.Values
                         where cam.DefenceAreaId == defenceAreaId
                         select cam;
            return camera.ToArray();
        }

        public WCamera this[int id]
        {
            get
            {
                WCamera res;
                if (_dictCameras.TryGetValue(id, out res))
                    return res;
                else
                    return null;
            }
            set
            {
                _dictCameras[id] = value;
            }
        }

        public WCamera this[string ip]
        {
            get
            {
                foreach (WCamera camera in _dictCameras.Values)
                {
                    if (ip.Equals(camera.Ip))
                        return camera;
                }
                return null;
            }
        }

        /// <summary>
        /// 获取排序的相机列表
        /// </summary>
        /// <returns></returns>
        public WCamera[] GetSortedArray()
        {
            lock (_lockDict)
            {
                var cameras_orderd = (from camera in _dictCameras.Values
                                      select camera).OrderBy((camera) => camera.Ip);

                return cameras_orderd.ToArray();
            }
        }

        public void Dispose()
        {
            if (_dictCameras != null)
            {
                foreach (var camera in _dictCameras.Values)
                {
                    camera.Dispose();
                }
                _dictCameras.Clear();
            }
            //throw new NotImplementedException();
        }

        #region ICollection成员

        public void Add(WCamera item)
        {
            bool bContains;
            bContains = _dictCameras.ContainsKey(item.ID);
            if (bContains)
                throw new ArgumentException("已存在相同键的值" + item.Ip);
            _dictCameras.TryAdd(item.ID, item);
        }

        public void Clear()
        {
            //throw new NotSupportedException();
            //_dictCameras.Clear();
        }

        public bool Contains(string ip)
        {
            WCamera camera = this[ip];
            return camera != null;
        }

        public bool Contains(WCamera item)
        {
            return _dictCameras.ContainsKey(item.ID);
        }

        public void CopyTo(WCamera[] array, int arrayIndex)
        {
            _dictCameras.Values.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _dictCameras.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(WCamera item)
        {
            bool result;
            WCamera tmp;
            result = _dictCameras.TryRemove(item.ID, out tmp);
            if (result)
            {
                //tmp.DeleteInDatabase();
                return true;
            }
            return false;
        }

        public IEnumerator<WCamera> GetEnumerator()
        {
            return _dictCameras.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _dictCameras.Values.GetEnumerator();
        }

        #endregion
    }
}
