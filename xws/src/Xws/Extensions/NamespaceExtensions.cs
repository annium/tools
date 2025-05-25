using System;
using System.Collections.Generic;
using System.Linq;
using Xws.Models;

namespace Xws.Extensions;

public static class NamespaceExtensions
{
    public static Namespace ToNamespace(this string ns) => Namespace.New(ns);

    public static Namespace ToNamespace(this IEnumerable<string> ns) => Namespace.New(ns);

    public static string[] ToNamespaceArray(this string ns)
    {
        if (ns is null)
            throw new ArgumentException("Value cannot be null.", nameof(ns));

        if (ns == string.Empty)
            return [];

        return ns.Split('.').ToArray().EnsureValidNamespace();
    }

    public static string ToNamespaceString(this IEnumerable<string> ns) => string.Join('.', ns);

    internal static T EnsureValidNamespace<T>(this T ns)
        where T : IEnumerable<string>
    {
        if (ns is null)
            throw new ArgumentNullException(nameof(ns));

        if (ns.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException($"Namespace {ns.ToNamespaceString()} contains empty parts");

        return ns;
    }

    public static IReadOnlyCollection<Namespace> ToUsagesFrom(
        this IEnumerable<Namespace> references,
        Namespace target
    ) => references.ToArray().Where(x => !target.StartsWith(x)).Distinct().OrderNamespaces().ToArray();

    public static IEnumerable<Namespace> OrderNamespaces(this IEnumerable<Namespace> namespaces) =>
        namespaces.OrderBy(x => x.FirstOrDefault() != "System").ThenBy(x => x.ToString());

    public static IEnumerable<string> OrderNamespaces(this IEnumerable<string> namespaces) =>
        namespaces.OrderBy(x => !x.StartsWith("System")).ThenBy(x => x.ToString());

    public static IReadOnlyCollection<string> ToUsageStrings(this IEnumerable<Namespace> references) =>
        references.Select(x => x.ToString()).ToArray();
}
