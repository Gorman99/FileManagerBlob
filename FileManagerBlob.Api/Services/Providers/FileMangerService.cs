using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using FileManagerBlob.Api.Models;
using FileManagerBlob.Api.Services.Interfaces;

namespace FileManagerBlob.Api.Services.Providers;

public class FileMangerService : IFileMangerService
{
    private readonly BlobServiceClient _blobService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileMangerService> _logger;
    private readonly string _containerName;

    public FileMangerService(BlobServiceClient blobService, IConfiguration configuration,
        ILogger<FileMangerService> logger)
    {
        _blobService = blobService;
        _configuration = configuration;
        _logger = logger;

        _containerName = _configuration.GetValue<string>("BlobContainerName");
    }

    public async Task<BlobResponseDto> UploadAsync(IFormFile blob)
    {
        BlobResponseDto response = new();
        var container = _blobService.GetBlobContainerClient(_containerName);
      //  await container.CreateIfNotExistsAsync();
        try
        {
            BlobClient blobClient = container.GetBlobClient(blob.FileName);

            await using (Stream? data = blob.OpenReadStream())
            {
                await blobClient.UploadAsync(data, new BlobHttpHeaders{ ContentType = blob.ContentType});
            }
        }
        catch (RequestFailedException ex)
            when (ex.ErrorCode == BlobErrorCode.BlobAlreadyExists)
        {
            _logger.LogError($"File with name {blob.FileName} already exists in container. Set another name to store the file in the container: '{_containerName}.'");
            response.Status = $"File with name {blob.FileName} already exists. Please use another name to store your file.";
            response.Error = true;
            return response;
        } 
        // If we get an unexpected error, we catch it here and return the error message
        catch (RequestFailedException ex)
        {
            // Log error to console and create a new response we can return to the requesting method
            _logger.LogError($"Unhandled Exception. ID: {ex.StackTrace} - Message: {ex.Message}");
            response.Status = $"Unexpected error: {ex.StackTrace}. Check log with StackTrace ID.";
            response.Error = true;
            return response;
        }

        return response;
    }

    public async Task<BlobDownloadDto> DownloadAsync(string blobFilename)
    {
        
        var  blobContainer = _blobService.GetBlobContainerClient(_containerName);

        try
        {
            // Get a reference to the blob uploaded earlier from the API in the container from configuration settings
            BlobClient file = blobContainer.GetBlobClient(blobFilename);

            // Check if the file exists in the container
            if (await file.ExistsAsync())
            {
                var data = await file.OpenReadAsync();
                Stream blobContent = data;

                // Download the file details async
                var content = await file.DownloadContentAsync();

                // Add data to variables in order to return a BlobDto
                string name = blobFilename;
                string contentType = content.Value.Details.ContentType;

                // Create new BlobDto with blob data from variables
                return new BlobDownloadDto { Content = blobContent, Name = name, ContentType = contentType };
            }
        }
        catch (RequestFailedException ex)
            when(ex.ErrorCode == BlobErrorCode.BlobNotFound)
        {
            // Log error to console
            _logger.LogError($"File {blobFilename} was not found.");
        }

        // File does not exist, return null and handle that in requesting method
        return null;
    }

    public async Task<BlobResponseDto> DeleteAsync(string blobFilename)
    {
        var blobContainer = _blobService.GetBlobContainerClient(_containerName);
        try
        {
            var file =  blobContainer.GetBlobClient(blobFilename);

            await file.DeleteAsync();
        }
        catch (RequestFailedException ex)
            when (ex.ErrorCode == BlobErrorCode.BlobNotFound)
        {
            // File did not exist, log to console and return new response to requesting method
            _logger.LogError($"File {blobFilename} was not found.");
            return new BlobResponseDto { Error = true, Status = $"File with name {blobFilename} not found." };
        }

        // Return a new BlobResponseDto to the requesting method
        return new BlobResponseDto { Error = false, Status = $"File: {blobFilename} has been successfully deleted." };
    }

    public async Task<List<BlobDto>> ListAsync()
    {
        var client = _blobService.GetBlobContainerClient(_containerName);
        List<BlobDto> files = new List<BlobDto>();

        await foreach (BlobItem file in client.GetBlobsAsync())
        {
            string uri = client.Uri.ToString();
            var name = file.Name;
            var fullUri = $"{uri}/{name}";

            files.Add(new BlobDto
            {
                Uri = fullUri,
                Name = name,
                ContentType = file.Properties.ContentType
            });
        }

        return files;
    }
}