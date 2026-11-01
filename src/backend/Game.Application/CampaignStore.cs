using System.Buffers;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace Game.Application;

public sealed class CampaignStore(ISessionStore sessions):ICampaignStore
{
    // Measured 23-event diagnostic histories reach 64MB raw. Bound both serialization and
    // decompression at 128MiB; the existing durable adapter's 4MB/chunk/CAS limits stay intact.
    private const int MaximumRawBytes=128*1024*1024;
    private const int MaximumStoredBytes=4_000_000;
    private const int CompressionThreshold=256*1024;
    private const string StorageFormat="campaign-brotli-json-v1";
    private sealed record Envelope(string StorageFormat,int UncompressedBytes,string Sha256,string Data);

    public IReadOnlyList<string> ListScheduled()=>sessions.ListSessions().Where(id=>id.StartsWith("campaign-",StringComparison.Ordinal)).Select(id=>id[9..]).ToArray();
    public StoredCampaign? Read(string id)
    {
        var saved=sessions.Read("campaign-"+id);if(saved is null)return null;
        CampaignRecord? record;
        try
        {
            if(Encoding.UTF8.GetByteCount(saved.Payload)>MaximumStoredBytes)throw new InvalidDataException("Campaign storage exceeds supported bounds.");
            using var document=JsonDocument.Parse(saved.Payload);
            if(document.RootElement.TryGetProperty("storageFormat",out _))
            {
                var envelope=document.Deserialize<Envelope>(BrowserReplay.JsonOptions)??throw new InvalidDataException();
                if(envelope.StorageFormat!=StorageFormat||envelope.UncompressedBytes is <1 or >MaximumRawBytes||envelope.Sha256 is null||envelope.Sha256.Length!=64||envelope.Data is null)
                    throw new InvalidDataException("Unsupported campaign storage envelope.");
                var compressed=Convert.FromBase64String(envelope.Data);
                // A fixed, declaration-bounded destination prevents expansion beyond the cap.
                // Reject truncation, excess decompressed bytes and trailing compressed data.
                var raw=new byte[envelope.UncompressedBytes];using var decoder=new BrotliDecoder();
                if(decoder.Decompress(compressed,raw,out var consumed,out var written)!=OperationStatus.Done||consumed!=compressed.Length||written!=raw.Length
                    ||!string.Equals(Convert.ToHexString(SHA256.HashData(raw)),envelope.Sha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Damaged campaign storage envelope.");
                record=JsonSerializer.Deserialize<CampaignRecord>(raw,BrowserReplay.JsonOptions);
            }
            else record=document.Deserialize<CampaignRecord>(BrowserReplay.JsonOptions); // Historical raw v1 records.
            if(record is not null)CampaignSaveIntegrity.Validate(record,id);
        }
        catch(Exception error) when(error is JsonException or ArgumentException or InvalidOperationException or NullReferenceException or KeyNotFoundException or InvalidDataException or FormatException){throw CampaignSaveIntegrity.Incompatible();}
        if(record is null||record.Schema!=CampaignCoordinator.Schema||record.CampaignId!=id)
            throw new SessionFault(SessionFailure.Conflict,"Campaign save is incompatible. Preserve it for the compatible deployment.");
        return new(saved.Version,record);
    }
    public string Write(string id,string? expectedVersion,CampaignRecord record)
    {
        using var raw=new BoundedBuffer(MaximumRawBytes);JsonSerializer.Serialize(raw,record,BrowserReplay.JsonOptions);
        var bytes=raw.GetBuffer().AsSpan(0,checked((int)raw.Length));string payload;
        if(bytes.Length<CompressionThreshold)payload=Encoding.UTF8.GetString(bytes);
        else
        {
            using var compressed=new BoundedBuffer((MaximumStoredBytes-256)/4*3);
            using(var encoder=new BrotliStream(compressed,CompressionLevel.Optimal,true))encoder.Write(bytes);
            // Base64 plus the small version/hash envelope must fit the unchanged adapter bound.
            if(compressed.Length>(MaximumStoredBytes-256)/4*3)throw Oversize();
            payload=JsonSerializer.Serialize(new Envelope(StorageFormat,bytes.Length,Convert.ToHexString(SHA256.HashData(bytes)),Convert.ToBase64String(compressed.GetBuffer(),0,checked((int)compressed.Length))),BrowserReplay.JsonOptions);
        }
        if(Encoding.UTF8.GetByteCount(payload)>MaximumStoredBytes)throw Oversize();
        return sessions.Write("campaign-"+id,expectedVersion,payload,null,record.AcquisitionWakeup);
    }
    private static SessionFault Oversize()=>new(SessionFailure.InvalidInput,"Campaign recovery exceeds supported storage bounds. No saved data was changed. Preserve the current campaign for diagnostics; reduce new advice requests or restore a compatible deployment with a reviewed larger storage format.");
    private sealed class BoundedBuffer(int maximumBytes):MemoryStream
    {
        public override void Write(byte[] buffer,int offset,int count){Check(count);base.Write(buffer,offset,count);}
        public override void Write(ReadOnlySpan<byte> buffer){Check(buffer.Length);base.Write(buffer);}
        public override void WriteByte(byte value){Check(1);base.WriteByte(value);}
        private void Check(int count){if(count>maximumBytes-Position)throw Oversize();}
    }
}
