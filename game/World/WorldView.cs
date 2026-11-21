using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Game;
using TheLastGate.State;
using GlidePoint = System.Numerics.Vector2;

namespace TheLastGate.World;

public partial class WorldView : Node2D
{
    private const int TileCount = WorldMap.TileCount;
    private const int NoTile = MapAction.NoTile;
    private const int TintShift = 4;
    private const int HighlightBit = 1 << 7;
    private const int SelectedBit = 1 << 8;
    private const int UnshownEffect = -1;
    private const int UnshownSprite = -1;
    private const float HighlightBrightness = 2f;
    private const float ShadowAlpha = 0.5f;
    private const string TintShaderPath = "res://World/sprite_tint.gdshader";
    private const string NameplateScenePath = "res://World/nameplate.tscn";
    private const string MagicGlowShaderPath = "res://World/magic_glow.gdshader";
    private const int PlayerNameplateLayer = 1;
    private const int ReservedValue = 4;
    private const int QueuedUseSprite = 45;

    // Sprite 4001 numbers the first queued use.
    private const int QueuedNumberSprites = 4000;
    private readonly Sprite2D actionMarker = new() { Centered = false, Visible = false };
    private readonly HashSet<int> areaFloors = [];
    private readonly HashSet<int> areaGlows = [];
    private readonly Sprite2D attackMarker = new() { Centered = false, Visible = false };

    private readonly Vector2I[] characterOffsets = new Vector2I[TileCount];
    private readonly SpriteLayer characters = new();
    private readonly HashSet<int> charactersShown = [];
    private readonly SpriteLayer floors = new();
    private readonly Stack<Nameplate> freeNameplates = [];
    private readonly Sprite2D giveMarker = new() { Centered = false, Visible = false };
    private readonly Dictionary<int, GlidePoint> glidePoints = [];
    private readonly Dictionary<MagicGlow, ShaderMaterial> glowMaterials = [];
    private readonly Dictionary<int, Nameplate> nameplatesByCharacter = [];
    private readonly SpriteLayer objects = new();
    private readonly List<Sprite2D> queuedNumberMarkers = [];
    private readonly List<Sprite2D> queuedUseMarkers = [];
    private readonly Sprite2D[] shadowNodes = new Sprite2D[TileCount];
    private readonly int[] shownShadows = new int[TileCount];
    private readonly TileEffects?[] tileEffects = new TileEffects?[TileCount];
    private readonly Dictionary<(SpriteTint Tint, bool Highlighted, bool Selected), ShaderMaterial> tintMaterials = [];
    private readonly Sprite2D walkMarker = new() { Centered = false, Visible = false };

    // The tiles are put together at a whole-number scale of the virtual resolution, and only the finished picture is
    // stretched to the window. Stretching each tile on its own leaves dark specks where diamond corners meet whenever
    // the scale isn't a whole number, because some GPUs round those corner texels to transparent. The scale is rounded
    // up rather than fixed at 1, so gliding sprites keep their sub-pixel positions.
    private readonly SubViewport worldCanvas = new()
    {
        TransparentBg = true,
        GuiDisableInput = true,
        CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest,
    };

    private readonly Sprite2D worldPicture = new() { Centered = false };
    private bool areaArmed;
    private (int X, int Y) areaEnd;
    private (int X, int Y)? areaStart;
    private Node2D floorLayer = null!;
    private MovementGlide glide = new();
    private bool glideMovement;
    private bool glideRecorded;
    private Shader? glowShader;
    private MapAction hover = MapAction.Nothing;
    private Vector2I mapShift = new(IsometricProjection.BaseMapShiftX, 0);
    private PackedScene nameplateScene = null!;
    private Node2D objectLayer = null!;
    private int[] queuedTiles = [];
    private Shader? tintShader;
    private float worldGamma = Gamma.Normal;

    public GameState State { get; set; } = null!;
    public GameSession Session { get; set; } = null!;
    public SpriteLoader Sprites { get; set; } = null!;
    public IFontSet Fonts { get; set; } = null!;
    public bool HideWalls { get; set; } = true;
    public bool ShowNames { get; set; } = true;
    public bool ShowHealthPercent { get; set; } = true;
    public bool ShowHealthBars { get; set; }
    public bool ShowShadows { get; set; } = true;
    public double LastRefreshMilliseconds { get; private set; }
    public int SelectedCharacter { get; private set; }

    public bool PickTileDrawnUnderMouse { get; set; }

    // A sprite's brightness is only set again when its light changes, so a new gamma has every lit sprite redone on the
    // next refresh.
    public float WorldGamma
    {
        get => worldGamma;
        set
        {
            if (value == worldGamma)
            {
                return;
            }

            worldGamma = value;
            foreach (var layer in (SpriteLayer[])[floors, objects, characters])
            {
                Array.Fill(layer.ShownEffects, UnshownEffect);
            }
        }
    }

    // Switching on waits for the next tick to glide from; switching off puts everything back on the tick.
    public bool GlideMovement
    {
        get => glideMovement;
        set
        {
            if (value == glideMovement)
            {
                return;
            }

            glideMovement = value;
            glide = new MovementGlide();
            glideRecorded = false;
            if (!value && IsInsideTree())
            {
                SnapToTicks();
            }
        }
    }

    public Action<byte[]> SendCommand { get; set; } = _ => { };

    public Action PlayClick { get; set; } = () => { };

    public override void _Ready()
    {
        nameplateScene = GD.Load<PackedScene>(NameplateScenePath);
        AddChild(worldCanvas);
        AddChild(worldPicture);
        worldPicture.Texture = worldCanvas.GetTexture();
        FitWorldCanvasToWindow();
        GetTree().Root.SizeChanged += FitWorldCanvasToWindow;
        floorLayer = AddLayer();
        objectLayer = AddLayer();
        for (var row = WorldMap.Size - 1; row >= 0; row--)
        {
            for (var column = 0; column < WorldMap.Size; column++)
            {
                var tileIndex = column + row * WorldMap.Size;
                floors.Nodes[tileIndex] = AddSpriteNode(floorLayer, column, row);
                objects.Nodes[tileIndex] = AddSpriteNode(objectLayer, column, row);
                characters.Nodes[tileIndex] = AddSpriteNode(objectLayer, column, row);
                shadowNodes[tileIndex] = AddShadowNode(characters.Nodes[tileIndex]);
            }
        }

        foreach (var marker in (Sprite2D[])[walkMarker, attackMarker, giveMarker, actionMarker])
        {
            floorLayer.AddChild(marker);
        }
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventKey { Pressed: true } key)
        {
            if (areaArmed && key.Keycode == Key.Escape)
            {
                StopArea();
                GetViewport().SetInputAsHandled();
            }

            return;
        }

        if (!areaArmed)
        {
            return;
        }

        switch (inputEvent)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
                StopArea();
                GetViewport().SetInputAsHandled();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press
                when GetViewport().GuiGetHoveredControl() == null && WorldTileAt(press.Position) is { } startTile:
                areaStart = startTile;
                areaEnd = startTile;
                ShowArea();
                GetViewport().SetInputAsHandled();
                break;
            case InputEventMouseMotion motion when areaStart != null && WorldTileAt(motion.Position) is { } endTile:
                if (AreaUse.FitEnd(areaStart.Value, endTile) is var fitted && fitted != areaEnd)
                {
                    areaEnd = fitted;
                    ShowArea();
                }

                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } when areaStart != null:
                QueueArea();
                StopArea();
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    private (int X, int Y)? WorldTileAt(Vector2 screenPosition)
    {
        if (PointUnder(screenPosition) is not var (column, row))
        {
            return null;
        }

        ref readonly var tile = ref State.Map.Tiles[(int)column + (int)row * WorldMap.Size];
        return (tile.X, tile.Y);
    }

    private List<int> AreaTiles()
    {
        var area = new List<int>();
        if (areaStart is not { } start)
        {
            return area;
        }

        var (firstX, lastX) = (Math.Min(start.X, areaEnd.X), Math.Max(start.X, areaEnd.X));
        var (firstY, lastY) = (Math.Min(start.Y, areaEnd.Y), Math.Max(start.Y, areaEnd.Y));
        for (var tileIndex = 0; tileIndex < TileCount; tileIndex++)
        {
            ref readonly var tile = ref State.Map.Tiles[tileIndex];
            if (tile.X >= firstX && tile.X <= lastX && tile.Y >= firstY && tile.Y <= lastY)
            {
                area.Add(tileIndex);
            }
        }

        return area;
    }

    private void FindArea()
    {
        areaFloors.Clear();
        areaGlows.Clear();
        var area = AreaTiles();
        areaFloors.UnionWith(area);
        areaGlows.UnionWith(AreaUse.Pick(State.Map, area).Select(target => target.TileIndex));
    }

    public void StartAreaUse()
    {
        areaArmed = true;
        State.Chat.Add(new LogLine("Drag an area to use everything in it.", (int)TextColour.Green));
    }

    private void ShowArea()
    {
        var before = areaFloors.ToList();
        FindArea();
        foreach (var tileIndex in before.Union(areaFloors))
        {
            ShowTile(tileIndex);
        }
    }

    private void StopArea()
    {
        areaArmed = false;
        areaStart = null;
        ShowArea();
    }

    private void QueueArea()
    {
        var targets = AreaUse.Pick(State.Map, AreaTiles());
        Session.AutoWalk.Stop();
        var queued = Session.UseQueue.QueueArea(targets, State.Player.Target);
        if (queued > 0)
        {
            PlayClick();
        }

        State.Chat.Add(new LogLine($"Queued {queued} in the area.", (int)TextColour.Green));
        Refresh();
    }

    public override void _Process(double delta)
    {
        if (GlideMovement && glideRecorded)
        {
            PlaceGlided();
        }

        var action = GetViewport().GuiGetHoveredControl() != null
            ? MapAction.Nothing
            : MapActions.Decide(
                State,
                Session,
                PointUnder(GetViewport().GetMousePosition()),
                HeldKeys.Current(),
                MapButton.None,
                SelectedCharacter);
        Input.SetDefaultCursorShape(CursorShapes.For(action.Cursor));
        if (action.HighlightedTile == hover.HighlightedTile && action.HighlightedLayer == hover.HighlightedLayer)
        {
            return;
        }

        var previousTile = hover.HighlightedTile;
        hover = action;
        if (previousTile != NoTile)
        {
            ShowTile(previousTile);
        }

        if (hover.HighlightedTile != NoTile)
        {
            ShowTile(hover.HighlightedTile);
        }
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton { Pressed: false } click)
        {
            return;
        }

        var button = click.ButtonIndex switch
        {
            MouseButton.Left => MapButton.Left,
            MouseButton.Right => MapButton.Right,
            _ => MapButton.None,
        };
        if (button == MapButton.None)
        {
            return;
        }

        if (button == MapButton.Left
            && AutoWalk.ButtonAt((int)click.Position.X, (int)click.Position.Y) is { } walkDirection)
        {
            Session.AutoWalk.Start(walkDirection);
            GetViewport().SetInputAsHandled();
            return;
        }

        var keys = (click.ShiftPressed ? MapKeys.Shift : MapKeys.None)
            | (click.CtrlPressed ? MapKeys.Ctrl : MapKeys.None)
            | (click.AltPressed ? MapKeys.Alt : MapKeys.None);
        var action = MapActions.Decide(State, Session, PointUnder(click.Position), keys, button, SelectedCharacter);
        if (action.Command != null || action.NewSelection != null)
        {
            Session.AutoWalk.Stop();
        }

        if (action.GivesMore)
        {
            Session.GiveMore.Gave(State.Player.HeldItem);
        }

        if (action.Command is { } command)
        {
            SendCommand(command);
            GetViewport().SetInputAsHandled();
        }

        if (action.QueuedUse is { } queuedUse)
        {
            if (Session.UseQueue.Add(queuedUse.X, queuedUse.Y, State.Player.Target) == UseQueueAdd.Queued)
            {
                PlayClick();
            }

            GetViewport().SetInputAsHandled();
        }

        if (action.NewSelection is { } selection)
        {
            SelectedCharacter = selection;
            Refresh();
            GetViewport().SetInputAsHandled();
        }
    }

    public void Refresh()
    {
        var clock = Stopwatch.StartNew();
        var selectedIsShown = false;
        charactersShown.Clear();
        var target = State.Player.Target;
        var markerTiles = new MarkerTiles(NoTile, NoTile, NoTile, NoTile, 0);
        if (queuedTiles.Length != Session.UseQueue.Count)
        {
            queuedTiles = new int[Session.UseQueue.Count];
        }

        Array.Fill(queuedTiles, NoTile);
        FindArea();
        for (var tileIndex = 0; tileIndex < TileCount; tileIndex++)
        {
            selectedIsShown |= ShowTile(tileIndex);
            markerTiles = FindMarkers(markerTiles, tileIndex, target);
            FindQueuedUse(tileIndex);
            ShowEffects(tileIndex);
            ref readonly var tile = ref State.Map.Tiles[tileIndex];
            PlaceCharacter(tileIndex, new Vector2I(tile.CharacterOffsetX, tile.CharacterOffsetY));
            ShowNameplate(tileIndex);
        }

        ReleaseUnusedNameplates();
        PlaceMarkers(markerTiles);
        if (!selectedIsShown)
        {
            SelectedCharacter = 0;
        }

        ShiftMapUnderPlayer();
        LastRefreshMilliseconds = clock.Elapsed.TotalMilliseconds;
    }

    // A tile only loads its texture when its sprite number changes, so a new sprite swap has every tile loaded again.
    public void RedrawSprites()
    {
        foreach (var layer in (SpriteLayer[])[floors, objects, characters])
        {
            Array.Fill(layer.ShownSprites, UnshownSprite);
        }

        Array.Fill(shownShadows, UnshownSprite);
        Refresh();
    }

    public void RecordMovement(int ticks, double tickMilliseconds)
    {
        if (!GlideMovement)
        {
            return;
        }

        glidePoints.Clear();
        for (var tileIndex = 0; tileIndex < TileCount; tileIndex++)
        {
            ref readonly var tile = ref State.Map.Tiles[tileIndex];
            if (characters.ShownSprites[tileIndex] != 0)
            {
                glidePoints[tile.CharacterNumber] = SnappedPoint(tile);
            }
        }

        var player = SnappedPoint(State.Map.Tiles[WorldMap.PlayerTile]);
        glide.Record(player, glidePoints, (long)Time.GetTicksMsec(), ticks, tickMilliseconds);
        glideRecorded = true;
    }

    public TileSprites? SpritesUnder(Vector2 screenPosition)
    {
        if (PointUnder(screenPosition) is not var (column, row))
        {
            return null;
        }

        var tileIndex = (int)column + (int)row * WorldMap.Size;
        return new TileSprites(
            floors.ShownSprites[tileIndex],
            objects.ShownSprites[tileIndex],
            characters.ShownSprites[tileIndex]);
    }

    private static Sprite2D AddSpriteNode(Node2D layer, int column, int row)
    {
        var (x, y) = IsometricProjection.TilePosition(column, row);
        var node = new Sprite2D { Centered = false, Position = new Vector2(x, y), Visible = false };
        layer.AddChild(node);
        return node;
    }

    private static Sprite2D AddShadowNode(Sprite2D characterNode)
    {
        var shadow = new Sprite2D
        {
            Centered = false,
            FlipV = true,
            ShowBehindParent = true,
            Visible = false,
            Modulate = new Color(0, 0, 0, ShadowAlpha),
        };
        characterNode.AddChild(shadow);
        return shadow;
    }

    private void FitWorldCanvasToWindow()
    {
        var virtualSize = GetViewportRect().Size;
        var windowSize = (Vector2)GetTree().Root.Size;
        var scale = Math.Max(
            1,
            (int)Math.Ceiling(Math.Max(windowSize.X / virtualSize.X, windowSize.Y / virtualSize.Y)));
        worldCanvas.Size = (Vector2I)(virtualSize * scale);
        worldCanvas.CanvasTransform = Transform2D.Identity.Scaled(new Vector2(scale, scale));
        worldPicture.Scale = new Vector2(1f / scale, 1f / scale);
    }

    private Node2D AddLayer()
    {
        var layer = new Node2D { Position = mapShift };
        worldCanvas.AddChild(layer);
        return layer;
    }

    private (float Column, float Row)? PointUnder(Vector2 screenPosition)
    {
        var (x, y) = ((int)screenPosition.X, (int)screenPosition.Y);
        var drawnShift = floorLayer.Position.Round();
        return PickTileDrawnUnderMouse
            ? MapPicking.PointAt(x, y, IsometricProjection.BaseMapShiftX - (int)drawnShift.X, -(int)drawnShift.Y)
            : MapPicking.PointAt(x, y);
    }

    // Returns whether the selected character is drawn on this tile.
    private bool ShowTile(int tileIndex)
    {
        var (column, row) = (tileIndex % WorldMap.Size, tileIndex / WorldMap.Size);
        ref readonly var tile = ref State.Map.Tiles[tileIndex];
        var environmentTint = MapSpriteRules.EnvironmentTint(tile);
        Show(
            floors,
            tileIndex,
            MapSpriteRules.FloorSprite(tile, State.ClientTick),
            new SpriteLook(
                tile.Light,
                environmentTint,
                IsHighlighted(tileIndex, MapLayer.Floor),
                areaFloors.Contains(tileIndex)));
        Show(
            objects,
            tileIndex,
            MapSpriteRules.ObjectSprite(tile, column, row, State.Player, HideWalls),
            new SpriteLook(
                tile.Light,
                environmentTint,
                IsHighlighted(tileIndex, MapLayer.Object) || areaGlows.Contains(tileIndex),
                false));
        var character = MapSpriteRules.CharacterSprite(tile, column, row);
        var selected = character != 0 && SelectedCharacter != 0 && tile.CharacterNumber == SelectedCharacter;
        Show(
            characters,
            tileIndex,
            character,
            new SpriteLook(
                tile.Light,
                MapSpriteRules.CharacterTint(tile),
                IsHighlighted(tileIndex, MapLayer.Character),
                selected));
        ShowShadow(tileIndex, character);
        return selected;
    }

    private void ShowShadow(int tileIndex, int characterSprite)
    {
        var spriteNumber = ShowShadows && CharacterShadow.Casts(characterSprite) ? characterSprite : 0;
        if (shownShadows[tileIndex] == spriteNumber)
        {
            return;
        }

        shownShadows[tileIndex] = spriteNumber;
        var shadow = shadowNodes[tileIndex];
        var texture = spriteNumber == 0 ? null : characters.Nodes[tileIndex].Texture;
        shadow.Visible = texture != null;
        if (texture == null)
        {
            return;
        }

        var (left, top, height) = CharacterShadow.For(texture.GetWidth(), texture.GetHeight());
        shadow.Texture = texture;
        shadow.Position = new Vector2(left, top);
        shadow.Scale = new Vector2(1, height / (float)texture.GetHeight());
    }

    private void ShowNameplate(int tileIndex)
    {
        ref readonly var tile = ref State.Map.Tiles[tileIndex];
        var showText = NameplateText.IsShown(ShowNames, ShowHealthPercent);
        if (tile.CharacterNumber == 0 || characters.ShownSprites[tileIndex] == 0 || (!showText && !ShowHealthBars))
        {
            return;
        }

        var name = State.Names.NameOf(tile.CharacterNumber, tile.CharacterId);
        if (!nameplatesByCharacter.TryGetValue(tile.CharacterNumber, out var nameplate))
        {
            nameplate = freeNameplates.Count > 0 ? freeNameplates.Pop() : nameplateScene.Instantiate<Nameplate>();
            nameplatesByCharacter[tile.CharacterNumber] = nameplate;
        }

        var characterNode = characters.Nodes[tileIndex];
        if (nameplate.GetParent() != characterNode)
        {
            nameplate.GetParent()?.RemoveChild(nameplate);
            characterNode.AddChild(nameplate);
        }

        charactersShown.Add(tile.CharacterNumber);
        nameplate.ZIndex = tileIndex == WorldMap.PlayerTile ? PlayerNameplateLayer : 0;
        nameplate.Display(
            NameplateText.For(name, tile.CharacterId, tile.CharacterHealthPercent, ShowNames, ShowHealthPercent),
            NameplateFontFor(tile.CharacterTextColour),
            tile.CharacterHealthPercent,
            tileIndex == WorldMap.PlayerTile ? State.Player.HitPoints[ReservedValue] : 0,
            showText,
            ShowHealthBars);
    }

    private NameplateFont NameplateFontFor(int textColour)
    {
        var font = Fonts.For((TextColour)NameplateText.FontFor(textColour));
        return new NameplateFont(font.Font, font.Size, font.Tint, Fonts.CentresLikeTheCurrentClient);
    }

    // Builder markers, injuries, deaths, graves and magic glows, drawn after the tile's character and its action
    // marker as the current client does.
    private void ShowEffects(int tileIndex)
    {
        var (column, row) = (tileIndex % WorldMap.Size, tileIndex / WorldMap.Size);
        ref readonly var tile = ref State.Map.Tiles[tileIndex];
        var hidden = MapSpriteRules.HidesObjectsAndCharacters(tile, column, row);
        var injury = hidden ? 0 : MapEffects.InjurySprite(tile);
        var death = hidden ? 0 : MapEffects.DeathSprite(tile);
        var tomb = hidden ? 0 : MapEffects.TombSprite(tile);
        var glow = hidden ? null : MagicGlow.For(tile);
        var builderMarkers = hidden ? [] : BuilderMarkers.For(tile);
        var effects = tileEffects[tileIndex];
        if (effects == null)
        {
            if (injury == 0 && death == 0 && tomb == 0 && glow == null && builderMarkers.Count == 0)
            {
                return;
            }

            effects = new TileEffects();
            var (x, y) = IsometricProjection.TilePosition(column, row);
            effects.Root.Position = new Vector2(x, y);
            characters.Nodes[tileIndex].AddSibling(effects.Root);
            tileEffects[tileIndex] = effects;
        }

        ShowBuilderMarkers(effects.BuilderMarkers, builderMarkers);
        var characterOffset = new Vector2(tile.CharacterOffsetX, tile.CharacterOffsetY);
        ShowEffect(effects.Injury, injury, characterOffset);
        ShowEffect(effects.Death, death, MapEffects.DeathFollowsCharacter(tile) ? characterOffset : Vector2.Zero);
        ShowEffect(effects.Tomb, tomb, Vector2.Zero);
        var brightness = SpriteLighting.Brightness(tile.Light, WorldGamma);
        effects.Tomb.SelfModulate = new Color(brightness, brightness, brightness);
        effects.Glow.Visible = glow != null;
        if (glow is { } shown)
        {
            effects.Glow.Material = GlowMaterial(shown);
            effects.Glow.Position = characterOffset + new Vector2(MagicGlow.Left, MagicGlow.Top);
        }
    }

    private ShaderMaterial GlowMaterial(MagicGlow glow)
    {
        if (glowMaterials.TryGetValue(glow, out var material))
        {
            return material;
        }

        glowShader ??= GD.Load<Shader>(MagicGlowShaderPath);
        material = new ShaderMaterial { Shader = glowShader };
        material.SetShaderParameter(
            "glow_colour",
            new Vector3(glow.Red ? 1 : 0, glow.Green ? 1 : 0, glow.Blue ? 1 : 0));
        material.SetShaderParameter("strength", (float)glow.Strength);
        glowMaterials[glow] = material;
        return material;
    }

    private void ShowBuilderMarkers(Node2D container, IReadOnlyList<int> spriteNumbers)
    {
        while (container.GetChildCount() < spriteNumbers.Count)
        {
            container.AddChild(new Sprite2D { Centered = false });
        }

        for (var markerIndex = 0; markerIndex < container.GetChildCount(); markerIndex++)
        {
            ShowEffect(
                container.GetChild<Sprite2D>(markerIndex),
                markerIndex < spriteNumbers.Count ? spriteNumbers[markerIndex] : 0,
                Vector2.Zero);
        }
    }

    private void ShowEffect(Sprite2D node, int spriteNumber, Vector2 position)
    {
        var texture = spriteNumber == 0 ? null : Sprites.GetTexture(spriteNumber);
        node.Visible = texture != null;
        if (texture == null)
        {
            return;
        }

        if (node.Texture != texture)
        {
            var (offsetX, offsetY) = IsometricProjection.SpriteOffset(texture.GetWidth(), texture.GetHeight());
            node.Texture = texture;
            node.Offset = new Vector2(offsetX, offsetY);
        }

        node.Position = position;
    }

    private MarkerTiles FindMarkers(MarkerTiles found, int tileIndex, PlayerTarget target)
    {
        var (column, row) = (tileIndex % WorldMap.Size, tileIndex / WorldMap.Size);
        ref readonly var tile = ref State.Map.Tiles[tileIndex];
        if (found.WalkTile == NoTile && MapMarkers.IsWalkTarget(tile, target))
        {
            found = found with { WalkTile = tileIndex };
        }

        if (MapSpriteRules.HidesObjectsAndCharacters(tile, column, row))
        {
            return found;
        }

        if (found.AttackTile == NoTile && characters.ShownSprites[tileIndex] != 0
            && MapMarkers.IsAttackTarget(tile, target))
        {
            found = found with { AttackTile = tileIndex };
        }

        if (found.GiveTile == NoTile && characters.ShownSprites[tileIndex] != 0
            && MapMarkers.IsGiveTarget(tile, target))
        {
            found = found with { GiveTile = tileIndex };
        }

        if (found.ActionTile == NoTile && MapMarkers.ActionMarker(tile, target) is var actionSprite and not 0)
        {
            found = found with { ActionTile = tileIndex, ActionSprite = actionSprite };
        }

        return found;
    }

    private void PlaceMarkers(MarkerTiles found)
    {
        PlaceOnGround(walkMarker, MapMarkers.WalkTarget, found.WalkTile, floors);
        PlaceOnCharacter(attackMarker, MapMarkers.AttackTarget, found.AttackTile, 1);
        PlaceOnCharacter(
            giveMarker,
            MapMarkers.GiveTarget,
            found.GiveTile,
            found.GiveTile == found.AttackTile ? 2 : 1);
        PlaceOnGround(actionMarker, found.ActionSprite, found.ActionTile, characters);

        // The use in progress shows its own marker. Each number goes in before its green use marker, which then sits
        // between the tile and the number.
        while (queuedUseMarkers.Count < queuedTiles.Length)
        {
            var useMarker = new Sprite2D { Centered = false, Visible = false };
            useMarker.Material = TintMaterial(SpriteTint.None, false, true);
            floorLayer.AddChild(useMarker);
            queuedUseMarkers.Add(useMarker);
            var numberMarker = new Sprite2D { Centered = false, Visible = false };
            floorLayer.AddChild(numberMarker);
            queuedNumberMarkers.Add(numberMarker);
        }

        for (var position = 0; position < queuedUseMarkers.Count; position++)
        {
            var tileIndex = position >= queuedTiles.Length ? NoTile
                : queuedTiles[position] == found.ActionTile ? NoTile : queuedTiles[position];
            PlaceOnGround(queuedNumberMarkers[position], QueuedNumberSprites + position + 1, tileIndex, characters);
            PlaceOnGround(queuedUseMarkers[position], QueuedUseSprite, tileIndex, characters);
        }
    }

    private void FindQueuedUse(int tileIndex)
    {
        if (Session.UseQueue.Count == 0)
        {
            return;
        }

        ref readonly var tile = ref State.Map.Tiles[tileIndex];
        var position = Session.UseQueue.PositionOf(tile.X, tile.Y);
        if (position > 0 && !MapSpriteRules.HidesObjectsAndCharacters(
            tile,
            tileIndex % WorldMap.Size,
            tileIndex / WorldMap.Size))
        {
            queuedTiles[position - 1] = tileIndex;
        }
    }

    // Ground markers are drawn right after the tile's node in its layer, at the tile's own position.
    private void PlaceOnGround(Sprite2D marker, int spriteNumber, int tileIndex, SpriteLayer after)
    {
        if (tileIndex == NoTile || Sprites.GetTexture(spriteNumber) is not { } texture)
        {
            marker.Visible = false;
            return;
        }

        var anchor = after.Nodes[tileIndex];
        if (marker.GetParent() != anchor.GetParent())
        {
            marker.GetParent()?.RemoveChild(marker);
            anchor.GetParent().AddChild(marker);
        }

        anchor.GetParent().MoveChild(marker, anchor.GetIndex() + (marker.GetIndex() < anchor.GetIndex() ? 0 : 1));
        var (x, y) = IsometricProjection.TilePosition(tileIndex % WorldMap.Size, tileIndex / WorldMap.Size);
        var (offsetX, offsetY) = IsometricProjection.SpriteOffset(texture.GetWidth(), texture.GetHeight());
        marker.Texture = texture;
        marker.Position = new Vector2(x, y);
        marker.Offset = new Vector2(offsetX, offsetY);
        marker.Visible = true;
    }

    // Character markers are children of the character's sprite, after its shadow and before its nameplate.
    private void PlaceOnCharacter(Sprite2D marker, int spriteNumber, int tileIndex, int childIndex)
    {
        if (tileIndex == NoTile || Sprites.GetTexture(spriteNumber) is not { } texture)
        {
            marker.Visible = false;
            return;
        }

        var characterNode = characters.Nodes[tileIndex];
        if (marker.GetParent() != characterNode)
        {
            marker.GetParent()?.RemoveChild(marker);
            characterNode.AddChild(marker);
        }

        characterNode.MoveChild(marker, childIndex);
        var (offsetX, offsetY) = IsometricProjection.SpriteOffset(texture.GetWidth(), texture.GetHeight());
        marker.Texture = texture;
        marker.Offset = new Vector2(offsetX, offsetY);
        marker.Visible = true;
    }

    // Pooled nameplates are outside the tree, so nothing else frees them.
    public override void _ExitTree()
    {
        GetTree().Root.SizeChanged -= FitWorldCanvasToWindow;
        while (freeNameplates.TryPop(out var nameplate))
        {
            nameplate.Free();
        }
    }

    private void ReleaseUnusedNameplates()
    {
        foreach (var (characterNumber, nameplate) in nameplatesByCharacter)
        {
            if (charactersShown.Contains(characterNumber))
            {
                continue;
            }

            nameplate.GetParent()?.RemoveChild(nameplate);
            nameplatesByCharacter.Remove(characterNumber);
            freeNameplates.Push(nameplate);
        }
    }

    private bool IsHighlighted(int tileIndex, MapLayer layer) =>
        hover.HighlightedTile == tileIndex && hover.HighlightedLayer == layer;

    private void PlaceCharacter(int tileIndex, Vector2I offset)
    {
        if (characterOffsets[tileIndex] == offset)
        {
            return;
        }

        characterOffsets[tileIndex] = offset;
        var (x, y) = IsometricProjection.TilePosition(tileIndex % WorldMap.Size, tileIndex / WorldMap.Size);
        characters.Nodes[tileIndex].Position = new Vector2(x + offset.X, y + offset.Y);
    }

    private static GlidePoint SnappedPoint(in MapTile tile) =>
        MovementGlide.WorldPoint(tile.X, tile.Y, tile.CharacterOffsetX, tile.CharacterOffsetY);

    // How far behind its latest position something is drawn. Left unrounded, so steps stay smooth on big windows.
    private static Vector2 Lag(GlidePoint shown, in MapTile tile)
    {
        var lag = shown - SnappedPoint(tile);
        return new Vector2(lag.X, lag.Y);
    }

    private void SnapToTicks()
    {
        Array.Fill(characterOffsets, new Vector2I(int.MinValue, int.MinValue));
        mapShift = new Vector2I(int.MinValue, int.MinValue);
        for (var tileIndex = 0; tileIndex < TileCount; tileIndex++)
        {
            if (tileEffects[tileIndex] is { } effects)
            {
                var (x, y) = IsometricProjection.TilePosition(tileIndex % WorldMap.Size, tileIndex / WorldMap.Size);
                effects.Root.Position = new Vector2(x, y);
            }
        }

        Refresh();
    }

    private void PlaceGlided()
    {
        var now = (long)Time.GetTicksMsec();
        var shift = new Vector2(mapShift.X, mapShift.Y) - Lag(glide.Player(now), State.Map.Tiles[WorldMap.PlayerTile]);
        floorLayer.Position = shift;
        objectLayer.Position = shift;

        for (var tileIndex = 0; tileIndex < TileCount; tileIndex++)
        {
            ref readonly var tile = ref State.Map.Tiles[tileIndex];
            if (characters.ShownSprites[tileIndex] == 0 || glide.Character(tile.CharacterNumber, now) is not { } shown)
            {
                continue;
            }

            var lag = Lag(shown, tile);
            var (x, y) = IsometricProjection.TilePosition(tileIndex % WorldMap.Size, tileIndex / WorldMap.Size);
            var snapped = new Vector2(x + tile.CharacterOffsetX, y + tile.CharacterOffsetY);
            characters.Nodes[tileIndex].Position = snapped + lag;
            if (tileEffects[tileIndex] is { } effects)
            {
                effects.Root.Position = new Vector2(x, y) + lag;
            }
        }
    }

    private void ShiftMapUnderPlayer()
    {
        var (x, y) = IsometricProjection.MapShift(State.Map);
        var offset = new Vector2I(x, y);
        if (offset == mapShift)
        {
            return;
        }

        mapShift = offset;
        floorLayer.Position = offset;
        objectLayer.Position = offset;
    }

    private void Show(SpriteLayer layer, int tileIndex, int spriteNumber, SpriteLook look)
    {
        var node = layer.Nodes[tileIndex];
        if (layer.ShownSprites[tileIndex] != spriteNumber)
        {
            layer.ShownSprites[tileIndex] = spriteNumber;
            var texture = spriteNumber == 0 ? null : Sprites.GetTexture(spriteNumber);
            node.Texture = texture;
            node.Visible = texture != null;
            if (texture != null)
            {
                var (offsetX, offsetY) = IsometricProjection.SpriteOffset(texture.GetWidth(), texture.GetHeight());
                node.Offset = new Vector2(offsetX, offsetY);
            }
        }

        var effect = look.Light
            | ((int)look.Tint << TintShift)
            | (look.Highlighted ? HighlightBit : 0)
            | (look.Selected ? SelectedBit : 0);
        if (layer.ShownEffects[tileIndex] == effect)
        {
            return;
        }

        layer.ShownEffects[tileIndex] = effect;
        var needsShader = look.Tint != SpriteTint.None || look.Selected;
        var brightness = SpriteLighting.Brightness(look.Light, WorldGamma);
        if (look.Highlighted && !needsShader)
        {
            brightness *= HighlightBrightness;
        }

        node.SelfModulate = new Color(brightness, brightness, brightness);
        node.Material = needsShader ? TintMaterial(look.Tint, look.Highlighted, look.Selected) : null;
    }

    private ShaderMaterial TintMaterial(SpriteTint tint, bool highlighted, bool selected)
    {
        if (tintMaterials.TryGetValue((tint, highlighted, selected), out var material))
        {
            return material;
        }

        tintShader ??= GD.Load<Shader>(TintShaderPath);
        material = new ShaderMaterial { Shader = tintShader };
        material.SetShaderParameter("grey", tint.HasFlag(SpriteTint.Grey));
        material.SetShaderParameter("infrared", tint.HasFlag(SpriteTint.Infrared));
        material.SetShaderParameter("underwater", tint.HasFlag(SpriteTint.Underwater));
        material.SetShaderParameter("highlight", highlighted);
        material.SetShaderParameter("selected", selected);
        tintMaterials[(tint, highlighted, selected)] = material;
        return material;
    }

    private readonly record struct MarkerTiles(
        int WalkTile,
        int AttackTile,
        int GiveTile,
        int ActionTile,
        int ActionSprite);

    private readonly record struct SpriteLook(int Light, SpriteTint Tint, bool Highlighted, bool Selected);

    private sealed class TileEffects
    {
        public TileEffects()
        {
            Root.AddChild(BuilderMarkers);
            Root.AddChild(Injury);
            Root.AddChild(Death);
            Root.AddChild(Tomb);
            Root.AddChild(Glow);
        }

        public Node2D Root { get; } = new();
        public Node2D BuilderMarkers { get; } = new();
        public Sprite2D Injury { get; } = new() { Centered = false, Visible = false };
        public Sprite2D Death { get; } = new() { Centered = false, Visible = false };
        public Sprite2D Tomb { get; } = new() { Centered = false, Visible = false };

        public ColorRect Glow { get; } = new()
        {
            Size = new Vector2(MagicGlow.Size, MagicGlow.Size),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false,
        };
    }

    private sealed class SpriteLayer
    {
        public Sprite2D[] Nodes { get; } = new Sprite2D[TileCount];
        public int[] ShownSprites { get; } = new int[TileCount];
        public int[] ShownEffects { get; } = new int[TileCount];
    }
}
