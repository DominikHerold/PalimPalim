using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

using HtmlAgilityPack;

namespace PalimPalim
{
    public class Program
    {
        private static Timer Timer;

        private const string Cookie = "foo"; // ToDo change
        private const string IncomesUrl = "/users/42/basic_incomes"; // ToDo change
        private const string TransfersUrl = "/users/42/transfers"; // ToDo change
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

        private static void SendToPushoverApi(string balance)
        {
            var client = new HttpClient();
            var toSend = string.Format($"{PushoverKey} {balance}&title=PalimPalim&url=https://palai.org{IncomesUrl}");
            var now = DateTime.Now;
            if (now.Hour >= 22 || now.Hour < 7)
                toSend = string.Format("{0}&sound=none", toSend);

            client.PostAsync("https://api.pushover.net/1/messages.json", new StringContent(toSend, Encoding.UTF8, "application/x-www-form-urlencoded")).GetAwaiter().GetResult();
        }

        private static void CheckPalai()
        {
            var content = DownloadString($"https://palai.org{IncomesUrl}");
            if (content.Contains($@"<form class=""button_to"" method=""post"" action=""{IncomesUrl}"">"))
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(content);
                var formNode = htmlDoc.DocumentNode.SelectNodes($"//form[@action='{IncomesUrl}']").Single();
                var authNode = formNode.SelectSingleNode(".//input[@name='authenticity_token']");
                var authToken = authNode.GetAttributeValue("value", "notset");
                authToken = WebUtility.UrlEncode(authToken);

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add(nameof(Cookie), Cookie);
                var toSend = $"authenticity_token={authToken}";
                client.PostAsync($"https://palai.org{IncomesUrl}", new StringContent(toSend, Encoding.UTF8, "application/x-www-form-urlencoded")).GetAwaiter().GetResult();

                var transferData = DownloadString($"https://palai.org{TransfersUrl}");
                htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(transferData);
                var balanceNode = htmlDoc.DocumentNode.SelectNodes("//td[@class='current-balance autohide']").Single();
                var balance = balanceNode.InnerText.Trim();

                SendToPushoverApi(balance);
            }
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