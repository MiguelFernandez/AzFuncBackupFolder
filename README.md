# AzFuncBackupFolder

 ## Description
 
 Function app that zips up the wwwroot folder for a function app and saves the zip file to an Azure storage acount blob container
## Application Settings


Need to define these settings for function app in **Azure Portal > Configuration**
  -  StorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=someaccountname;AccountKey=somelongaccountkey==;EndpointSuffix=core.windows.net"
  -  AppServicePublishProfileUserName = "$username" (this is found in the publish profile for the app service)
  -  AppServicePublishProfilePassword = "password" (this is found in the publish profile for the app service)
  -  StorageBlobContanierName = "wwwrootbackup"
  -  AppServiceName =  "name of your app service"  as in <AppServiceName>.azurewebsites.net
    
