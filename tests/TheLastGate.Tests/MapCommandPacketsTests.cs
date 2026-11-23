using TheLastGate.Net;

namespace TheLastGate.Tests;

public class MapCommandPacketsTests
{
    [Fact]
    public void ForTile_PutsTheTilesWorldPositionAfterTheCommand()
    {
        byte[] expected = [(byte)ClientCommand.Move, 0xD2, 0x04, 0x70, 0x11, 0x01, 0x00, 0, 0, 0, 0, 0, 0, 0, 0, 0];

        Assert.Equal(expected, MapCommandPackets.ForTile(ClientCommand.Move, 1234, 70000));
    }

    [Fact]
    public void ForTile_UsesTheSameLayoutForEveryTileCommand()
    {
        byte[] expected = [(byte)ClientCommand.Turn, 0x0A, 0x00, 0x14, 0x00, 0x00, 0x00, 0, 0, 0, 0, 0, 0, 0, 0, 0];

        Assert.Equal(expected, MapCommandPackets.ForTile(ClientCommand.Turn, 10, 20));
    }

    [Fact]
    public void TileOf_ReadsBackWhatForTilePutIn()
    {
        Assert.Equal(
            (1234, 70000),
            MapCommandPackets.TileOf(MapCommandPackets.ForTile(ClientCommand.Use, 1234, 70000)));
    }

    [Fact]
    public void ForCharacter_PutsTheCharactersNumberAfterTheCommand()
    {
        byte[] expected = [(byte)ClientCommand.Look, 0xC1, 0x16, 0x00, 0x00, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

        Assert.Equal(expected, MapCommandPackets.ForCharacter(ClientCommand.Look, 5825));
    }
}
