using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;

namespace Engine.Tests.Commands;

// Bus-level handler behaviour for Translate + Subtract on the managed stub
// (non-native, deterministic). The stub supports CreateBox but deliberately not
// the transform/boolean capabilities, so these exercise the handlers' structured
// rejections without needing the native library. The successful geometry path is
// covered by the native-gated ManifoldGeometryBackendTests + HTTP scenario.
public class BooleanTransformHandlerTests
{
    private const string SomeGuid = "11111111-1111-1111-1111-111111111111";

    private static (Document doc, CommandBus bus) NewBus()
    {
        var doc = new Document();
        var registry = new CommandRegistry();
        registry.Register(new CreateBoxCommandHandler());
        registry.Register(new TranslateCommandHandler());
        registry.Register(new SubtractCommandHandler());
        var sink = new InMemoryEventSink();
        var bus = new CommandBus(doc, registry, sink, new InProcessMeshBackend());
        return (doc, bus);
    }

    [Fact]
    public async Task Translate_On_Stub_With_Existing_Body_Returns_E_GEOM_CAP_MISSING()
    {
        var (_, bus) = NewBus();
        var box = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        await bus.Apply(box);

        var result = await bus.Apply(new TranslateCommand
        {
            BodyId = box.CommandId,
            Dx = 5, Dy = 0, Dz = 0,
        });

        Assert.Equal(CommandStatus.Rejected, result.Status);
        Assert.Equal(DiagnosticCodes.GeomCapMissing, result.Error!.Code);
    }

    [Fact]
    public async Task Subtract_On_Stub_With_Existing_Bodies_Returns_E_GEOM_CAP_MISSING()
    {
        var (_, bus) = NewBus();
        var a = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        var b = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        await bus.Apply(a);
        await bus.Apply(b);

        var result = await bus.Apply(new SubtractCommand
        {
            MinuendBodyId = a.CommandId,
            SubtrahendBodyId = b.CommandId,
        });

        Assert.Equal(CommandStatus.Rejected, result.Status);
        Assert.Equal(DiagnosticCodes.GeomCapMissing, result.Error!.Code);
    }

    [Fact]
    public async Task Translate_Unknown_Body_Returns_E_GEOM_BODY_NOT_FOUND()
    {
        var (_, bus) = NewBus();
        var result = await bus.Apply(new TranslateCommand
        {
            BodyId = Guid.NewGuid(),
            Dx = 1, Dy = 1, Dz = 1,
        });

        Assert.Equal(CommandStatus.Rejected, result.Status);
        Assert.Equal(DiagnosticCodes.GeomBodyNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task Subtract_Unknown_Operands_Returns_E_GEOM_BODY_NOT_FOUND()
    {
        var (_, bus) = NewBus();
        var result = await bus.Apply(new SubtractCommand
        {
            MinuendBodyId = Guid.NewGuid(),
            SubtrahendBodyId = Guid.NewGuid(),
        });

        Assert.Equal(CommandStatus.Rejected, result.Status);
        Assert.Equal(DiagnosticCodes.GeomBodyNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task Subtract_Missing_Subtrahend_Is_Reported_Even_When_Minuend_Exists()
    {
        // Operand existence is checked before backend capability, so a bad
        // reference is reported as BODY-NOT-FOUND rather than CAP-MISSING.
        var (_, bus) = NewBus();
        var a = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        await bus.Apply(a);

        var result = await bus.Apply(new SubtractCommand
        {
            MinuendBodyId = a.CommandId,
            SubtrahendBodyId = Guid.Parse(SomeGuid),
        });

        Assert.Equal(CommandStatus.Rejected, result.Status);
        Assert.Equal(DiagnosticCodes.GeomBodyNotFound, result.Error!.Code);
    }
}
