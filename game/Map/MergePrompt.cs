using System;
using Godot;
using TheLastGate.Notes;
using TheLastGate.Procedural;
using TheLastGate.Windows;

namespace TheLastGate.Map;

public partial class MergePrompt : ModalBackdrop
{
    private int clashId;
    private CategoryDrag drag;
    private WorldMapEditor? editor;
    private NoteSet? notes;
    private CategoryDropSlot slot;

    private LineEdit NameField => GetNode<LineEdit>("%Name");

    public event Action? Done;

    public override void _Ready()
    {
        Button("%Merge").Pressed += Merge;
        Button("%Rename").Pressed += ShowRenamePage;
        Button("%Cancel").Pressed += Close;
        Button("%Move").Pressed += Move;
        Button("%Back").Pressed += () => ShowPage(false);
        NameField.TextChanged += _ => ShowMoveState();
        NameField.TextSubmitted += _ => Move();
    }

    public override void _Process(double delta)
    {
        if (Visible && notes != null && (notes.FindCategory(drag.Id) == null || notes.FindCategory(clashId) == null
            || (slot.ParentId != 0 && notes.FindCategory(slot.ParentId) == null)))
        {
            Close();
        }
    }

    public void Open(
        NoteSet shownNotes,
        WorldMapEditor shownEditor,
        CategoryDrag dragged,
        CategoryDropSlot droppedAt,
        int clash)
    {
        if (shownNotes.FindCategory(dragged.Id) is not { } moved || shownNotes.FindCategory(clash) is not { } there)
        {
            return;
        }

        notes = shownNotes;
        editor = shownEditor;
        drag = dragged;
        slot = droppedAt;
        clashId = clash;
        var target = shownNotes.FindCategory(droppedAt.ParentId)?.Name;
        GetNode<Label>("%Question").Text = WorldMapWording.MergeQuestion(moved.Name, target);
        GetNode<Label>("%Clash").Text = WorldMapWording.MergeClash(target, there.Name);
        var check = NoteImport.CheckMergeMoved(shownNotes, dragged.Id, clash);
        var problem = GetNode<Label>("%Problem");
        problem.Visible = check.Status != NoteImportStatus.Ok;
        problem.Text = problem.Visible ? NoteImport.Problem(check) : "";
        Button("%Merge").Disabled = problem.Visible;
        ShowPage(false);
        Visible = true;
    }

    private ProceduralButton Button(string path) => GetNode<ProceduralButton>(path);

    private void ShowPage(bool renaming)
    {
        GetNode<Control>("%MergePage").Visible = !renaming;
        GetNode<Control>("%RenamePage").Visible = renaming;
        foreach (var path in (string[])["%Merge", "%Rename", "%Cancel"])
        {
            GetNode<Control>(path).Visible = !renaming;
        }

        foreach (var path in (string[])["%Move", "%Back"])
        {
            GetNode<Control>(path).Visible = renaming;
        }
    }

    private void ShowRenamePage()
    {
        if (notes?.FindCategory(drag.Id) is not { } moved)
        {
            return;
        }

        GetNode<Label>("%RenameHeading").Text = $"Rename \"{moved.Name}\" to";
        NameField.Text = notes.UniqueCategoryName(slot.ParentId, moved.Name);
        ShowPage(true);
        NameField.GrabFocus();
        NameField.Edit();
        ShowMoveState();
    }

    private void ShowMoveState() =>
        Button("%Move").Disabled = notes == null || !IsFree(NameField.Text);

    private bool IsFree(string name) =>
        name.Trim().Length > 0
        && notes!.FindNamed(slot.ParentId, name.Trim(), notes.Categories.Count, drag.Id) == null;

    private void Merge()
    {
        if (notes != null && editor?.MergeDropped(notes, drag.Id, clashId).Status == NoteImportStatus.Ok)
        {
            Finish();
        }
    }

    private void Move()
    {
        if (notes != null && IsFree(NameField.Text)
            && editor?.RenameDropped(notes, drag.Id, slot, NameField.Text) == true)
        {
            Finish();
        }
    }

    private void Finish()
    {
        Close();
        Done?.Invoke();
    }

    private void Close()
    {
        NameField.ReleaseFocus();
        Visible = false;
    }
}
