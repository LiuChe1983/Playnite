using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Ionic.Zip;
using System.IO;
using Playnite.SDK;
using System.Timers;

namespace Playnite
{
    public class SaveManager
    {
        private static ILogger logger = LogManager.GetLogger();

        static string LocalAppData = Environment.GetEnvironmentVariable("LocalAppData");
        static string USERPROFILE = Environment.GetEnvironmentVariable("USERPROFILE");
        static Dictionary<string, string> saveKeys = new Dictionary<string, string>();
        static Dictionary<string, string> defaultSaveKeys = new Dictionary<string, string>();

        static int UserID = -1;
        //static string userCloudSavePath; //每次用户登录时更新
        static List<GameSaveWatcher> gsws;
        static Timer timer;
        static string gameSavePath, gameSavePathUpload;

        static object lockGsws = new object();

        static SaveManager()
        {
            saveKeys = new Dictionary<string, string>();
            gsws = new List<GameSaveWatcher>();
            timer = new System.Timers.Timer(1000*60*5); //暂时设置5分钟
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
            gameSavePath = $"{AppDomain.CurrentDomain.BaseDirectory}GameSave\\";
            gameSavePathUpload = $"{AppDomain.CurrentDomain.BaseDirectory}GameSaveUpload\\";
            if (!Directory.Exists(gameSavePath)) Directory.CreateDirectory(gameSavePath);
            if (!Directory.Exists(gameSavePathUpload)) Directory.CreateDirectory(gameSavePathUpload);

        }

        private static async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            logger.Info("开始检查存档");
            await CheckSaveUpdate();
            logger.Info("检查存档完成");

        }

        public static void Clear()
        {
            foreach (var gsw in gsws)
            {
                gsw.Clear();
            }
            gsws.Clear();
            logger.Info("SaveManager Cleared");
        }

        public static async Task CheckSaveUpdate()
        {
            IEnumerable<GameSaveWatcher> changedGsws;
            lock (lockGsws)
            {
                changedGsws = from s in gsws
                                  where s.IsChanged
                                  select s;
            }

            foreach (var gsw in changedGsws)
            {
                if (gsw.IsChanged)
                {
                    string fullZipName = gameSavePathUpload + gsw.game.GameName + ".zip";

                    DirectoryInfo di = new DirectoryInfo(gsw.realPath);
                    if (!di.Exists) continue;
                    try
                    {
                        ZipFile zip = new ZipFile(System.Text.Encoding.Default);
                        zip.AddDirectory(di.FullName);
                        zip.Save(fullZipName);
                        if (File.Exists(fullZipName))
                        {
                            logger.Info("压缩存档成功:" + fullZipName);
                        }
                    }
                    catch(Exception zipEx)
                    {
                        logger.Info("压缩存档失败:" + fullZipName+zipEx.Message);
                        continue;
                    }
                    try
                    {
                        CosRes res = await Cos.Ins.Upload($"{UserID}/{gsw.game.GameName}.zip", fullZipName);
                        if (res.IsSucceed)
                            logger.Info("COS上传成功:" + res.Msg);
                        else
                        {
                            logger.Info("COS上传失败:" + res.Msg);
                            continue;
                        }
                    }
                    catch (Exception cosEx)
                    {
                        logger.Info("COS上传错误:" + cosEx.Message);
                    }
                    finally
                    {
                        if (File.Exists(fullZipName)) File.Delete(fullZipName);
                    }
                    gsw.IsChanged = false;
                }
            }
        }

        /// <summary>
        /// 待观察，异步解压缩是否会和游戏读取存档冲突 = 会
        /// </summary>
        /// <param name="saveName"></param>
        /// <param name="savePath"></param>
        public static async Task<bool> LoadSave(string saveName,string savePath)
        {
            string cosPath = $"{UserID}/{saveName}.zip";  // = key
            string realSavePath = GetRealPath(savePath);
            if (!saveKeys.ContainsKey(cosPath))
            {
                //不存在存档，查找默认存档是否存在
                if (defaultSaveKeys.ContainsKey($"default/{saveName}.zip"))
                {
                    logger.Debug("用户存档不存在，配置为默认存档");
                    cosPath = $"default/{saveName}.zip";
                }
                else
                {
                    //不存在存档，建立目录并监控
                    DirectoryInfo di = new DirectoryInfo(realSavePath); //没有文件夹的话应该先创建文件夹
                    if (!di.Exists)
                    {
                        di.Create();
                    }
                    Game game = new Game() { GameName = saveName, SavePath = savePath };
                    GameSaveWatcher gsw = new GameSaveWatcher(game);
                    gsws.Add(gsw);
                    return true;
                }
                
            }
            string localDir = $"{AppDomain.CurrentDomain.BaseDirectory}GameSave\\";
            string zipFileName = localDir + $"{saveName}.zip";
            //if (File.Exists(zipFileName)) File.Delete(zipFileName);
            try
            {
                CosRes res = await Cos.Ins.Download(cosPath, localDir, $"{saveName}.zip");
                if (res.IsSucceed)
                {
                    logger.Debug("下载存档成功"); 
                    if (!File.Exists(zipFileName))
                    {
                        logger.Debug("存档文件不存在？:" + zipFileName);
                        return false;
                    }
                    //解压缩出错是否是因为下载后磁盘文件未及时更新？
                    ZipFile zip = new ZipFile(zipFileName, System.Text.Encoding.Default);
                    zip.ExtractAll(realSavePath, ExtractExistingFileAction.OverwriteSilently);
                    zip.Dispose();
                    saveKeys.Remove(cosPath);
                    logger.Debug("解压存档成功:" + zipFileName);
                    //添加监控器
                    Game game = new Game() { GameName = saveName, SavePath = savePath };
                    GameSaveWatcher gsw = new GameSaveWatcher(game);
                    gsws.Add(gsw);
                }
                else
                {
                    logger.Error("读取云存档失败:" + res.Msg);
                    return false;
                }
            }
            catch(Exception ex)
            {
                logger.Error("读取云存档出错:" + ex.ToString());
                return false;
            }
            finally
            {
                if (File.Exists(zipFileName)) File.Delete(zipFileName);
            }
            return true;
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

        // 也可以认为是初始化函数
        public static void GetCloudSaveList(int userid)
        {
            // "1/"
            UserID = userid;
            // 1/BloodstainedRotN.zip
            saveKeys = Cos.Ins.List($"{userid}/").ToDictionary(x=>x, x => x);
            saveKeys.Remove(@"/");

            defaultSaveKeys = Cos.Ins.List($"default/").ToDictionary(x => x, x => x);
            defaultSaveKeys.Remove(@"/");
        }
    }
}
