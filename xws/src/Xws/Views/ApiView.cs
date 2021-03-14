using System.Collections.Generic;

namespace Xws.Views
{
    internal class ApiView
    {
        public IReadOnlyCollection<string> Usages { get; }
        public string Namespace { get; }
        public string Name { get; }

        public ApiView(
            IReadOnlyCollection<string> usages,
            string ns,
            string name
        )
        {
            Usages = usages;
            Namespace = ns;
            Name = name;
        }
    }
}