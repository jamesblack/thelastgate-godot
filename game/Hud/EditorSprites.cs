using System;
using System.IO;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Config;

namespace TheLastGate.Hud;

public static class EditorSprites
{
    private const long CacheBudgetBytes = 64L * 1024 * 1024;

    private static SpriteLoader? shared;
    private static bool failed;

    public static SpriteLoader? Shared
    {
        get
        {
            if (!Engine.IsEditorHint() || shared != null || failed)
            {
                return shared;
            }

            try
            {
                shared = new SpriteLoader(DataPaths.Root(), CacheBudgetBytes);
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException
                or UnauthorizedAccessException)
            {
                failed = true;
                GD.PushWarning($"The interface sprites can't be shown: {exception.Message}");
            }

            return shared;
        }
    }
}
