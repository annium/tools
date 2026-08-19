using Annium.Testing;
using Backuper.Api.Tools;
using Xunit;

namespace Backuper.Api.Tests.Tools;

public class NamerTests
{
    [Fact]
    public void GetName_RendersTheInstantSortableAndZeroPadded()
    {
        // arrange — the archive is pruned by ordering names as text, so the fields have to run from
        // most to least significant and stay fixed-width; swapping month and day breaks pruning
        // silently, and the id is also what an operator reads
        var namer = new Namer(new TestTimeProvider(advance: false));

        // act
        var name = namer.GetName();

        // assert
        name.Is("2026.07.31_12.00.dump");
    }
}
