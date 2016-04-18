using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Microsoft.WindowsAzure.Storage;
using Miriot.Web.Tools;

namespace Miriot.Web
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            InitializeBlobStorage();
        }

        private void Application_Error(object sender, EventArgs e)
        {
            // Code that runs when an unhandled error occurs
            // Give the user some information, but
            // stay on the default page
            var exc = Server.GetLastError();
            
            var storage = new Storage();
            Task.Run(() => storage.LogAsunc("Application error", Level.Error, exc));

            // Clear the error from the server
            Server.ClearError();
        }


        private void InitializeBlobStorage()
        {
            StorageAccount.ConnectionString =
                ConfigurationManager.ConnectionStrings["StorageConnection"].ConnectionString;
            var storageAccount = CloudStorageAccount.Parse(StorageAccount.ConnectionString);

            StorageAccount.UsersQueueName = ConfigurationManager.AppSettings["UsersQueueName"];
            StorageAccount.UsersTableName = ConfigurationManager.AppSettings["UsersTableName"];
            StorageAccount.LogsTableName = ConfigurationManager.AppSettings["LogsTableName"];
            StorageAccount.UsersImagesContainerName = ConfigurationManager.AppSettings["UsersImagesContainerName"];

            var queueClient = storageAccount.CreateCloudQueueClient();
            var tableClient = storageAccount.CreateCloudTableClient();

            var logsTable = tableClient.GetTableReference(StorageAccount.LogsTableName);
            logsTable.CreateIfNotExists();

            var usersQueue = queueClient.GetQueueReference(StorageAccount.UsersQueueName);
            usersQueue.CreateIfNotExists();

            var usersTable = tableClient.GetTableReference(StorageAccount.UsersTableName);
            usersTable.CreateIfNotExists();

            var blobClient = storageAccount.CreateCloudBlobClient();
            var usersImageContainer = blobClient.GetContainerReference(StorageAccount.UsersImagesContainerName);
            usersImageContainer.CreateIfNotExists();
        }
    }
}
