namespace Xws.Commands
{
    public class Group : Annium.Extensions.Arguments.Group
    {
        public override string Id { get; } = "";

        public override string Description { get; } = "WebSockets tool";

        public Group()
        {
            Add<GenerateCommand>();
        }
    }
}