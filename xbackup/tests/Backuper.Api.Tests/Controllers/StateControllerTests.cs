using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Annium.Logging;
using Annium.Testing;
using Backuper.Api.Controllers;
using Backuper.Api.State;
using Backuper.Api.Tools;
using Backuper.Notification.Abstract;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Backuper.Api.Tests.Controllers;

// The manual endpoints repeat what the scheduler does, so they repeat its failure modes: prune before
// the new backup exists, leak a dump when a step fails, or answer 500 where the caller asked for
// something that is not configured.
public class StateControllerTests
{
    [Fact]
    public async Task CreateBackupAsync_Succeeds_UploadsBeforePruningAndKeepsNoTempFile()
    {
        // arrange — pruning first meant a failing backup still consumed a slot
        var storage = new TestStorage(["2026.07.29_00.00.dump", "2026.07.30_00.00.dump"]);
        var (controller, connection) = Setup(storage, capacity: 2);

        // act
        var result = await controller.CreateBackupAsync("db", "daily");

        // assert
        result.As<OkObjectResult>().Value.As<string>().IsContaining(".dump");
        storage.Operations.First().StartsWith("upload:").IsTrue();
        storage.Operations.Count(x => x.StartsWith("delete:")).Is(1);
        storage.Operations.Last().Is("delete:2026.07.29_00.00.dump");
        File.Exists(connection.LastPath!).IsFalse();
    }

    [Fact]
    public async Task CreateBackupAsync_UploadFails_KeepsArchiveAndDeletesTempFile()
    {
        // arrange
        var storage = new TestStorage(["2026.07.29_00.00.dump"], failUpload: true);
        var (controller, connection) = Setup(storage, capacity: 1);

        // act
        var result = await controller.CreateBackupAsync("db", "daily");

        // assert — the one stored backup must survive a failed attempt to replace it
        result.As<ObjectResult>().StatusCode.Is(500);
        storage.Items.Has(1);
        File.Exists(connection.LastPath!).IsFalse();
    }

    [Fact]
    public async Task CreateBackupAsync_ChannelThrows_StillReportsTheOriginalFailure()
    {
        // arrange — a channel throwing while reporting an error used to escape the catch block and
        // replace the diagnostic response with a generic one
        var storage = new TestStorage([]);
        var (controller, _) = Setup(storage, capacity: 1, connectionFails: true, channel: new ThrowingChannel());

        // act
        var result = await controller.CreateBackupAsync("db", "daily");

        // assert
        result.As<ObjectResult>().StatusCode.Is(500);
    }

    [Fact]
    public async Task ListBackupsAsync_ReturnsWhatStorageHolds()
    {
        // arrange
        var storage = new TestStorage(["2026.07.30_00.00.dump"]);
        var (controller, _) = Setup(storage, capacity: 2);

        // act
        var result = await controller.ListBackupsAsync("db", "daily");

        // assert
        result.As<OkObjectResult>().Value.As<string[]>().Has(1).At(0).Is("2026.07.30_00.00.dump");
    }

    [Fact]
    public async Task ListBackupsAsync_UnknownServer_ReturnsNotFound()
    {
        // arrange — the lookup used to index the dictionary, so an unknown name threw instead
        var (controller, _) = Setup(new TestStorage([]), capacity: 2);

        // act
        var result = await controller.ListBackupsAsync("nope", "daily");

        // assert
        result.As<NotFoundObjectResult>().Value.Is("Server nope is not configured");
    }

    [Fact]
    public async Task ListBackupsAsync_UnknownPlan_ReturnsNotFound()
    {
        // arrange
        var (controller, _) = Setup(new TestStorage([]), capacity: 2);

        // act
        var result = await controller.ListBackupsAsync("db", "nope");

        // assert
        result.As<NotFoundObjectResult>().Value.Is("Server db has no plan nope");
    }

    [Fact]
    public async Task RestoreBackupAsync_UnknownBackup_ReturnsNotFound()
    {
        // arrange
        var (controller, _) = Setup(new TestStorage(["2026.07.30_00.00.dump"]), capacity: 2);

        // act
        var result = await controller.RestoreBackupAsync("db", "daily", "nope");

        // assert
        result.As<NotFoundObjectResult>().Value.Is("Backup nope not found in storage");
    }

    [Fact]
    public async Task RestoreBackupAsync_KnownBackup_RestoresItAndKeepsNoTempFile()
    {
        // arrange
        var storage = new TestStorage(["2026.07.30_00.00.dump"]);
        var (controller, connection) = Setup(storage, capacity: 2);

        // act
        var result = await controller.RestoreBackupAsync("db", "daily", "2026.07.30_00.00.dump");

        // assert
        result.As<NoContentResult>();
        connection.RestoredPath.IsNotDefault();
        File.Exists(connection.RestoredPath!).IsFalse();
    }

    [Fact]
    public async Task RestoreBackupAsync_RestoreFails_StillDeletesTheDownloadedFile()
    {
        // arrange — a failing pg_restore would otherwise leave a full-sized dump behind on every try
        var storage = new TestStorage(["2026.07.30_00.00.dump"]);
        var (controller, connection) = Setup(storage, capacity: 2, restoreFails: true);

        // act
        var result = await controller.RestoreBackupAsync("db", "daily", "2026.07.30_00.00.dump");

        // assert
        result.As<ObjectResult>().StatusCode.Is(500);
        File.Exists(connection.RestoredPath!).IsFalse();
    }

    [Fact]
    public async Task DeleteBackupAsync_UnknownBackup_ReturnsNotFound()
    {
        // arrange
        var (controller, _) = Setup(new TestStorage(["2026.07.30_00.00.dump"]), capacity: 2);

        // act
        var result = await controller.DeleteBackupAsync("db", "daily", "nope");

        // assert
        result.As<NotFoundObjectResult>().Value.Is("Backup nope not found in storage");
    }

    [Fact]
    public async Task DeleteBackupAsync_KnownBackup_DeletesIt()
    {
        // arrange
        var storage = new TestStorage(["2026.07.30_00.00.dump"]);
        var (controller, _) = Setup(storage, capacity: 2);

        // act
        var result = await controller.DeleteBackupAsync("db", "daily", "2026.07.30_00.00.dump");

        // assert
        result.As<NoContentResult>();
        storage.Items.IsEmpty();
    }

    private static (StateController Controller, TestConnection Connection) Setup(
        TestStorage storage,
        int capacity,
        bool connectionFails = false,
        bool restoreFails = false,
        IChannel? channel = null
    )
    {
        var connection = new TestConnection(connectionFails, restoreFails);
        var plan = new Plan(
            "daily",
            storage,
            "0 0 * * *",
            capacity,
            new Dictionary<string, IChannel> { ["slack"] = channel ?? new TestChannel() }
        );
        var state = new Backuper.Api.State.State(
            new Dictionary<string, Server>
            {
                ["db"] = new("db", connection, new Dictionary<string, Plan> { ["daily"] = plan }),
            }
        );
        var controller = new StateController(
            () => state,
            new Namer(new TestTimeProvider()),
            new TestMediator(),
            new TestServiceProvider(),
            VoidLogger.Instance
        );

        return (controller, connection);
    }
}
