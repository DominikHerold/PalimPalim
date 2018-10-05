using System;
using System.Net.Http;
using System.Text;

namespace PalimPalim
{
    public class Program
    {
        private static Timer Timer;

        private const string Cookie = "foo"; // ToDo change
        private const string IncomesUrl = "/users/42/basic_incomes"; // ToDo change
        private const string PushoverKey = "token=foo&user=bar&message=PalimPalim"; // ToDo change

        private static void Main()
        {
            Console.WriteLine("Start");

            Timer = new Timer((int)TimeSpan.FromHours(5).TotalMilliseconds, false);
            Timer.Elapsed += CheckPalai;

            CheckPalai(null, null);

            Console.ReadLine();
            Timer.Dispose();
        }

        private static string DownloadString(string address)
        {
            using (var webClient = new HttpClient())
            {
                webClient.DefaultRequestHeaders.Add(nameof(Cookie), Cookie);
                var data = webClient.GetAsync(address).GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
                return data;
            }
        }

        private static void SendToPushoverApi()
        {
            var client = new HttpClient();
            var toSend = string.Format($"{PushoverKey}&title=PalimPalim&url=https://palai.org{IncomesUrl}");
            var now = DateTime.Now;
            if (now.Hour >= 22 || now.Hour < 7)
                toSend = string.Format("{0}&sound=none", toSend);

            client.PostAsync("https://api.pushover.net/1/messages.json", new StringContent(toSend, Encoding.UTF8, "application/x-www-form-urlencoded")).GetAwaiter().GetResult();
        }

        private static void CheckPalai()
        {
            var content = DownloadString($"https://palai.org{IncomesUrl}");
            if (content.Contains($@"<form class=""button_to"" method=""post"" action=""{IncomesUrl}"">"))
                SendToPushoverApi();
        }

        private static void CheckPalai(object sender, EventArgs e)
        {
            try
            {
                CheckPalai();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Timer.Start();
            }
        }
    }
}