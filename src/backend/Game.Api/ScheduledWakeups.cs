using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Game.Application;
using Google.Apis.Auth.OAuth2;
namespace Game.Api;

public sealed class CloudTasksWakeups:IScheduledWakeups,IDisposable
{
    private readonly HttpClient client=new(){Timeout=TimeSpan.FromSeconds(5)};
    private readonly string parent,callback,audience,email;
    public CloudTasksWakeups(IConfiguration configuration)
    {
        string Required(string name)=>configuration["Wakeups:"+name] is {Length:>0} value?value:throw new InvalidOperationException("Configure Wakeups:"+name+" before using Cloud Tasks.");
        var project=Required("ProjectId");var region=Required("Location");var queue=Required("Queue");
        foreach(var value in new[]{project,region,queue})if(value.Any(c=>!char.IsAsciiLetterOrDigit(c)&&c is not ('-' or '_')))throw new InvalidOperationException("Wakeup resource IDs must not contain path characters.");
        parent=$"projects/{project}/locations/{region}/queues/{queue}";
        callback=Required("CallbackUrl");audience=Required("Audience");email=Required("ServiceAccountEmail");
        if(!Uri.TryCreate(callback,UriKind.Absolute,out var uri)||uri.Scheme!="https"||uri.Query.Length>0||uri.AbsolutePath!="/internal/wakeups")throw new InvalidOperationException("Wakeups:CallbackUrl must be an HTTPS /internal/wakeups URL without query parameters.");
    }
    public void Enqueue(ScheduledWakeup wakeup)
    {
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var credentials=GoogleCredential.GetApplicationDefault().CreateScoped("https://www.googleapis.com/auth/cloud-platform");
        var token=credentials.UnderlyingCredential.GetAccessTokenForRequestAsync(cancellationToken:timeout.Token).GetAwaiter().GetResult();
        var payload=JsonSerializer.Serialize(new WakeupInput(wakeup.SessionId,wakeup.Generation,wakeup.Phase,wakeup.WindowRevision),new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var request=new HttpRequestMessage(HttpMethod.Post,$"https://cloudtasks.googleapis.com/v2/{parent}/tasks");
        request.Headers.Authorization=new("Bearer",token);
        request.Content=JsonContent.Create(new{task=new{name=$"{parent}/tasks/wakeup-{wakeup.Generation}",scheduleTime=wakeup.DueUtc.UtcDateTime.ToString("O"),dispatchDeadline="30s",httpRequest=new{
            httpMethod="POST",url=callback,headers=new Dictionary<string,string>{{"Content-Type","application/json"}},body=Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)),oidcToken=new{serviceAccountEmail=email,audience}}}});
        using var response=client.SendAsync(request,timeout.Token).GetAwaiter().GetResult();
        if(response.StatusCode!=HttpStatusCode.Conflict)response.EnsureSuccessStatusCode();
    }
    public void Dispose()=>client.Dispose();
}

// Local development mirrors delivery without granting an HTTP callback bypass. Hosted recovery uses Cloud Scheduler.
public sealed class LocalWakeupQueue:IScheduledWakeups
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string,ScheduledWakeup> pending=[];
    public void Enqueue(ScheduledWakeup wakeup)=>pending[wakeup.Generation]=wakeup;
    public IReadOnlyList<ScheduledWakeup> Due(DateTimeOffset now)=>pending.Values.Where(w=>w.DueUtc<=now).ToArray();
    public void Complete(string generation)=>pending.TryRemove(generation,out _);
}
public sealed class LocalWakeupDelivery(LocalWakeupQueue queue,SessionCoordinator sessions,TimeProvider clock,ILogger<LocalWakeupDelivery> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer=new PeriodicTimer(TimeSpan.FromMilliseconds(250));
        while(await timer.WaitForNextTickAsync(stoppingToken))
            foreach(var wakeup in queue.Due(clock.GetUtcNow()))
            {
                try{await sessions.DeliverScheduledWakeup(new(wakeup.SessionId,wakeup.Generation,wakeup.Phase,wakeup.WindowRevision));queue.Complete(wakeup.Generation);}
                catch(Exception error){logger.LogWarning("Local wakeup delivery failed for {Session}: {Failure}",wakeup.SessionId,error.GetType().Name);}
            }
    }
}
