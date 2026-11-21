using System.Numerics;
using GdUnit4;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class GlideTrackTests
{
    private const long Start = 1_000;

    [TestCase]
    public void ANewTrack_ShowsWhereItStarted()
    {
        var track = new GlideTrack(new Vector2(10, 20));

        AssertThat(track.Shown(Start)).IsEqual(new Vector2(10, 20));
        AssertThat(track.Shown(Start + 500)).IsEqual(new Vector2(10, 20));
    }

    [TestCase]
    public void MoveTo_GlidesEvenlyOverTheDuration()
    {
        var track = new GlideTrack(Vector2.Zero);

        track.MoveTo(new Vector2(4, 2), Start, 50);

        AssertThat(track.Shown(Start)).IsEqual(Vector2.Zero);
        AssertThat(track.Shown(Start + 25)).IsEqual(new Vector2(2, 1));
        AssertThat(track.Shown(Start + 50)).IsEqual(new Vector2(4, 2));
    }

    [TestCase]
    public void MoveTo_HoldsStillOnceTheGlideEnds()
    {
        var track = new GlideTrack(Vector2.Zero);

        track.MoveTo(new Vector2(4, 2), Start, 50);

        AssertThat(track.Shown(Start + 400)).IsEqual(new Vector2(4, 2));
    }

    [TestCase]
    public void MoveTo_StartsFromWhereTheTrackIsShownNotWhereItWasHeading()
    {
        var track = new GlideTrack(Vector2.Zero);
        track.MoveTo(new Vector2(4, 0), Start, 50);

        track.MoveTo(new Vector2(8, 0), Start + 25, 50);

        AssertThat(track.Shown(Start + 25)).IsEqual(new Vector2(2, 0));
        AssertThat(track.Shown(Start + 50)).IsEqual(new Vector2(5, 0));
        AssertThat(track.Shown(Start + 75)).IsEqual(new Vector2(8, 0));
    }

    [TestCase]
    public void MoveTo_SnapsOverAJumpLongerThanTheLimit()
    {
        var track = new GlideTrack(Vector2.Zero);

        track.MoveTo(new Vector2(GlideTrack.JumpLimit + 1, 0), Start, 50);

        AssertThat(track.Shown(Start)).IsEqual(new Vector2(GlideTrack.JumpLimit + 1, 0));
    }

    [TestCase]
    public void MoveTo_GlidesAJumpExactlyAtTheLimit()
    {
        var track = new GlideTrack(Vector2.Zero);

        track.MoveTo(new Vector2(GlideTrack.JumpLimit, 0), Start, 50);

        AssertThat(track.Shown(Start + 25)).IsEqual(new Vector2(GlideTrack.JumpLimit / 2, 0));
    }

    [TestCase(0)]
    [TestCase(-10)]
    public void MoveTo_SnapsWhenThereIsNoTimeToGlide(double duration)
    {
        var track = new GlideTrack(Vector2.Zero);

        track.MoveTo(new Vector2(4, 2), Start, duration);

        AssertThat(track.Shown(Start)).IsEqual(new Vector2(4, 2));
    }
}
