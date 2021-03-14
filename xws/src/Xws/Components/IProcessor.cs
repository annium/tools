using Xws.Models;
using Xws.Views;

namespace Xws.Components
{
    internal interface IProcessor
    {
        ClientContainerView Process(Namespace ns, ApiModel api);
    }
}