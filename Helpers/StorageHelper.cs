using InsuranceClientPortal.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InsuranceClientPortal.Helpers
{
    //Contains methods for adding customer data to table and image to blob storage.
    public class StorageHelper
    {
        private CloudStorageAccount storageAccount;
        private CloudBlobClient blobClient;
        private CloudTableClient tableClient;
        private CloudQueueClient queueClient;

        public string StorageConnectionString
        {
            set
            {
                this.storageAccount = CloudStorageAccount.Parse(value);
                this.blobClient = storageAccount.CreateCloudBlobClient();
                //this.tableClient = storageAccount.CreateCloudTableClient(); //Created separate property for cosmos migration
                this.queueClient = storageAccount.CreateCloudQueueClient();
            }
        }

        public string TableConnectionString
        {
            set
            {
                var cosmosAcc = CloudStorageAccount.Parse(value);
                this.tableClient = cosmosAcc.CreateCloudTableClient();
            }
        }

        private async Task<CloudBlobContainer> CreateContainerIfNotExistsAsync(string containerName)
        {
            var container = blobClient.GetContainerReference(containerName);
            BlobContainerPermissions permissions = new BlobContainerPermissions(){
                PublicAccess = BlobContainerPublicAccessType.Blob
            };
            await container.SetPermissionsAsync(permissions);

            await container.CreateIfNotExistsAsync();
            return container;
        }

        public async Task<string> UploadFileAsync(string imagePath, string containerName)
        {
            var fileName = Path.GetFileName(imagePath);
            var container = await CreateContainerIfNotExistsAsync(containerName);
            var blob = container.GetBlockBlobReference(fileName);
            await blob.UploadFromFileAsync(imagePath);
            return blob.Uri.AbsoluteUri;
        }

        private async Task<CloudTable> CreateTableIfNotExistsAsync(string tableName)
        {
            var table = tableClient.GetTableReference(tableName);
            await table.CreateIfNotExistsAsync();
            return table;
        }

        public async Task<Customer> SaveInsuranceDetailAsync(Customer customer, string tableName)
        {
            var table = await CreateTableIfNotExistsAsync(tableName);
            TableOperation tableOperation = TableOperation.InsertOrMerge(customer);
            TableResult entity = await table.ExecuteAsync(tableOperation);
            return entity.Result as Customer;
        }

        private async Task<CloudQueue> CreateQueueIfNotExistsAsync(string queueName)
        {
            var queue = queueClient.GetQueueReference(queueName);
            await queue.CreateIfNotExistsAsync();
            return queue;
        }

        public async Task<bool> SendMessageAsync(string messageText, string queueName)
        {
            var queue = await CreateQueueIfNotExistsAsync(queueName);

            CloudQueueMessage message = new CloudQueueMessage(messageText);
            await queue.AddMessageAsync(message, TimeSpan.FromMinutes(30),TimeSpan.Zero,null,null);
            return true;
        }
    }
}
