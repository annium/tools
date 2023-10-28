using System.Reflection;
using Annium.Core.Runtime.Types;
using Xws.Models;

namespace Xws.Components;

internal interface IParser
{
    ApiModel Parse(Assembly assembly, string name, ITypeManager tm);
}
