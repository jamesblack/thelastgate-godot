using System;
using System.IO;
using Godot;
using TheLastGate.Map;
using TheLastGate.Net;

namespace TheLastGate.Hud.Depot;

public sealed class DepotLayoutKeeper
{
    private const string FilePrefix = "TLGDepotLayout_";
    private const string SetAsideSuffix = ".bad";
    private const string TemporarySuffix = ".tmp";

    private readonly string path;
    private bool unreadable;

    public DepotLayoutKeeper(ServerAddress server)
    {
        path = ProjectSettings.GlobalizePath("user://" + ServerFiles.Name(FilePrefix, server.Host, server.Port));
        Layouts = Read() ?? new DepotLayouts();
    }

    public DepotLayouts Layouts { get; }

    public void Save()
    {
        if (unreadable || !Layouts.Changed)
        {
            return;
        }

        var temporaryPath = path + TemporarySuffix;
        try
        {
            File.WriteAllText(temporaryPath, DepotLayoutStore.Save(Layouts));
            File.Move(temporaryPath, path, true);
            Layouts.ClearChanged();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushWarning($"Couldn't save the depot page layout to {path}");
        }
    }

    private DepotLayouts? Read()
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            if (DepotLayoutStore.Load(json) is { } layouts)
            {
                return layouts;
            }

            var setAsidePath = FreeSetAsidePath();
            File.Move(path, setAsidePath);
            GD.PushWarning($"The saved depot page layout couldn't be read and was set aside as {setAsidePath}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            unreadable = true;
            GD.PushWarning($"Couldn't read the depot page layout from {path}, so changes won't be saved this session");
        }

        return null;
    }

    private string FreeSetAsidePath()
    {
        var candidate = path + SetAsideSuffix;
        for (var attempt = 2; File.Exists(candidate); attempt++)
        {
            candidate = path + SetAsideSuffix + attempt;
        }

        return candidate;
    }
}
