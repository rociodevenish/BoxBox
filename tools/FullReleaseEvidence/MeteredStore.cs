using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Game.Application;
namespace FullReleaseEvidence;
public sealed class PayloadMetrics
{
 public long Reads{get;set;}public long Writes{get;set;}public long SuccessfulWrites{get;set;}
 public long ReadBytes{get;set;}public long WriteBytes{get;set;}public long MaximumEncodedBytes{get;set;}public long MaximumRawBytes{get;set;}
 public double ReadMilliseconds{get;set;}public double WriteMilliseconds{get;set;}
}
public sealed class MeteredStore(ISessionStore inner,Control control,Action saveControl):ISessionStore
{
 public Dictionary<string,PayloadMetrics> Metrics{get;}=new(){["campaign"]=new(),["weekend"]=new()};
 public long Lists{get;private set;}public bool Armed{get;set;}public bool CommitFirst{get;set;}public int InjectedFailures{get;private set;}
 private PayloadMetrics For(string id)=>Metrics[id.StartsWith("campaign-",StringComparison.Ordinal)?"campaign":"weekend"];
 private static void Size(PayloadMetrics m,string payload){var count=Encoding.UTF8.GetByteCount(payload);using var json=JsonDocument.Parse(payload);var raw=json.RootElement.TryGetProperty("uncompressedBytes",out var n)?n.GetInt64():count;m.MaximumEncodedBytes=Math.Max(m.MaximumEncodedBytes,count);m.MaximumRawBytes=Math.Max(m.MaximumRawBytes,raw);}
 public StoredSession? Read(string id){var m=For(id);m.Reads++;var timer=Stopwatch.StartNew();try{var value=inner.Read(id);if(value is not null){m.ReadBytes+=Encoding.UTF8.GetByteCount(value.Payload);Size(m,value.Payload);}return value;}finally{m.ReadMilliseconds+=timer.Elapsed.TotalMilliseconds;}}
 public IReadOnlyList<string> ListSessions(){Lists++;return string.IsNullOrEmpty(control.ActiveSession)?[]:[control.ActiveSession];}
 public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code).Where(control.Ids.Contains).ToArray();
 public string Write(string id,string? version,string payload,string? code)=>Write(id,version,payload,code,null);
 public string Write(string id,string? version,string payload,string? code,ScheduledWakeup? wakeup){control.Ids.Add(id);saveControl();var m=For(id);m.Writes++;m.WriteBytes+=Encoding.UTF8.GetByteCount(payload);Size(m,payload);var timer=Stopwatch.StartNew();try{
 if(Armed&&id=="campaign-"+control.CampaignId){Armed=false;InjectedFailures++;if(CommitFirst){inner.Write(id,version,payload,code,wakeup);m.SuccessfulWrites++;}throw new IOException("Deliberate evidence acknowledgement interruption");}
 var value=inner.Write(id,version,payload,code,wakeup);m.SuccessfulWrites++;return value;
 }finally{m.WriteMilliseconds+=timer.Elapsed.TotalMilliseconds;}}
}
