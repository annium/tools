using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Annium;
using Xws.Extensions;

namespace Xws.Models;

public sealed record Namespace : IEnumerable<string>
{
    #region static

    public static Namespace Of(Type type) => New(type.Namespace!.ToNamespaceArray());

    public static Namespace New(string ns) => New(ns.ToNamespaceArray());

    public static Namespace New(IEnumerable<string> ns) => new(ns.ToArray().EnsureValidNamespace());

    #endregion

    #region instance

    public string Last => _parts.Count > 0 ? _parts[^1] : string.Empty;

    private readonly IReadOnlyList<string> _parts;

    private Namespace(IReadOnlyList<string> parts)
    {
        _parts = parts;
    }

    public bool StartsWith(Namespace ns)
    {
        if (ns._parts.Count > _parts.Count)
            return false;

        for (var i = 0; i < ns._parts.Count; i++)
            if (_parts[i] != ns._parts[i])
                return false;

        return true;
    }

    public Namespace From(Namespace ns)
    {
        if (!StartsWith(ns))
            throw new ArgumentException($"Namespace {this} doesn't contain namespace {ns}");

        return new Namespace(_parts.Skip(ns._parts.Count).ToArray());
    }

    public Namespace Pop()
    {
        if (_parts.Count <= 1)
            return this;

        return new Namespace(_parts.SkipLast(1).ToArray());
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<string> GetEnumerator() => _parts.GetEnumerator();

    public string ToPath(string? basePath = default) =>
        basePath is null ? Path.Combine(_parts.ToArray()) : Path.Combine(basePath, Path.Combine(_parts.ToArray()));

    public override string ToString() => _parts.ToNamespaceString();

    public bool Equals(Namespace? other)
    {
        return other is not null && (ReferenceEquals(this, other) || _parts.SequenceEqual(other._parts));
    }

    public override int GetHashCode() => HashCodeSeq.Combine(_parts);

    #endregion
}
