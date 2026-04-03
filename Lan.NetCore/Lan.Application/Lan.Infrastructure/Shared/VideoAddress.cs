using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Lan.Infrastructure
{
    public class VideoAddress
    {
        private static string PathPrefix(string file)
        {
            string _strFlie = file.Replace("\\", "/") + "/" + DateTime.Now.ToString("yyyy_MM_dd");
            EnsureDirExist(_strFlie);

            return _strFlie + "/" + DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss.ffff_");
        }

        //确保目录存在
        public static void EnsureDirExist(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);

                }
            }
            catch (Exception ex)
            {
                //Log.Error(ex.ToString());
            }
        }

        //生成视频文件名
        public static string CreateVideoName(string ip, string file)
        {
            try
            {
                string aa = "";
                aa = PathPrefix(file) + ip + ".mp4";
                return aa;
                //}
            }
            catch (Exception ex)
            {
                //Log.Error(ex.ToString());
                return "";
            }
        }
    }
}
