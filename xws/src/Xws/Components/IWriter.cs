using Xws.Views;

namespace Xws.Components;

internal interface IWriter
{
    void Write(string output, ApiView api);
}
