using Annium.Extensions.Arguments;

namespace Xws.Commands;

public class Group : Annium.Extensions.Arguments.Group, ICommandDescriptor
{
    public static string Id => "";

    public static string Description => "WebSockets tool";

    public Group()
    {
        Add<GenerateCommand>();
    }
}
