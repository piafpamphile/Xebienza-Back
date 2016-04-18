using System;
using System.Configuration;
using Microsoft.Azure.WebJobs;

namespace Miriot.WebJob
{
    class Program
    {
        static void Main()
        {
            if (!VerifyConfiguration())
            {
                Console.ReadLine();
                return;
            }

            var host = new JobHost();
            host.RunAndBlock();
        }

        private static bool VerifyConfiguration()
        {
            var webJobsDashboard = ConfigurationManager.ConnectionStrings["AzureWebJobsDashboard"].ConnectionString;
            var webJobsStorage = ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString;

            var configOk = true;
            if (string.IsNullOrWhiteSpace(webJobsDashboard) || string.IsNullOrWhiteSpace(webJobsStorage))
            {
                configOk = false;
                Console.WriteLine("Please add the Azure Storage account credentials in App.config");

            }

            return configOk;
        }
    }
}
