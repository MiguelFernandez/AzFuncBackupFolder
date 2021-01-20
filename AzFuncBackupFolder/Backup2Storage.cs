using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;


namespace AzFuncBackupFolder
{

    public static class Backup2Storage
    {
        private static long _uploadFileSize;

        [FunctionName("Backup")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var appServiceName = Environment.GetEnvironmentVariable("AppServiceName");
            log.LogInformation("C# HTTP trigger function processed a request.");
            await DownloadWWWRootAsZipAndUploadToStorage(appServiceName, log);
            return new OkObjectResult("Function completed");
        }

        public static async Task DownloadWWWRootAsZipAndUploadToStorage(string appServiceName, ILogger log)
        {
            log.LogInformation($"Starting wwwroot zip download for {appServiceName}");
            try
            {

                var url = $"https://{appServiceName}.scm.azurewebsites.net/api/zip/site/wwwroot/";
                var uri = new Uri(url);
                using (WebClient wc = new WebClient())
                {
                    var username = Environment.GetEnvironmentVariable("AppServicePublishProfileUserName");
                    var password = Environment.GetEnvironmentVariable("AppServicePublishProfilePassword");
                    string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
                    wc.Headers[HttpRequestHeader.Authorization] = string.Format(
                        "Basic {0}", credentials);

                    await wc.DownloadFileTaskAsync(uri, $"d:\\home\\wwwroot-backup.zip");
                    log.LogInformation($"Finished downloading file to d:\\home drive");
                    await UploadFileToAzStorage(log);
                }

            }
            catch (Exception ex)
            {

                var errorJson = Newtonsoft.Json.JsonConvert.SerializeObject(ex, Formatting.Indented, new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                log.LogError(errorJson);

                throw;
            }

        }

        private static async Task UploadFileToAzStorage(ILogger log)
        {
            try
            {
                var connectionString = Environment.GetEnvironmentVariable("StorageConnectionString");
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                var storageBlobContainerName = Environment.GetEnvironmentVariable("StorageBlobContanierName");
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(storageBlobContainerName);

                string localFilePath = $"d:\\home\\wwwroot-backup.zip";

                BlobClient blobClient = containerClient.GetBlobClient($"www-backup{DateTime.Now.ToLongDateString()}.zip");
                log.LogInformation($"Uploading to {blobClient.Uri}");


                using FileStream uploadFileStream = File.OpenRead(localFilePath);
                _uploadFileSize = uploadFileStream.Length;

                var progressHandler = new Progress<long>();
                BlobUploadOptions blobUploadOptions = new BlobUploadOptions() { ProgressHandler = progressHandler };
                progressHandler.ProgressChanged += (s, l) => UploadProgressChanged(s, l, log);

                log.LogInformation("Starting upload to storage account");
                await blobClient.UploadAsync(uploadFileStream, blobUploadOptions);
                log.LogInformation("Upload completed. Closing upload file stream");
                uploadFileStream.Close();
                log.LogInformation("Upload filestream closed");
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                log.LogError(ex.StackTrace);
                throw;
            }


        }

        private static string GetProgressPercentage(double totalSize, double currentSize)
        {
            return Math.Round((currentSize / totalSize) * 100).ToString();
        }

        private static void UploadProgressChanged(object sender, long bytesUploaded, ILogger log)
        {
            //https://www.craftedforeveryone.com/upload-or-download-file-from-azure-blob-storage-with-progress-percentage-csharp/
            var progressPercentage = GetProgressPercentage(_uploadFileSize, bytesUploaded);
            log.LogInformation($"{progressPercentage}% uploaded");
        }
    }
}
