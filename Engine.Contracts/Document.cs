namespace Engine.Contracts;

public sealed class Document
{
    public Guid DocumentId { get; }
    public Guid? ProjectId { get; }
    public int SchemaVersion { get; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    // ADR-0015 item 7 (KD4): the one length unit of the Document. Each number of a
    // command is a double in this unit, and no code converts a number.
    public const string Millimetre = "mm";

    public string LengthUnit { get; }

    // ADR-0071 item 1 (phase P8b1, TASK-0202): the kind of the Document. A part holds the bodies, the sketches,
    // the images and the datums of one part. An assembly holds references to parts at a fixed version only. The
    // kind is fixed when the Document is made; a document file of an earlier version is a part.
    public const string Part = "part";

    public const string Assembly = "assembly";

    public string Kind { get; }

    private readonly List<LogEntry> _log = new();

    // Finding E26 of the codebase review of 2026-10-08 (TASK-0135): Log returned the list itself, and a
    // cast to List and a Clear moved the version from 2 to 0. Log is a read-only view of the list; the
    // public shape does not change.
    private readonly System.Collections.ObjectModel.ReadOnlyCollection<LogEntry> _logView;

    // The version counts applied commands: it is the count of entries in Log
    // (ADR-0020). A rejected command and a cancelled command do not change it,
    // so a replay of the log rebuilds it. Seq, the identity of an event, is a
    // separate counter that the command bus keeps (ADR-0005), and no code
    // computes one from the other. The version cannot go back, because no code
    // can set it (finding E10 of the codebase review of 2026-09-30).
    public long Version => _log.Count;

    // ADR-0015 item 5: each entry holds the command and the backend that applied it.
    public IReadOnlyList<LogEntry> Log => _logView;

    // Body projection per ADR-0012 §3. The Document holds handles + minimum
    // metadata; the backend owns the geometry data (ADR-0001 §3).
    // Mutation goes only through CommandBus's commit section. Since ADR-0021 the
    // collection holds the live bodies only: a command that consumes a body
    // removes it. A consumed body becomes live again only by an Undo of the step
    // that consumed it (ADR-0031 item 9).
    private readonly Dictionary<Guid, BodyRecord> _bodies = new();
    public IReadOnlyCollection<BodyRecord> Bodies => _bodies.Values;

    // ADR-0033 item 4: one record for each live sketch. Mutation goes only through the
    // commit section of the CommandBus, as for a body. An Undo of the step that created
    // a sketch removes it, and a Redo makes it live again.
    private readonly Dictionary<Guid, Sketch.SketchRecord> _sketches = new();
    public IReadOnlyCollection<Sketch.SketchRecord> Sketches => _sketches.Values;

    // ADR-0045 item 3: one record for each live image, as for a sketch.
    private readonly Dictionary<Guid, Images.ImageRecord> _images = new();
    public IReadOnlyCollection<Images.ImageRecord> Images => _images.Values;

    // ADR-0051 item 4: one record for each datum in effect, as for a sketch. The three origin
    // planes of ADR-0033 item 1 are not in this collection. The collection gives no order: a
    // rebuild takes the datums in the order of the log.
    private readonly Dictionary<Guid, Sketch.DatumRecord> _datums = new();
    public IReadOnlyCollection<Sketch.DatumRecord> Datums => _datums.Values;

    // ADR-0071 item 3: one record for each reference of an assembly, as for an image. A part holds none.
    private readonly Dictionary<Guid, Assembly.PartReference> _references = new();
    public IReadOnlyCollection<Assembly.PartReference> References => _references.Values;

    public Document(Guid? projectId = null, int schemaVersion = 1, string lengthUnit = Millimetre, string kind = Part)
    {
        if (lengthUnit != Millimetre)
            throw new ArgumentException($"The length unit {lengthUnit} is not permitted; the millimetre ({Millimetre}) is the only permitted unit (ADR-0015 item 7).", nameof(lengthUnit));
        if (kind is not (Part or Assembly))
            throw new ArgumentException($"The kind {kind} is not permitted; a Document is a {Part} or an {Assembly} (ADR-0071 item 1).", nameof(kind));

        LengthUnit = lengthUnit;
        Kind = kind;
        _logView = _log.AsReadOnly();
        DocumentId = Guid.NewGuid();
        ProjectId = projectId;
        SchemaVersion = schemaVersion;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    internal void AppendCommand(LogEntry entry)
    {
        _log.Add(entry);
        UpdatedAt = DateTime.UtcNow;
    }

    internal void AddBody(BodyRecord body) => _bodies[body.Handle.Id] = body;

    internal bool HasBody(BodyHandle handle) => _bodies.ContainsKey(handle.Id);

    internal void RemoveBody(BodyHandle handle) => _bodies.Remove(handle.Id);

    // ADR-0028 item 6: the bus replaces the record of a live body that a command
    // changed. The body keeps its place in the collection.
    internal void ReplaceBody(BodyRecord body)
    {
        if (!_bodies.ContainsKey(body.Handle.Id))
            throw new InvalidOperationException($"No live body {body.Handle.Id} to replace.");
        _bodies[body.Handle.Id] = body;
    }

    internal void AddSketch(Sketch.SketchRecord sketch) => _sketches[sketch.Id] = sketch;

    internal bool HasSketch(Guid id) => _sketches.ContainsKey(id);

    internal void RemoveSketch(Guid id) => _sketches.Remove(id);

    // ADR-0033 item 4: an edit gives a new record with the same identifier.
    internal void ReplaceSketch(Sketch.SketchRecord sketch)
    {
        if (!_sketches.ContainsKey(sketch.Id))
            throw new InvalidOperationException($"No live sketch {sketch.Id} to replace.");
        _sketches[sketch.Id] = sketch;
    }

    internal void AddImage(Images.ImageRecord image) => _images[image.Id] = image;

    internal void RemoveImage(Guid id) => _images.Remove(id);

    // ADR-0045 item 1: an edit gives a new record with the same identifier.
    internal void ReplaceImage(Images.ImageRecord image)
    {
        if (!_images.ContainsKey(image.Id))
            throw new InvalidOperationException($"No live image {image.Id} to replace.");
        _images[image.Id] = image;
    }

    internal void AddDatum(Sketch.DatumRecord datum) => _datums[datum.Id] = datum;

    internal void AddReference(Assembly.PartReference reference) => _references[reference.Id] = reference;

    internal void RemoveReference(Guid id) => _references.Remove(id);

    internal void RemoveDatum(Guid id) => _datums.Remove(id);

    // ADR-0051 item 4: a change gives a new record with the same identifier.
    internal void ReplaceDatum(Sketch.DatumRecord datum)
    {
        if (!_datums.ContainsKey(datum.Id))
            throw new InvalidOperationException($"No datum {datum.Id} in effect to replace.");
        _datums[datum.Id] = datum;
    }
}
