using System;

namespace Xws.Models;

public interface IHandlerModel
{
    Namespace Namespace { get; }
    string Name { get; }
    Type[] References { get; }
}