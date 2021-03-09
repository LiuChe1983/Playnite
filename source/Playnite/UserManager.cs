using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace Playnite
{
    public class UserManager
    {
        public static Station station;
        public static User user;
        public static MyWebApiClient apiClient = new MyWebApiClient();

        public static void FakeInit()
        {
            station = new Station(-1);
            user = new User(-1) { UserName = "admin" };
        }
        public static async Task<bool> Init(int stationID)
        {
            Station s = await apiClient.UpdateAsync(stationID);
            if (s != null)
            {
                station = s;
                user = new User(s.ClientUserId);
                return true;
            }
            return false;
        }

    }

    public class MyWebApiClient
    {
        HttpClient client = new HttpClient()
        {
            BaseAddress = new Uri(@"http://121.5.79.148:5000/api/")
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
