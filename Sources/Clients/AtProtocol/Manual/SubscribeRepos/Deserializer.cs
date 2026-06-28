using System;
using System.Collections.Generic;
using System.Formats.Cbor;

using BlueBlaze.Core;

namespace BlueBlaze.Client.AtProtocol.Manual.SubscribeRepos;

public sealed class Deserializer : ISubscriptionMessageDeserializer<IMessage>
{
    public static readonly Deserializer Instance = new();

    private Deserializer() { }

    public IMessage? Deserialize(string? messageType, ReadOnlyMemory<byte> payload)
    {
        return messageType switch
        {
            "#commit" => DeserializeCommit(payload),
            "#sync" => DeserializeSync(payload),
            "#identity" => DeserializeIdentity(payload),
            "#account" => DeserializeAccount(payload),
            "#info" => DeserializeInfo(payload),
            _ => null
        };
    }

    private static MessageCommit DeserializeCommit(ReadOnlyMemory<byte> payload)
    {
        var reader = new CborReader(payload, CborConformanceMode.Lax);
        long seq = 0;
        bool rebase = false;
        bool tooBig = false;
        string? repo = null;
        byte[]? commit = null;
        string? rev = null;
        string? since = null;
        bool sinceRead = false;
        byte[]? blocks = null;
        List<RepoOp>? ops = null;
        List<byte[]>? blobs = null;
        string? time = null;
        ReadOnlyMemory<byte>? prevData = null;

        ReadMap(reader, key =>
        {
            switch (key)
            {
                case "seq":
                    seq = reader.ReadInt64();
                    break;
                case "rebase":
                    rebase = reader.ReadBoolean();
                    break;
                case "tooBig":
                    tooBig = reader.ReadBoolean();
                    break;
                case "repo":
                    repo = reader.ReadTextString();
                    break;
                case "commit":
                    commit = ReadCidLink(reader);
                    break;
                case "rev":
                    rev = reader.ReadTextString();
                    break;
                case "since":
                    sinceRead = true;
                    since = ReadNullableString(reader);
                    break;
                case "blocks":
                    blocks = reader.ReadByteString();
                    break;
                case "ops":
                    ops = ReadRepoOpArray(reader);
                    break;
                case "blobs":
                    blobs = ReadCidLinkArray(reader);
                    break;
                case "time":
                    time = reader.ReadTextString();
                    break;
                case "prevData":
                    prevData = ReadCidLink(reader);
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        });

        _ = sinceRead;
        return new MessageCommit(
            seq, rebase, tooBig,
            repo ?? string.Empty,
            commit ?? [],
            rev ?? string.Empty,
            since,
            blocks ?? [],
            (IReadOnlyList<RepoOp>?)ops ?? [],
            (IReadOnlyList<byte[]>?)blobs ?? [],
            time ?? string.Empty,
            prevData);
    }

    private static MessageSync DeserializeSync(ReadOnlyMemory<byte> payload)
    {
        var reader = new CborReader(payload, CborConformanceMode.Lax);
        long seq = 0;
        string? did = null;
        byte[]? blocks = null;
        string? rev = null;
        string? time = null;

        ReadMap(reader, key =>
        {
            switch (key)
            {
                case "seq":
                    seq = reader.ReadInt64();
                    break;
                case "did":
                    did = reader.ReadTextString();
                    break;
                case "blocks":
                    blocks = reader.ReadByteString();
                    break;
                case "rev":
                    rev = reader.ReadTextString();
                    break;
                case "time":
                    time = reader.ReadTextString();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        });

        return new MessageSync(seq, did ?? string.Empty, blocks ?? [], rev ?? string.Empty, time ?? string.Empty);
    }

    private static MessageIdentity DeserializeIdentity(ReadOnlyMemory<byte> payload)
    {
        var reader = new CborReader(payload, CborConformanceMode.Lax);
        long seq = 0;
        string? did = null;
        string? time = null;
        string? handle = null;

        ReadMap(reader, key =>
        {
            switch (key)
            {
                case "seq":
                    seq = reader.ReadInt64();
                    break;
                case "did":
                    did = reader.ReadTextString();
                    break;
                case "time":
                    time = reader.ReadTextString();
                    break;
                case "handle":
                    handle = reader.ReadTextString();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        });

        return new MessageIdentity(seq, did ?? string.Empty, time ?? string.Empty, handle);
    }

    private static MessageAccount DeserializeAccount(ReadOnlyMemory<byte> payload)
    {
        var reader = new CborReader(payload, CborConformanceMode.Lax);
        long seq = 0;
        string? did = null;
        string? time = null;
        bool active = false;
        string? status = null;

        ReadMap(reader, key =>
        {
            switch (key)
            {
                case "seq":
                    seq = reader.ReadInt64();
                    break;
                case "did":
                    did = reader.ReadTextString();
                    break;
                case "time":
                    time = reader.ReadTextString();
                    break;
                case "active":
                    active = reader.ReadBoolean();
                    break;
                case "status":
                    status = reader.ReadTextString();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        });

        return new MessageAccount(seq, did ?? string.Empty, time ?? string.Empty, active, status);
    }

    private static MessageInfo DeserializeInfo(ReadOnlyMemory<byte> payload)
    {
        var reader = new CborReader(payload, CborConformanceMode.Lax);
        string? name = null;
        string? message = null;

        ReadMap(reader, key =>
        {
            switch (key)
            {
                case "name":
                    name = reader.ReadTextString();
                    break;
                case "message":
                    message = reader.ReadTextString();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        });

        return new MessageInfo(name ?? string.Empty, message);
    }

    private static RepoOp DeserializeRepoOp(CborReader reader)
    {
        string? action = null;
        string? path = null;
        ReadOnlyMemory<byte>? cid = null;
        ReadOnlyMemory<byte>? prev = null;

        ReadMap(reader, key =>
        {
            switch (key)
            {
                case "action":
                    action = reader.ReadTextString();
                    break;
                case "path":
                    path = reader.ReadTextString();
                    break;
                case "cid":
                    cid = ReadNullableCidLink(reader);
                    break;
                case "prev":
                    prev = ReadCidLink(reader);
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        });

        return new RepoOp(action ?? string.Empty, path ?? string.Empty, cid, prev);
    }

    private static List<RepoOp> ReadRepoOpArray(CborReader reader)
    {
        var list = new List<RepoOp>();
        var len = reader.ReadStartArray();
        bool isDefinite = len.HasValue;
        int remaining = len ?? 0;

        while (isDefinite ? remaining-- > 0 : reader.PeekState() != CborReaderState.EndArray)
        {
            list.Add(DeserializeRepoOp(reader));
        }

        reader.ReadEndArray();
        return list;
    }

    private static List<byte[]> ReadCidLinkArray(CborReader reader)
    {
        var list = new List<byte[]>();
        var len = reader.ReadStartArray();
        bool isDefinite = len.HasValue;
        int remaining = len ?? 0;

        while (isDefinite ? remaining-- > 0 : reader.PeekState() != CborReaderState.EndArray)
        {
            list.Add(ReadCidLink(reader));
        }

        reader.ReadEndArray();
        return list;
    }

    private static void ReadMap(CborReader reader, Action<string> handleKey)
    {
        var len = reader.ReadStartMap();
        bool isDefinite = len.HasValue;
        int remaining = len ?? 0;

        while (isDefinite ? remaining-- > 0 : reader.PeekState() != CborReaderState.EndMap)
        {
            var key = reader.ReadTextString();
            handleKey(key);
        }

        reader.ReadEndMap();
    }

    private static byte[] ReadCidLink(CborReader reader)
    {
        reader.ReadTag();
        return reader.ReadByteString();
    }

    private static ReadOnlyMemory<byte>? ReadNullableCidLink(CborReader reader)
    {
        if (reader.PeekState() == CborReaderState.Null)
        {
            reader.ReadNull();
            return null;
        }

        return ReadCidLink(reader);
    }

    private static string? ReadNullableString(CborReader reader)
    {
        if (reader.PeekState() == CborReaderState.Null)
        {
            reader.ReadNull();
            return null;
        }

        return reader.ReadTextString();
    }
}
