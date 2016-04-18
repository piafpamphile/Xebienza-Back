using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Miriot.Common.Model;
using Miriot.Common.Request;
using Newtonsoft.Json;

namespace Miriot.Web.Tools
{
    public class Storage
    {
        private readonly CloudTableClient _tableClient;
        private readonly CloudQueueClient _queueClient;
        private readonly CloudBlobClient _blobClient;

        public Storage()
        {
            var storageAccount = CloudStorageAccount.Parse(StorageAccount.ConnectionString);
            _tableClient = storageAccount.CreateCloudTableClient();
            _queueClient = storageAccount.CreateCloudQueueClient();
            _blobClient = storageAccount.CreateCloudBlobClient();
        }

        public List<User> GetUsersFromTableStorage()
        {
            var table = _tableClient.GetTableReference(StorageAccount.UsersTableName);
            var users = table.ExecuteQuery(new TableQuery<UserEntity>());

            return users.Select(o => new User
            {
                Name = o.Name,
                Id = Guid.Parse(o.RowKey),
                Widgets = JsonConvert.DeserializeObject<List<Widget>>(o.Widgets)
            }).ToList();
        }

        public async Task<User> GetUserFromTableStorageAsync(Guid personId)
        {
            var table = _tableClient.GetTableReference(StorageAccount.UsersTableName);
            var userOperation = TableOperation.Retrieve<UserEntity>("User", personId.ToString());
            var userOperationResult = (await table.ExecuteAsync(userOperation)).Result as UserEntity;
            
            if (userOperationResult != null)
            {
                return new User
                {
                    Id = Guid.Parse(userOperationResult.RowKey),
                    Name = userOperationResult.Name,
                    Widgets = JsonConvert.DeserializeObject<List<Widget>>(userOperationResult.Widgets)
                };
            }

            return null;
        }

        public async Task CreateOrUpdateUserInTableStorageAsync(Guid personId, string name, string widgets)
        {
            var table = _tableClient.GetTableReference(StorageAccount.UsersTableName);
            var userOperation = TableOperation.Retrieve<UserEntity>("User", personId.ToString());
            var userOperationResult = (await table.ExecuteAsync(userOperation)).Result as UserEntity;

            if (userOperationResult != null)
            {
                userOperationResult.Name = name;
                userOperationResult.Widgets = widgets ?? userOperationResult.Widgets;

                var insertOrReplaceOperation = TableOperation.InsertOrReplace(userOperationResult);
                await table.ExecuteAsync(insertOrReplaceOperation);
            }
            else
            {
                var setting = new UserEntity(personId, name, string.Empty);
                var insertOperation = TableOperation.Insert(setting);
                await table.ExecuteAsync(insertOperation);
            }
        }

        public async Task AddPersonFaceAsync(Guid id, byte[] image)
        {
            var container = _blobClient.GetContainerReference(StorageAccount.UsersImagesContainerName);
            var blob = container.GetBlockBlobReference($"{DateTime.UtcNow.ToString("yyMMddhhmmss")}_{id}.jpg");
            using (var stream = new MemoryStream(image))
                await blob.UploadFromStreamAsync(stream);

            var request = new UserFaceUpdateRequest
            {
                Id = id,
                Image = blob.Name
            };

            var queue = _queueClient.GetQueueReference(StorageAccount.UsersQueueName);
            await queue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(request)));
        }

        public async Task LogAsunc(string message, Level level, Exception exception = null)
        {
            var logTable = _tableClient.GetTableReference(StorageAccount.LogsTableName);
            var logEntry = new LogEntity(message, level, exception);
            var insertOperation = TableOperation.Insert(logEntry);
            await logTable.ExecuteAsync(insertOperation);
        }
    }
}
