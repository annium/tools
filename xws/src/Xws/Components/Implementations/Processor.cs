using System;
using System.Collections.Generic;
using System.Linq;
using Annium.Core.Primitives;
using Annium.Core.Reflection;
using Xws.Extensions;
using Xws.Models;
using Xws.Views;

namespace Xws.Components.Implementations
{
    internal class Processor : IProcessor
    {
        private const string Clients = "Clients";
        private const string Client = "Client";
        private const string Root = "Root";

        public ApiView Process(Namespace rootNs, ApiModel api)
        {
            var ns = rootNs.Append(Clients).ToNamespace();
            var candidates = BuildClientCandidates(
                GetRawViews(ns, api.Broadcasters, Parse),
                GetRawViews(ns, api.EventHandlers, Parse),
                GetRawViews(ns, api.RequestHandlers, Parse),
                GetRawViews(ns, api.RequestResponseHandlers, Parse),
                GetRawViews(ns, api.SubscriptionHandlers, Parse)
            );

            return BuildApiNode(ns, api.Project, $"{api.Project}{Client}", $"{api.Project}Test{Client}", candidates);
        }

        private IHandlerView Parse(BroadcasterModel model) =>
            new BroadcasterView(model.Message.FriendlyName(), model.Name);

        private IHandlerView Parse(EventHandlerModel model) =>
            new EventHandlerView(model.Message.FriendlyName(), model.Name);

        private IHandlerView Parse(RequestHandlerModel model) =>
            new RequestHandlerView(model.Request.FriendlyName(), model.Request.HasDefaultConstructor(), model.Name);

        private IHandlerView Parse(RequestResponseHandlerModel model) =>
            new RequestResponseHandlerView(model.Request.FriendlyName(), model.Request.HasDefaultConstructor(), model.Name, model.Response.FriendlyName());

        private IHandlerView Parse(SubscriptionHandlerModel model) =>
            new SubscriptionHandlerView(model.Init.FriendlyName(), model.Init.HasDefaultConstructor(), model.Name, model.Message.FriendlyName());

        private IReadOnlyCollection<HandlerContainer> GetRawViews<T>(
            Namespace rootNs,
            IReadOnlyCollection<T> models,
            Func<T, IHandlerView> parseView
        )
            where T : IHandlerModel
        {
            return models
                .Select(x =>
                {
                    var ns = rootNs.Concat(x.Namespace).ToNamespace();
                    var view = parseView(x);
                    var usages = CollectNamespaces(x).ToUsagesFrom(ns);

                    return new HandlerContainer(usages, ns, x.Name, view);
                })
                .ToArray();
        }

        private IReadOnlyCollection<Namespace> CollectNamespaces(IHandlerModel model)
        {
            var references = new HashSet<Type>();

            foreach (var x in model.References)
                CollectTypeReferences(x);

            return references.Select(Namespace.Of).ToHashSet();

            void CollectTypeReferences(Type type)
            {
                if (type.IsBaseType())
                    return;

                references.Add(type);

                if (type.IsGenericType)
                    foreach (var argument in type.GetGenericArguments())
                        CollectTypeReferences(argument);
            }
        }

        private IReadOnlyCollection<ClientCandidate> BuildClientCandidates(
            params IReadOnlyCollection<HandlerContainer>[] containers
        ) => containers
            .SelectMany(x => x)
            .GroupBy(x => x.Namespace)
            .Select(x =>
            {
                var ns = x.Key.Pop();
                var usages = x.SelectMany(y => y.Usages)
                    .Append(Namespace.New("Annium.Infrastructure.WebSockets.Client"))
                    .ToUsagesFrom(ns);
                var name = x.Key.Last;
                var handlers = x.Select(y => y.View).ToArray();

                return new ClientCandidate(usages, ns, name, $"{name}{Client}", handlers);
            })
            .ToArray();

        private ApiView BuildApiNode(
            Namespace ns,
            string name,
            string type,
            string testType,
            IReadOnlyCollection<ClientCandidate> candidates
        )
        {
            var node = BuildClientNode(ns, string.Empty, type, candidates);

            var apiNs = Namespace.New(node.Namespace).Pop().ToNamespaceString();
            var apiUsages = new[]
            {
                "System",
                "System.Net.WebSockets",
                "Annium.Core.DependencyInjection",
                "Annium.Infrastructure.WebSockets.Client",
                node.Namespace
            }.OrderNamespaces().ToArray();

            var clientUsages = node.Usages
                .Concat(new[]
                {
                    "System",
                    "System.Threading",
                    "System.Threading.Tasks"
                })
                .OrderNamespaces()
                .ToArray();
            var clientRoot = new ClientRootView(node.Namespace, clientUsages, type, node.Clients);

            var testClientUsages = node.Usages
                .Concat(new[]
                {
                    "System",
                    "System.Threading.Tasks"
                })
                .OrderNamespaces()
                .ToArray();
            var testClientRoot = new ClientRootView(node.Namespace, testClientUsages, testType, node.Clients);

            return new ApiView(apiNs, apiUsages, name, clientRoot, testClientRoot);
        }

        private ClientContainerView BuildClientNode(
            Namespace ns,
            string name,
            string type,
            IReadOnlyCollection<ClientCandidate> candidates
        )
        {
            if (candidates.Count == 0)
                throw new ArgumentException("Can't build container without clients");

            if (candidates.Count == 1)
                return new ClientContainerView(
                    new[] { candidates.First().Namespace }
                        .ToUsagesFrom(ns)
                        .ToUsageStrings()
                        .Append("Annium.Infrastructure.WebSockets.Client")
                        .OrderNamespaces()
                        .ToArray(),
                    ns.ToString(),
                    name,
                    type,
                    candidates.Select(x => (ClientView) x).ToArray()
                );

            var lookup = candidates.ToLookup(x => x.Namespace == ns);

            var children = lookup[true].ToArray();

            var ancestors = lookup[false]
                .GroupBy(x => ns.Append(x.Namespace.From(ns).First()).ToNamespace())
                .ToDictionary(
                    x => x.Key,
                    x => BuildClientNode(x.Key, x.Key.Last(), $"{x.Key.Last()}{Root}", x.ToArray())
                );

            var clientUsages = new[]
            {
                "Annium.Infrastructure.WebSockets.Client",
            };

            var usages = children
                .Select(x => x.Namespace)
                .Concat(ancestors.Keys)
                .ToUsagesFrom(ns)
                .ToUsageStrings()
                .Concat(clientUsages)
                .OrderNamespaces()
                .ToArray();

            var clients = ancestors.Values
                .OrderBy(x => x.Namespace.ToString())
                .ThenBy(x => x.Name)
                .Concat<IClientView>(children
                    .Select(x => (ClientView) x)
                    .OrderBy(x => x.Namespace)
                    .ThenBy(x => x.Name)
                )
                .ToArray();

            return new ClientContainerView(usages, ns.ToString(), name, type, clients);
        }
    }
}