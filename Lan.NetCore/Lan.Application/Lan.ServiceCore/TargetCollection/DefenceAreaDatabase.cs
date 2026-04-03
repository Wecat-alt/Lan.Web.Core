
using Lan.ServiceCore.Services;
using Lan.ServiceCore.TargetCollection;
using Model;

namespace Lan.Database
{
    class DefenceAreaDatabase
    {
        public static WDefenceArea[] GetDefenceAreas()
        {
            DefenceareaService defenceareaService = new DefenceareaService();
            var ds = defenceareaService.GetAllList();

            List<WDefenceArea> list = new List<WDefenceArea>(ds.Count);

            foreach (DefenceareaModel dtRow in ds)
            {
                try
                {
                    WDefenceArea camera = new WDefenceArea(dtRow);
                    list.Add(camera);
                }
                catch (System.Exception ex)
                {
                    // Log.Error("从数据库加载防区失败：\r\n" + ex.ToString());
                }
            }
            return list.ToArray();
        }
    }
}
