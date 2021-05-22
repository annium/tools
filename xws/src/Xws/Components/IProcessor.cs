using Xws.Models;
using Xws.Views;

namespace Xws.Components
{
    internal interface IProcessor
    {
        ApiView Process(Namespace ns, ApiModel api);
    }
}