using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Timers;
using Playnite.SDK;
using System.Windows;

namespace Playnite
{
    public class UserManager
    {
        public static Station station;
        public static User user;
        public static MyWebApiClient apiClient = new MyWebApiClient();
        public static bool isFake = false;
        static Timer timer;
        private static ILogger logger = LogManager.GetLogger();
        private static PlayniteApplication playniteApplication;

        public static void FakeInit(int stationid,int userid)
        {
            station = new Station(stationid);
            user = new User(userid) { UserName = "admin" };
            isFake = true;
        }

        public static async Task<bool> Init(int stationID, PlayniteApplication _playniteApplication)
        {
            playniteApplication = _playniteApplication;
            Station s = await apiClient.UpdateAsync(stationID);
            if (s != null)
            {
                station = s;
                //如果工作站用户为-1 则采用1 = qjzh984
                int userid = s.ClientUserId == -1 ? 1 : s.ClientUserId;
                user = new User(userid);
                //初始化完成后，开始更新工作站状态
                timer = new System.Timers.Timer(1000 * 5); //暂时设置5秒
                timer.Elapsed += Timer_Elapsed;
                timer.Start();
                return true;
            }
            return false;
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            CheckStationInfo();
        }

        public static StationState lastState = StationState.Usable;
        /// <summary>
        ///  创建进程检查是否需要强行退出
        /// </summary>
        /// <param name="stationID"></param>
        public static async void CheckStationInfo()
        {
            Station s = await apiClient.UpdateAsync(station.StationID);
            if (s != null)
            {
                switch(s.State)
                {
                    case StationState.Gaming:
                        return;
                    case StationState.Saving://保存并退出
                        logger.Info("StationState.Saving: 保存并退出");
                        await SaveManager.CheckSaveUpdate();
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            playniteApplication.Quit();
                        }));
                        return;
                }
            }
            else
            {
                //更新状态失败？
                logger.Info("获取工作站状态失败");
            }
        }

    }

    public class MyWebApiClient
    {
        HttpClient client = new HttpClient()
        {
            BaseAddress = new Uri(@"http://1.15.225.130:5000/api/")
        };

        public MyWebApiClient()
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Connection.Add("keep-alive");
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = new TimeSpan(0, 0, 10);//10秒超时
        }

        public async Task<Station> UpdateAsync(int stationID)
        {
            Station s = null;
            HttpResponseMessage response = await client.GetAsync($"Station/Update?id={stationID}");
            if (response.IsSuccessStatusCode)
            {
                string res = await response.Content.ReadAsStringAsync();
                s = JsonConvert.DeserializeObject<Station>(res);
            }
            return s;
        }
    }

}
