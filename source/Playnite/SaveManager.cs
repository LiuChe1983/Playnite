using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Ionic.Zip;
using System.IO;
using Playnite.SDK;

namespace Playnite
{
    public class SaveManager
    {
        private static ILogger logger = LogManager.GetLogger();

        static string LocalAppData = Environment.GetEnvironmentVariable("LocalAppData");
        static string USERPROFILE = Environment.GetEnvironmentVariable("USERPROFILE");
        static Dictionary<string, string> saveKeys = new Dictionary<string, string>();
        static int UserID = -1;


        /// <summary>
        /// 待观察，异步解压缩是否会和游戏读取存档冲突
        /// </summary>
        /// <param name="saveName"></param>
        /// <param name="savePath"></param>
        public static async void LoadSave(string saveName,string savePath)
        {
            string cosPath = $"{UserID}/{saveName}.zip";  // = key
            if (!saveKeys.ContainsKey(cosPath)) return;
            string localDir = $"{AppDomain.CurrentDomain.BaseDirectory}GameSave\\";
            CosRes res = await Cos.Ins.Download(cosPath, localDir, $"{saveName}.zip");
            if(res.IsSucceed)
            {
                //解压缩咯
                string zipFileName = localDir + $"{saveName}.zip";
                ZipFile zip = new ZipFile(zipFileName, System.Text.Encoding.Default);
                string path = GetRealPath(savePath);
                zip.ExtractAll(path, ExtractExistingFileAction.OverwriteSilently);
                zip.Dispose();
                File.Delete(zipFileName);
                saveKeys.Remove(cosPath);
                logger.Debug("解压存档成功:"+ zipFileName);
            }
            else
            {
                logger.Error("读取云存档失败:" + res.Msg);
            }
        }
        /// <summary>
        /// 移除已经读取的存档key
        /// </summary>
        /// <param name="key"></param>
        public static void SaveLoaded(string key)
        {
            saveKeys.Remove(key);
        }
        public static string GetRealPath(string path)
        {
            return path.Replace("%LocalAppData%", LocalAppData).Replace("%USERPROFILE%", USERPROFILE);
        }

        
        public static void GetCloudSaveList(int userid)
        {
            // "1/"
            UserID = userid;
            // 1/BloodstainedRotN.zip
            saveKeys = Cos.Ins.List($"{userid}/").ToDictionary(x=>x, x => x);
            saveKeys.Remove(@"/"); 
        }
    }
}
