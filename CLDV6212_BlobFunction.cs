using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;
using System;
using Azure.Storage.Blobs.Models;
using Azure;

/*
 ================================================================
// Code Attribution

Author: Mick Gouweloos
Link: https://github.com/mickymouse777/Cloud_Storage
Date Accessed: 20 August 2024

Author: Mick Gouweloos
Link: https://github.com/mickymouse777/SimpleSample.git
Date Accessed: 20 September 2024

Author: W3schools
Link: https://www.w3schools.com/colors/colors_picker.asp
Date Accessed: 21 August 2024

Author: W3schools
Link: https://www.w3schools.com/css/css_font.asp 
Date Accessed: 21 August 2024

 *********All Images used throughout project are adapted from https://bangtanpictures.net/index.php and https://shop.weverse.io/en/home*************

 ================================================================
!--All PAGES are edited but layout depicted from Tooplate Template-
(https://www.tooplate.com/) 

 */
namespace CLDV6212_BlobFunction
{
    public class CLDV6212_BlobFunction
    {
        // Private BlobServiceClient to interact with Azure Blob Storage.
        private readonly BlobServiceClient _blobServiceClient;

        // Constructor initializes the BlobServiceClient using the connection string stored in environment variables.
        public CLDV6212_BlobFunction()
        {
            // Retrieves the connection string from environment variables (configured in Azure).
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            // Initializes the BlobServiceClient to connect to Azure Blob Storage using the connection string.
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        // Function that handles HTTP POST requests to upload files to Azure Blob Storage.
        [Function("CLDV6212_BlobFunction")]
        public async Task<HttpResponseData> Run(
            // The function is triggered by an HTTP POST request.
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            // The function context provides logging and execution metadata.
            FunctionContext executionContext)
        {
            // Logger to log information about the function's execution.
            var logger = executionContext.GetLogger("UploadToBlob");
            logger.LogInformation("Uploading to Blob Storage...");

            try
            {
                // Gets a reference to the blob container client named "products".
                var containerClient = _blobServiceClient.GetBlobContainerClient("products");

                // Creates the container if it doesn't exist. Useful for ensuring the container is available.
                await containerClient.CreateIfNotExistsAsync();

                // Retrieves the original file name from the request headers.
                if (!req.Headers.TryGetValues("file-name", out var fileNameValues))
                {
                    // Throws an exception if the file name is missing from the request headers.
                    throw new Exception("File name is missing in the request headers.");
                }

                // Gets the first file name from the list of header values.
                string originalFileName = fileNameValues.FirstOrDefault();
                if (string.IsNullOrEmpty(originalFileName))
                {
                    // Throws an exception if the file name is invalid or empty.
                    throw new Exception("Invalid file name.");
                }

                // Retrieves a BlobClient for the specific blob (file) in the "products" container using the file name.
                var blobClient = containerClient.GetBlobClient(originalFileName);

                // Uploads the file (request body stream) to Azure Blob Storage, overwriting any existing blob with the same name.
                using (var stream = req.Body)
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                // Creates a success response indicating the file was uploaded successfully.
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync($"Blob '{originalFileName}' uploaded successfully.");
                return response;
            }
            catch (Exception ex)
            {
                // Logs the error message if an exception occurs during the upload process.
                logger.LogError($"Error uploading to Blob Storage: {ex.Message}");

                // Returns an error response with a 500 Internal Server Error status.
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Failed to upload blob.");
                return errorResponse;
            }
        }

        // Function that handles HTTP DELETE requests to remove a blob (file) from Azure Blob Storage.
        [Function("DeleteBlob")]
        public async Task<HttpResponseData> DeleteBlobAsync(
            // The function is triggered by an HTTP DELETE request.
            [HttpTrigger(AuthorizationLevel.Function, "delete")] HttpRequestData req,
            // The function context provides logging and execution metadata.
            FunctionContext executionContext)
        {
            // Logger to log information about the function's execution.
            var logger = executionContext.GetLogger("DeleteBlob");
            logger.LogInformation("Deleting from Blob Storage...");

            try
            {
                // Parses the query string of the HTTP request to extract the blob URI (the location of the blob to delete).
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                string blobUri = query["blobUri"];

                // Throws an exception if the blob URI is missing from the query string.
                if (string.IsNullOrEmpty(blobUri))
                {
                    throw new Exception("Blob URI is missing from the query string.");
                }

                // Parses the blob URI and extracts the blob name from the last segment of the URI.
                Uri uri = new Uri(blobUri);
                string blobName = uri.Segments[^1];

                // Gets a reference to the blob container client for the "products" container.
                var containerClient = _blobServiceClient.GetBlobContainerClient("products");
                var blobClient = containerClient.GetBlobClient(blobName);

                // Deletes the blob from Azure Blob Storage, including any snapshots (versions) of the blob.
                await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);

                // Creates a success response indicating the blob was deleted successfully.
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync($"Blob '{blobName}' deleted successfully.");
                return response;
            }
            catch (Exception ex)
            {
                // Logs the error message if an exception occurs during the deletion process.
                logger.LogError($"Error deleting blob from Blob Storage: {ex.Message}");

                // Returns an error response with a 500 Internal Server Error status.
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Failed to delete blob.");
                return errorResponse;
            }
        }
    }
}
