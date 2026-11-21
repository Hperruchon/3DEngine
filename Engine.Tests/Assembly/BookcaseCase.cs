using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;

namespace Engine.Tests.Assembly;

// Row K10a (TASK-0429): the bookcase Bibliotheque120 of the owner, the first case of K10 (register entry R-0070, the
// choice of the owner of 2026-11-20). The owner made the bookcase in the window; its document file stays on the computer
// of the owner and is not in the repository. This builder sends the commands of its 19 bodies through a DocumentSession,
// with the names, the sizes and the places of the file. docs/proposals/2026-11-21-the-bookcase-case-of-k10.md gives the
// table of the bodies, and BookcaseCaseTests checks each box against it.
//
// The file holds 240 commands: 48 CreateBox, 54 Translate, 48 RenameBody and 90 Undo. After the undos, 60 commands stay in
// effect: one CreateBox, one Translate and one RenameBody for each body, and three more Translate for the back panel
// ("Fond", -197 then +394 on y) and the two shelves ("Bas" and "Haut", +3 then -6 on y). The builder sends one Translate
// for each body, the sum of the moves of the file, so it sends 57 commands. The final bodies are the same.
//
// CreateBox makes a box with its centre at the origin of the Document, so the Translate of a body is the centre of its
// box. Each size and each centre is an integer of millimetres, so each bound of a box is exact in a double.
internal static class BookcaseCase
{
    // One body of the bookcase: its name, the size of its box on x, y and z, and the centre of its box. All in mm.
    internal sealed record Part(string Name, double SizeX, double SizeY, double SizeZ, double X, double Y, double Z);

    // The 14 volumes of the modules, then the 5 panels of the open case between them, in the order of the file. No body
    // of the file has the name of a module 4 or of a module 13.
    internal static readonly IReadOnlyList<Part> Parts =
    [
        new("Module 1 - volume indicatif", 800, 400, 1000, -1300, 0, 500),
        new("Module 2 - volume indicatif", 800, 400, 1000, 1300, 0, 500),
        new("Module 3 - volume indicatif", 400, 400, 950, 100, 0, 1875),
        new("Module 5 - volume indicatif", 400, 400, 550, 1300, 0, 2075),
        new("Module 6 - volume indicatif", 600, 400, 600, 200, 0, 300),
        new("Module 7 - volume indicatif", 800, 400, 400, -1300, 0, 1200),
        new("Module 8 - volume indicatif", 600, 400, 400, -600, 0, 800),
        new("Module 9 - volume indicatif", 800, 400, 600, -500, 0, 300),
        new("Module 10 - volume indicatif", 400, 400, 600, 700, 0, 300),
        new("Module 11 - volume indicatif", 400, 400, 400, 1300, 0, 1600),
        new("Module 12 - volume indicatif", 600, 400, 400, -600, 0, 1200),
        new("Module 14 - volume indicatif", 800, 400, 800, 700, 0, 1800),
        new("Module 15 - volume indicatif", 800, 400, 950, -1300, 0, 1875),
        new("Module 16 - volume indicatif", 800, 400, 950, -500, 0, 1875),
        new("Cote gauche", 18, 400, 800, -291, 0, 1000),
        new("Cote droit", 18, 400, 800, 891, 0, 1000),
        new("Bas", 1164, 394, 18, 300, -3, 609),
        new("Haut", 1164, 394, 18, 300, -3, 1391),
        new("Fond", 1164, 6, 800, 300, 197, 1000),
    ];

    // Sends CreateBox, Translate and RenameBody for each part, and gives the handle of each final body by its name.
    internal static async Task<IReadOnlyDictionary<string, Guid>> Build(DocumentSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var bodies = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var part in Parts)
        {
            var box = Output(await Applied(session, new CreateBoxCommand { SizeX = part.SizeX, SizeY = part.SizeY, SizeZ = part.SizeZ }));
            var placed = Output(await Applied(session, new TranslateCommand { BodyId = box, Dx = part.X, Dy = part.Y, Dz = part.Z }));
            await Applied(session, new RenameBodyCommand { BodyId = placed, NewName = part.Name });
            bodies.Add(part.Name, placed);
        }

        return bodies;
    }

    // The count of the commands that Build sends.
    internal static int CommandCount => 3 * Parts.Count;

    internal static async Task<CommandResult> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result;
    }

    internal static Guid Output(CommandResult result)
        => result.Outputs.TryGet<Guid>("bodyId", out var id) ? id : throw new InvalidOperationException("The command gave no output bodyId.");
}
