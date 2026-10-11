using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Game.Application;
using Google.Apis.Auth.OAuth2;
namespace Game.Api;

/// <summary>Firestore REST infrastructure adapter. A manifest CAS atomically publishes bounded record chunks.</summary>
public sealed class FirestoreSessionStore : ISessionStore, IDisposable
{
    private readonly HttpClient client=new(){Timeout=TimeSpan.FromSeconds(15)};
    private readonly string documents;
    private readonly string endpoint;
    private readonly bool emulator;
    public FirestoreSessionStore(string project,string? emulatorHost)
    {
        if(string.IsNullOrWhiteSpace(project)||project.Any(c=>!char.IsAsciiLetterOrDigit(c)&&c!='-'))throw new ArgumentException("Use a Firebase project ID.");
        emulator=!string.IsNullOrWhiteSpace(emulatorHost);
        documents=$"projects/{project}/databases/(default)/documents";
        endpoint=emulator?$"http://{emulatorHost}/v1/":"https://firestore.googleapis.com/v1/";
    }
    private JsonElement? Send(HttpMethod method,string path,object? body=null)
    {
        using var request=new HttpRequestMessage(method,endpoint+path);
        if(body is not null)request.Content=JsonContent.Create(body);
        if(emulator)request.Headers.Authorization=new("Bearer","owner");
        else
        {
            var credentials=GoogleCredential.GetApplicationDefault().CreateScoped("https://www.googleapis.com/auth/datastore");
            request.Headers.Authorization=new("Bearer",credentials.UnderlyingCredential.GetAccessTokenForRequestAsync().GetAwaiter().GetResult());
        }
        using var response=client.SendAsync(request).GetAwaiter().GetResult();
        if(response.StatusCode==HttpStatusCode.NotFound)return null;
        if(response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.PreconditionFailed)
            throw new SessionFault(SessionFailure.Conflict,"Saved session revision changed. Reconnect and retry your command.");
        if(response.StatusCode==HttpStatusCode.BadRequest)
        {
            var error=JsonSerializer.Deserialize<JsonElement>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            if(error.TryGetProperty("error",out var details)&&details.TryGetProperty("status",out var status)&&status.GetString() is "FAILED_PRECONDITION" or "ABORTED")
                throw new SessionFault(SessionFailure.Conflict,"Saved session revision changed. Reconnect and retry your command.");
        }
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
    }
    private static string Text(JsonElement fields,string name)=>fields.GetProperty(name).GetProperty("stringValue").GetString()!;
    public StoredSession? Read(string id)
    {
        var name=$"{documents}/sessions/{id}";
        for(var attempt=0;attempt<3;attempt++)
        {
            var manifest=Send(HttpMethod.Get,name);if(manifest is null)return null;
            var version=manifest.Value.GetProperty("updateTime").GetString()!;
            var fields=manifest.Value.GetProperty("fields");var generation=Text(fields,"generation");var count=int.Parse(Text(fields,"count"));
            if(count is <1 or >32)throw new InvalidDataException("Recovery manifest exceeds supported bounds.");
            var payload=new StringBuilder();var missing=false;
            for(var i=0;i<count;i++)
            {
                var chunk=Send(HttpMethod.Get,$"{name}/records/{generation}-{i}");
                if(chunk is null){missing=true;break;}
                payload.Append(Text(chunk.Value.GetProperty("fields"),"payload"));
            }
            var latest=Send(HttpMethod.Get,name);
            if(latest?.GetProperty("updateTime").GetString()!=version)continue;
            if(missing)throw new IOException("A committed recovery chunk is missing.");
            return new(version,payload.ToString());
        }
        throw new IOException("Recovery changed repeatedly. Reconnect and retry.");
    }    public IReadOnlyList<string> FindByJoinCode(string code)
    {
        var query=Send(HttpMethod.Post,$"{documents}:runQuery",new{structuredQuery=new{from=new[]{new{collectionId="sessions"}},where=new{fieldFilter=new{field=new{fieldPath="joinCode"},op="EQUAL",value=new{stringValue=code}}}}});
        return query!.Value.EnumerateArray().Where(p=>p.TryGetProperty("document",out _)).Select(p=>p.GetProperty("document").GetProperty("name").GetString()!.Split('/').Last()).ToArray();
    }
    public string Write(string id,string? expectedVersion,string payload,string? joinCode)
    {
        // UTF-16 slicing stays well below Firestore's 1MiB limit even for multi-byte Unicode.
        const int chunkCharacters=160_000;
        if(Encoding.UTF8.GetByteCount(payload)>4_000_000)throw new IOException("Recovery exceeds the supported 4MB race limit. Preserve the current saved race for diagnostics.");
        var count=(payload.Length+chunkCharacters-1)/chunkCharacters;var generation=Guid.NewGuid().ToString("N");
        var name=$"{documents}/sessions/{id}";
        var writes=new List<object>();
        for(var i=0;i<count;i++)writes.Add(new{update=new{name=$"{name}/records/{generation}-{i}",fields=new{payload=new{stringValue=payload.Substring(i*chunkCharacters,Math.Min(chunkCharacters,payload.Length-i*chunkCharacters))}}},currentDocument=new{exists=false}});
        var fields=new{generation=new{stringValue=generation},count=new{stringValue=count.ToString()},joinCode=new{stringValue=joinCode??""}};
        writes.Add(new{update=new{name,fields},currentDocument=expectedVersion is null?(object)new{exists=false}:new{updateTime=expectedVersion}});
        // Delete superseded chunks in the same commit. Retained manifests never point to incomplete writes.
        var previous=Send(HttpMethod.Get,name);
        if(previous is not null)
        {
            var old=previous.Value.GetProperty("fields");var oldCount=int.Parse(Text(old,"count"));var oldGeneration=Text(old,"generation");
            for(var i=0;i<oldCount;i++)writes.Add(new{delete=$"{name}/records/{oldGeneration}-{i}"});
        }
        var result=Send(HttpMethod.Post,$"{documents}:commit",new{writes})!.Value;
        // The manifest write precedes deletes in the batch.
        return result.GetProperty("writeResults")[count].GetProperty("updateTime").GetString()!;
    }
    public void Dispose()=>client.Dispose();
}
