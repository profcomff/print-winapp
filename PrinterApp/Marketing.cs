using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace PrinterApp
{
    public static class Marketing
    {
#if DEBUG
        private static readonly HttpClient SharedClient = new HttpClient
        {
            BaseAddress = new Uri("https://marketing.api.test.profcomff.com/v1/"),
        };
#else
        private static readonly HttpClient SharedClient = new HttpClient
        {
            BaseAddress = new Uri("https://marketing.api.profcomff.com/v1/"),
        };
#endif

        private static async Task Post(string action, string status, string pathFrom,
            string pathTo)
        {
            var body = new MarketingBody(action: action,
                additional_data: $"{{\"status\": \"{status}\"}}",
                path_from: pathFrom, path_to: pathTo);
            await SharedClient.PostAsJsonAsync("action", body);
        }

        public static async Task StartDownload(string pathFrom,
            string pathTo)
        {
            await Post(
                action: "print terminal start download file",
                status: "start_download",
                pathFrom: pathFrom,
                pathTo: pathTo);
        }

        public static async Task DownloadException(string pathFrom,
            string status)
        {
            await Post(
                action: "print terminal download exception",
                status: status,
                pathFrom: pathFrom,
                pathTo: "");
        }

        public static async Task FinishDownload(string pathFrom,
            string pathTo)
        {
            await Post(
                action: "print terminal finish download file",
                status: "finish_download",
                pathFrom: pathFrom,
                pathTo: pathTo);
        }

        public static async Task PrintException(string pathFrom,
            string status)
        {
            await Post(
                action: "print terminal print exception",
                status: status,
                pathFrom: pathFrom,
                pathTo: "");
        }

        public static async Task PrintNotFile(string pathFrom)
        {
            await Post(
                action: "print terminal check filename",
                status: "not_file",
                pathFrom: pathFrom,
                pathTo: "");
        }

        public static async Task CheckCode(string pathFrom,
            bool statusOk)
        {
            await Post(
                action: "print terminal check code",
                status: $"{(statusOk ? "check_code_ok" : "check_code_fail")}",
                pathFrom: pathFrom,
                pathTo: "");
        }

        public static async Task StartSumatra(string pathFrom)
        {
            await Post(
                action: "print terminal start sumatra",
                status: "start_sumatra",
                pathFrom: pathFrom,
                pathTo: "");
        }
    }
}