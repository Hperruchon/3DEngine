using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0179 item 2 (phase R30i, TASK-0428; the answer Q5 A of the owner): gives each field of the title block of a live sheet. A
// field that the command does not give is empty, so one command replaces the whole block in one step of undo.
public sealed record SetSheetTitleCommand : Command
{
    public override string Name => "SetSheetTitle";
    public override int SchemaVersion => 1;

    public required Guid SheetId { get; init; }
    public string Title { get; init; } = "";
    public string IdentificationNumber { get; init; } = "";
    public string Revision { get; init; } = "";
    public string IssueDate { get; init; } = "";
    public string SheetNumber { get; init; } = "";
    public string LegalOwner { get; init; } = "";
    public string Material { get; init; } = "";
    public string Creator { get; init; } = "";
    public string GeneralTolerances { get; init; } = "";
    public string ApprovalPerson { get; init; } = "";
    public string DocumentType { get; init; } = "";
}
