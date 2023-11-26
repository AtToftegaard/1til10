using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Azure;
using Azure.Core.Serialization;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EtTilTi.Shared;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace csharp_isolated;

public class Functions
{
    [Function("negotiate")]
    public static HttpResponseData Negotiate([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [SignalRConnectionInfoInput(HubName = "serverless")] string connectionInfo)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        response.WriteString(connectionInfo);
        return response;
    }

    [Function(nameof(SaveSession))]
    [SignalROutput(HubName = "serverless")]
    public async Task<SignalRMessageAction> SaveSession([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sessions/{session}")] HttpRequestData req)
    {
            var session = await req.ReadFromJsonAsync<Session>();
            var blob = Container.GetBlobClient($"{session?.SessionName}.txt");
            var exists = blob.Exists();
            await blob.UploadAsync(BinaryData.FromObjectAsJson(session), overwrite: true);
            if (exists)
                return new SignalRMessageAction(SignalrMethods.SessionUpdated, new object[] { session });
            else
                return new SignalRMessageAction(SignalrMethods.SessionCreated, new object[] { session });
    }

    [Function(nameof(GetSession))]
    public Session GetSession([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sessions/{session}")] HttpRequestData req,
        string session)
    {
        var download = Container.GetBlobClient(session).DownloadContent();
        var content = download.Value.Content.ToString();
        return JsonSerializer.Deserialize<Session>(content);
    }

    [Function(nameof(GetSessions))]
    public async Task<HttpResponseData> GetSessions([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sessions")] HttpRequestData req)
    {
        var resultSegment = Container.GetBlobsAsync().AsPages(default, 10);
        var results = new List<Session>();
        await foreach (Page<BlobItem> blobPage in resultSegment)
        {
            foreach (BlobItem blobItem in blobPage.Values)
            {
                var download = Container.GetBlobClient(blobItem.Name).DownloadContent();
                var content = download.Value.Content.ToString();
                results.Add(JsonSerializer.Deserialize<Session>(content));
            }
        }
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(results, new JsonObjectSerializer(WithoutGuessValue));
        return response;
    }

    [Function(nameof(DeleteSessions))]
    public async Task DeleteSessions([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "sessions")] HttpRequestData req)
    {
        var resultSegment = Container.GetBlobsAsync().AsPages(default, 10);
        await foreach (Page<BlobItem> blobPage in resultSegment)
        {
            foreach (BlobItem blobItem in blobPage.Values)
            {
                await Container.GetBlobClient(blobItem.Name).DeleteAsync();
            }
        }
    }

    private static BlobContainerClient Container =>
        new(Environment.GetEnvironmentVariable("StorageConnection"), Environment.GetEnvironmentVariable("SessionContainerName"));

    static JsonSerializerOptions WithoutGuessValue => new JsonSerializerOptions
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { IgnoresGuessProperty }
        }
    };

    static void IgnoresGuessProperty(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type != typeof(Session))
            return;
        foreach (JsonPropertyInfo propertyInfo in typeInfo.Properties)
        {
            if (propertyInfo.Name == nameof(Session.CreatorGuessValue) || propertyInfo.Name == nameof(Session.PlayerGuessValue))
                propertyInfo.ShouldSerialize = (obj, value) => ((Session)obj).HasGuess;
        }
    }
}