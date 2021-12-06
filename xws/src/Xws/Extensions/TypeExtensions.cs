using System;
using System.Collections.Immutable;

namespace Xws.Extensions;

internal static class TypeExtensions
{
    private static readonly ImmutableArray<Type> BaseTypes = ImmutableArray.Create(
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(short),
        typeof(ushort),
        typeof(byte),
        typeof(sbyte),
        typeof(bool),
        typeof(float),
        typeof(double),
        typeof(decimal),
        typeof(char),
        typeof(string),
        typeof(object),
        typeof(void)
    );

    public static bool IsBaseType(this Type type) => BaseTypes.Contains(type);
}