using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.ProjectOxford.Face;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Miriot.Common.Request;

namespace Miriot.WebJob
{
    public class Functions
    {
        private const string OxfordKey = "76cad5e7644346669f094fa0315de735";
        private const string XebienzaPersonGroup = "usrxebienza";

        static Functions()
        {
            var cloudAccount =
                CloudStorageAccount.Parse(
                    ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString);
            var blobClient = cloudAccount.CreateCloudBlobClient();
            var logTable = cloudAccount.CreateCloudTableClient().GetTableReference("logs");
            logTable.CreateIfNotExists();
            var blobContainer =
                blobClient.GetContainerReference(ConfigurationManager.AppSettings["UsersImagesContainerName"]);
            blobContainer.CreateIfNotExists();

        }

        public static async Task ProcessQueueMessage([QueueTrigger("users")] UserFaceUpdateRequest request,
            TextWriter log)
        {
            var cloudAccount =
               CloudStorageAccount.Parse(
                   ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString);
            var blobClient = cloudAccount.CreateCloudBlobClient();
            var logTable = cloudAccount.CreateCloudTableClient().GetTableReference("logs");
            var blobContainer =
                blobClient.GetContainerReference(ConfigurationManager.AppSettings["UsersImagesContainerName"]);

            try
            {
                var blob = blobContainer.GetBlockBlobReference(request.Image);

                var faceClient = new FaceServiceClient(OxfordKey);

                using (var stream = await blob.OpenReadAsync())
                    await faceClient.AddPersonFaceAsync(XebienzaPersonGroup, request.Id, stream);

                await faceClient.TrainPersonGroupAsync(XebienzaPersonGroup);

                await LogAsunc(logTable, "Message processed !", Level.Info);
            }
            catch (Exception e)
            {
                await LogAsunc(logTable, "Error while processing message", Level.Error, e);
            }
        }

        private static async Task LogAsunc(CloudTable logTable, string message, Level level, Exception exception = null)
        {
            var logEntry = new LogEntity(message, level, exception);
            var insertOperation = TableOperation.Insert(logEntry);
            await logTable.ExecuteAsync(insertOperation);
        }

        /// <summary>
        /// This function will be invoked when a message ends up in the poison queue
        /// </summary>
        public static void BindToPoisonQueue([QueueTrigger("badqueue-poison")] string message, TextWriter log)
        {
            log.Write("This message couldn't be processed by the original function: " + message);
        }
    }

    internal class LogEntity : TableEntity
    {
        public LogEntity(string message, Level level, Exception exception)
        {
            PartitionKey = level.ToString();
            RowKey = DateTime.UtcNow.ToString("yyyyMMddhhmmssfff");
            Message = message;
            Exception = exception?.ToString();
        }

        public string Exception { get; set; }

        public string Message { get; set; }

        public LogEntity() { }

    }

    internal enum Level
    {
        Info,
        Warning,
        Error
    }
}
