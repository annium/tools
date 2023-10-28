using Xws.Models;

namespace Xws.Components;

public interface ILoader
{
    ApiModel Load(string assemblyPath, string projectName);
}
