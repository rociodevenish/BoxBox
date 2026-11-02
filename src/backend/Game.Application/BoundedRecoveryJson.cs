using System.Buffers;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace Game.Application;

// One bounded wire envelope for frozen campaign and weekend recovery graphs.
// Callers own format identity and actionable domain failure messages.
internal static class BoundedRecoveryJson
{
 private const int MaximumRawBytes=128*1024*1024;
 private const int MaximumStoredBytes=4_000_000;
 private const int CompressionThreshold=256*1024;
 private sealed record Envelope(string StorageFormat,int UncompressedBytes,string Sha256,string Data);
 public static T? Decode<T>(string payload,string format)
 {
  if(Encoding.UTF8.GetByteCount(payload)>MaximumStoredBytes)throw new InvalidDataException("Recovery storage exceeds supported bounds.");
  using var document=JsonDocument.Parse(payload);
  if(!document.RootElement.TryGetProperty("storageFormat",out _))return document.Deserialize<T>(BrowserReplay.JsonOptions);
  var envelope=document.Deserialize<Envelope>(BrowserReplay.JsonOptions)??throw new InvalidDataException();
  if(envelope.StorageFormat!=format||envelope.UncompressedBytes is <1 or >MaximumRawBytes||envelope.Sha256 is null||envelope.Sha256.Length!=64||envelope.Data is null)throw new InvalidDataException("Unsupported recovery storage envelope.");
  var compressed=Convert.FromBase64String(envelope.Data);var raw=new byte[envelope.UncompressedBytes];using var decoder=new BrotliDecoder();
  if(decoder.Decompress(compressed,raw,out var consumed,out var written)!=OperationStatus.Done||consumed!=compressed.Length||written!=raw.Length||!string.Equals(Convert.ToHexString(SHA256.HashData(raw)),envelope.Sha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Damaged recovery storage envelope.");
  return JsonSerializer.Deserialize<T>(raw,BrowserReplay.JsonOptions);
 }
 public static string Encode<T>(T record,string format,Func<SessionFault> oversize)
 {
  using var raw=new BoundedBuffer(MaximumRawBytes,oversize);JsonSerializer.Serialize(raw,record,BrowserReplay.JsonOptions);
  var bytes=raw.GetBuffer().AsSpan(0,checked((int)raw.Length));string payload;
  if(bytes.Length<CompressionThreshold)payload=Encoding.UTF8.GetString(bytes);
  else{
   using var compressed=new BoundedBuffer((MaximumStoredBytes-256)/4*3,oversize);
   using(var encoder=new BrotliStream(compressed,CompressionLevel.Optimal,true))encoder.Write(bytes);
   if(compressed.Length>(MaximumStoredBytes-256)/4*3)throw oversize();
   payload=JsonSerializer.Serialize(new Envelope(format,bytes.Length,Convert.ToHexString(SHA256.HashData(bytes)),Convert.ToBase64String(compressed.GetBuffer(),0,checked((int)compressed.Length))),BrowserReplay.JsonOptions);
  }
  if(Encoding.UTF8.GetByteCount(payload)>MaximumStoredBytes)throw oversize();return payload;
 }
    private sealed class BoundedBuffer(int maximumBytes,Func<SessionFault> oversize):MemoryStream
    {
        public override void Write(byte[] buffer,int offset,int count){Check(count);base.Write(buffer,offset,count);}
        public override void Write(ReadOnlySpan<byte> buffer){Check(buffer.Length);base.Write(buffer);}
        public override void WriteByte(byte value){Check(1);base.WriteByte(value);}
        private void Check(int count){if(count>maximumBytes-Position)throw oversize();}
    }
}
