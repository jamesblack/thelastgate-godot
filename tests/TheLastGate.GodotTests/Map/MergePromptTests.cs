using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Map;
using TheLastGate.Notes;
using TheLastGate.Procedural;
using TheLastGate.Windows;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Map;

[TestSuite]
[RequireGodotRuntime]
public class MergePromptTests
{
    private int done;
    private WorldMapEditor editor = null!;
    private NoteSet notes = new();
    private MergePrompt prompt = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public void ShowAPrompt()
    {
        notes = new NoteSet();
        editor = new WorldMapWindow().Editor;
        prompt = GD.Load<PackedScene>("res://Map/merge_prompt.tscn").Instantiate<MergePrompt>();
        Tree.Root.AddChild(prompt);
        done = 0;
        prompt.Done += () => done++;
    }

    [AfterTest]
    public void FreeThePrompt() => prompt.Free();

    [TestCase]
    public void MergePrompt_ShowsTheClash()
    {
        var (moving, places, there) = Clashing();

        prompt.Open(notes, editor, new NoteDrag(moving, true), Onto(places), there);

        AssertThat(prompt.Visible).IsTrue();
        AssertThat(Text("%Question")).IsEqual("Move \"Dungeons\" into \"Places\"?");
        AssertThat(Text("%Clash")).IsEqual("\"Places\" already has a category called \"Dungeons\".");
    }

    [TestCase]
    public void MergePrompt_MergeDisabledOverALimit()
    {
        var (moving, places, there) = Clashing();
        for (var count = 0; count < NoteLimits.NotesPerCategory; count++)
        {
            notes.AddPoint(there, NoteKind.Pin, 1, count, $"{count}");
        }

        notes.AddPoint(moving, NoteKind.Pin, 9, 9, "One more");

        prompt.Open(notes, editor, new NoteDrag(moving, true), Onto(places), there);

        AssertThat(Button("%Merge").Disabled).IsTrue();
        AssertThat(prompt.GetNode<Control>("%Problem").Visible).IsTrue();
        AssertThat(Text("%Problem")).IsEqual("Import would overfill \"Dungeons\".");
    }

    [TestCase]
    public void MergePrompt_MergeMergesAndCloses()
    {
        var (moving, places, there) = Clashing();
        notes.AddPoint(moving, NoteKind.Pin, 9, 9, "Boss");
        prompt.Open(notes, editor, new NoteDrag(moving, true), Onto(places), there);

        Press("%Merge");

        AssertThat(notes.FindCategory(moving)).IsNull();
        AssertThat(notes.FindCategory(there)!.Notes.Select(note => note.Label)).ContainsExactly("Boss");
        AssertThat(done).IsEqual(1);
        AssertThat(prompt.Visible).IsFalse();
    }

    [TestCase]
    public void MergePrompt_RenameMovesAndKeepsIds()
    {
        var (moving, places, there) = Clashing();
        var pin = notes.AddPoint(moving, NoteKind.Pin, 9, 9, "Boss");
        prompt.Open(notes, editor, new NoteDrag(moving, true), Onto(places), there);

        Press("%Rename");
        AssertThat(prompt.GetNode<LineEdit>("%Name").Text).IsEqual("Dungeons (2)");
        Press("%Move");

        AssertThat(notes.FindCategory(moving)!.ParentId).IsEqual(places);
        AssertThat(notes.FindCategory(moving)!.Name).IsEqual("Dungeons (2)");
        AssertThat(notes.OwnerOf(pin)!.Id).IsEqual(moving);
        AssertThat(done).IsEqual(1);
    }

    [TestCase]
    public void MergePrompt_MoveIsDisabledWhileTheNameIsTaken()
    {
        var (moving, places, there) = Clashing();
        prompt.Open(notes, editor, new NoteDrag(moving, true), Onto(places), there);
        Press("%Rename");

        prompt.GetNode<LineEdit>("%Name").Text = "dungeons";
        prompt.GetNode<LineEdit>("%Name").EmitSignal(LineEdit.SignalName.TextChanged, "dungeons");

        AssertThat(Button("%Move").Disabled).IsTrue();
    }

    [TestCase]
    public async Task MergePrompt_ClosesWhenTheDraggedCategoryIsGone()
    {
        var (moving, places, there) = Clashing();
        prompt.Open(notes, editor, new NoteDrag(moving, true), Onto(places), there);

        notes.RemoveCategory(moving);
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);

        AssertThat(prompt.Visible).IsFalse();
        AssertThat(notes.FindCategory(there)).IsNotNull();
    }

    [TestCase]
    public void RenameField_KeepsHotkeysAway()
    {
        var (moving, places, there) = Clashing();
        prompt.Open(notes, editor, new NoteDrag(moving, true), Onto(places), there);

        Press("%Rename");

        AssertThat(TextFields.BeingTypedIn(prompt.GetViewport())).IsSame(prompt.GetNode<LineEdit>("%Name"));
    }

    private static NoteDropSlot Onto(int categoryId) => new(categoryId, -1, true);

    private (int Moving, int Places, int There) Clashing()
    {
        var places = notes.AddCategory("Places", 0);
        var there = notes.AddCategory("Dungeons", 0, places);
        var moving = notes.AddCategory("Dungeons", 0);
        return (moving, places, there);
    }

    private string Text(string path) => prompt.GetNode<Label>(path).Text;

    private ProceduralButton Button(string path) => prompt.GetNode<ProceduralButton>(path);

    private void Press(string path) =>
        Button(path)
            .GetNode<ClickArea>("Click")
            .EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));
}
