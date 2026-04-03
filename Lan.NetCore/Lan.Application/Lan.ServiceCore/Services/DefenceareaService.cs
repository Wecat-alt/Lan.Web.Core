using Infrastructure;
using Lan.Dto;
using Lan.Model;
using Lan.Model.Vo;
using Lan.Repository;
using Lan.ServiceCore.IService;
using Lan.ServiceCore.Public;
using Lan.ServiceCore.Services.Base;
using Lan.ServiceCore.TargetCollection;
using Model;
using SqlSugar;
using System.Security.Cryptography;

namespace Lan.ServiceCore.Services
{
    [AppService(ServiceType = typeof(IDefenceareaService), ServiceLifetime = LifeTime.Singleton)]
    public class DefenceareaService : Repository<DefenceareaModel>, IDefenceareaService
    {
        public DefenceareaModel InsertUser(DefenceareaModel sysUser)
        {
            Insertable(sysUser).ExecuteReturnIdentity();
            return sysUser;
        }

        private readonly IRadarService RadarService;
        private readonly ICameraService CameraService;
        public DefenceareaService(IRadarService radarService = null, ICameraService cameraService = null)
        {
            RadarService = radarService;
            CameraService = cameraService;
        }

        /// <summary>
        /// 查询列表
        /// </summary>
        /// <param name="parm"></param>
        /// <returns></returns>
        public List<DefenceareaModel> GetList(DefenceareaQueryDto parm)
        {
            var predicate = QueryExp(parm);

            var exp = Expressionable.Create<DefenceareaModel>();
            exp.AndIF(!string.IsNullOrEmpty(parm.name), u => u.Name.Contains(parm.name));

            var response = Queryable()
                .Where(exp.ToExpression()).ToList();

            return response;
        }


        /// <summary>
        /// 获取详情
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public DefenceareaModel GetInfo(int Id)
        {
            var response = Queryable().Filter(null, true)
                .Where(f => f.Id == Id).First();
            if (response != null && response.Id > 0)
            {
                response.Cameras = CameraService.SelectCameraIds(Id);
                response.CameraIds = response.Cameras.Select(x => x.Id).ToArray();

                response.Radars = RadarService.SelectRadarIds(Id);
                response.RadarIds = response.Radars.Select(x => x.Id).ToArray();
            }
            return response;
        }

        public int AddDefencearea(DefenceareaModel model)
        {
            GlobalVariable.TrackStatus = false;

            int id = Insertable(model).ExecuteReturnIdentity();

            if (id > 0)
            {
                if (model.CameraIds != null)
                {
                    if (model.CameraIds.Length > 0)
                    {
                        CameraService.unUpdateBindCamera(id);
                        CameraService.UpdateBindCamera(model, id);
                    }
                }

                if (model.RadarIds != null)
                {
                    if (model.RadarIds.Length > 0)
                    {
                        RadarService.unUpdateBindRadar(id);
                        RadarService.UpdateBindRadar(model, id);
                    }
                }
            }
            BaseService.LoadDefenceAreaAdd(id);
            GlobalVariable.TrackStatus = true;
            return 1;
        }
        public bool GetInfoByName(string Name)
        {
            bool bl = Queryable().Any(x => x.Name == Name);
            return bl;
        }
        public bool GetInfoByName(string Name, int id)
        {
            bool bl = Queryable().Any(x => x.Name == Name && x.Id != id);
            return bl;
        }
        public int EnableDefencearea(int status)
        {
            var result = Update(w => true, it => new DefenceareaModel()
            {
                DefenceEnable = status,
                DefenceEnableName = status == 1 ? "布防" : "撤防",
            });
            BaseService.LoadDefenceAreaUpdate(status);
            return result;
        }

        public int UpdateDefencearea(DefenceareaModel model)
        {
            GlobalVariable.TrackStatus = false;
            CameraService.unUpdateBindCamera(model.Id);
            RadarService.unUpdateBindRadar(model.Id);

            CameraService.UpdateBindCamera(model, model.Id);
            RadarService.UpdateBindRadar(model, model.Id);

            BaseService.LoadDefenceAreaUpdate(model);

            int i = Update(model, true);
            GlobalVariable.TrackStatus = true;
            return i;
        }
        public int DeleteDefencearea(int[] id)
        {
            GlobalVariable.TrackStatus = false;
            foreach (var item in id)
            {
                BaseService.LoadDefenceAreaDelete(item);
            }

            int i = Delete(id);
            GlobalVariable.TrackStatus = true;
            return i;
        }
        private static Expressionable<DefenceareaModel> QueryExp(DefenceareaQueryDto parm)
        {
            var predicate = Expressionable.Create<DefenceareaModel>();

            return predicate;
        }

        public List<DefenceareaModel> GetAllList()
        {
            return Queryable().ToList();
        }

        public List<TreeSelectVo> BuildDeptTreeSelect(List<DefenceareaDtoParent> depts)
        {
            List<DefenceareaDtoParent> menuTrees = BuildDeptTree(depts);
            List<TreeSelectVo> treeMenuVos = new List<TreeSelectVo>();
            foreach (var item in menuTrees)
            {
                treeMenuVos.Add(new TreeSelectVo(item));
            }
            return treeMenuVos;
        }

        public List<DefenceareaDtoParent> BuildDeptTree(List<DefenceareaDtoParent> depts)
        {
            List<DefenceareaDtoParent> returnList = new List<DefenceareaDtoParent>();
            List<long> tempList = depts.Select(f => f.Id).ToList();
            foreach (var dept in depts)
            {
                // 如果是顶级节点, 遍历该父节点的所有子节点
                if (!tempList.Contains(dept.ParentId))
                {
                    RecursionFn(depts, dept);
                    returnList.Add(dept);
                }
            }

            if (!returnList.Any())
            {
                returnList = depts;
            }
            return returnList;
        }
        /// <summary>
        /// 递归列表
        /// </summary>
        /// <param name="list"></param>
        /// <param name="t"></param>
        private void RecursionFn(List<DefenceareaDtoParent> list, DefenceareaDtoParent t)
        {
            //得到子节点列表
            List<DefenceareaDtoParent> childList = GetChildList(list, t);
            t.children = childList;
            foreach (var item in childList)
            {
                if (GetChildList(list, item).Count() > 0)
                {
                    RecursionFn(list, item);
                }
            }
        }
        /// <summary>
        /// 递归获取子菜单
        /// </summary>
        /// <param name="list">所有菜单</param>
        /// <param name="dept"></param>
        /// <returns></returns>
        private List<DefenceareaDtoParent> GetChildList(List<DefenceareaDtoParent> list, DefenceareaDtoParent dept)
        {
            return list.Where(p => p.ParentId == dept.Id).ToList();
        }
    }
}
