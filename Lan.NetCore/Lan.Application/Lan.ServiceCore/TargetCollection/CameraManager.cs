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
                dic = new Dictionary<int, WCamera>(allCamera.Length);
                foreach (WCamera camera in allCamera)
                {
                    dic[camera.ID] = camera;
                }
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

        public WCamera GetLastCamera()
        {
            //int lastId = CameraDatabase.GetMaxId();
            //WCamera camera;
            //if (_dictCameras.TryGetValue(lastId, out camera))
            //    return camera;
            //else
            return null;
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


        #region 相机登录线程
        //控制线程
        System.Threading.Thread _thCameraConnect;
        //线程运行状态
        bool _bThreadRunning = false;

        //开启线程
        public bool CameraConnectThreadStart()
        {
            try
            {
                _bThreadRunning = true;
                _thCameraConnect = new System.Threading.Thread(CameraConnectThreadRun);
                _thCameraConnect.IsBackground = true;
                _thCameraConnect.Name = "相机连接线程";
                _thCameraConnect.Start();
                //Log.Debug(_thCameraConnect.Name + "_已开启");
                return true;
            }
            catch (Exception ex)
            {
                //Log.Error(ex.ToString());
                return false;
            }
        }

        //等待线程结束
        public bool CameraConnectThreadStop()
        {
            try
            {
                if (_thCameraConnect != null)
                {
                    _bThreadRunning = false;
                    //停止线程
                    _thCameraConnect.Join();//等待线程停止
                    //Log.Debug(_thCameraConnect.Name + "_已停止");
                    _thCameraConnect = null;
                }

                return true;
            }
            catch (Exception ex)
            {
                //Log.Error(ex.ToString());
                return false;
            }
        }

        //控制线程
        void CameraConnectThreadRun()
        {
            // Log.Debug(_thCameraConnect.Name + "_已开启");
            int currIndex = 0;
            WCamera camera;
            //System.Threading.Thread.Sleep(1000);
            //登录所有相机
            foreach (WCamera ca in _dictCameras.Values)
            {

            }
            System.Threading.Thread.Sleep(2000);
            while (_bThreadRunning)
            {
                System.Threading.Thread.Sleep(100);

                try
                {

                }
                catch (Exception ex)
                {
                    //Log.Error(ex.ToString());
                }
            }
            //Log.Debug(_thCameraConnect.Name + "_运行结束");
        }



        #endregion

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
