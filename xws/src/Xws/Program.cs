using Annium.Core.Entrypoint;
using Annium.Extensions.Arguments;
using Xws;
using Group = Xws.Commands.Group;

await using var entry = await Entrypoint.Default.UseServicePack<ServicePack>().SetupAsync();

var (provider, ct) = entry;

await Commander.RunAsync<Group>(provider, args, ct);
