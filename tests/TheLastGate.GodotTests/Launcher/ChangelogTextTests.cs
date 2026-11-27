using GdUnit4;
using TheLastGate.Assets;
using TheLastGate.Launcher;
using TheLastGate.Updates;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Launcher;

[TestSuite]
[RequireGodotRuntime]
public class ChangelogTextTests
{
    private const string Notes =
        "## [1.5.0](https://example.com) (2026-11-20)\n\n### Windows\n\n* The launcher has the [new] look.\n";

    private static readonly AvailableRelease Release = new(new Version(1, 5, 0), Notes, 100);

    [TestCase]
    public void AVersionHeading_IsWhite() =>
        AssertThat(ChangelogText.Bbcode([Release]))
            .Contains($"[color={Hex(TextColour.White)}]Version 1.5.0 (2026-11-20)[/color]");

    [TestCase]
    public void ASectionName_IsGrey() =>
        AssertThat(ChangelogText.Bbcode([Release])).Contains($"[color={Hex(TextColour.Grey)}]Windows[/color]");

    [TestCase]
    public void AReleasesLines_AreYellowWithTheirBracketsKept() =>
        AssertThat(ChangelogText.Bbcode([Release]))
            .Contains($"[color={Hex(TextColour.Yellow)}]- The launcher has the [lb]new] look.[/color]");

    [TestCase]
    public void NoReleases_GiveOneGreyLine() =>
        AssertThat(ChangelogText.Bbcode([]))
            .IsEqual($"[color={Hex(TextColour.Grey)}]You have the latest version.[/color]");

    [TestCase]
    public void NotesToShow_AreTheNewerReleasesWhenThereAreAny()
    {
        var catalog = new UpdateCatalog(new Version(1, 4, 0), [Release, Older("1.4.0")]);

        AssertThat(catalog.NotesToShow.Select(release => release.Version)).ContainsExactly(new Version(1, 5, 0));
    }

    [TestCase]
    public void NotesToShow_AreTheInstalledVersionsWhenUpToDate()
    {
        var catalog = new UpdateCatalog(new Version(1, 5, 0), [Release, Older("1.4.0")]);

        AssertThat(catalog.NotesToShow.Select(release => release.Version)).ContainsExactly(new Version(1, 5, 0));
    }

    [TestCase]
    public void NotesToShow_AreEmptyWhenTheInstalledVersionIsNotListed() =>
        AssertThat(new UpdateCatalog(new Version(1, 6, 0), [Release]).NotesToShow).IsEmpty();

    private static AvailableRelease Older(string version) => new(Version.Parse(version), "", 100);

    private static string Hex(TextColour colour) => "#" + ClassicFonts.SavedCaption(colour).Tint.ToHtml(false);
}
