using System.Collections.Generic;

namespace Xws.Views
{
    internal interface IClientView
    {
        IReadOnlyCollection<string> Usages { get; }
        string Namespace { get; }
        string Name { get; }
        string Type { get; }
    }
}