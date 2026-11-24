using TheLastGate.Items;
using TheLastGate.State;

namespace TheLastGate.Tests;

public class LookCaptureTests
{
    private static readonly LogLine Header = new("Steel Sword:", (int)LogFont.Lime);
    private static readonly LogLine Description = new("A finely crafted steel sword.", (int)LogFont.Yellow);
    private static readonly LogLine Chatter = new("Bob: hello", (int)LogFont.Blue);
    private readonly LookCapture capture;

    private readonly List<LookReply> replies = [];

    public LookCaptureTests() => capture = new LookCapture(replies.Add);

    [Fact]
    public void Offer_HidesBothRepliesToABackgroundLookAndResolvesTheirSharedLines()
    {
        capture.ExpectFetch(0);

        Assert.True(capture.Offer(Header));
        Assert.True(capture.Offer(Description));
        Assert.True(capture.Offer(Header));
        Assert.True(capture.Offer(Description));
        capture.EndOfTick();

        var reply = Assert.Single(replies);
        Assert.Equal([Header, Description], reply.Shared);
        Assert.Empty(reply.Released);
    }

    [Fact]
    public void Offer_ReleasesOtherChatCaughtAfterAReply()
    {
        capture.ExpectFetch(0);

        capture.Offer(Header);
        capture.Offer(Description);
        capture.Offer(Chatter);
        capture.Offer(Header);
        capture.Offer(Description);
        capture.EndOfTick();

        var reply = Assert.Single(replies);
        Assert.Equal([Header, Description], reply.Shared);
        Assert.Equal([Chatter], reply.Released);
    }

    [Fact]
    public void Offer_LetsLinesBeforeAReplyThrough()
    {
        capture.ExpectFetch(0);

        Assert.False(capture.Offer(Chatter));
    }

    [Fact]
    public void Offer_LetsAReplyNobodyAskedForThrough()
    {
        Assert.False(capture.Offer(Header));
        Assert.False(capture.Offer(Description));
    }

    [Fact]
    public void Offer_LetsAPlayersOwnLookThroughToChat()
    {
        capture.ExpectManual(0);

        Assert.False(capture.Offer(Header));
        Assert.False(capture.Offer(Description));
        Assert.False(capture.HasPending);
    }

    [Fact]
    public void Offer_MatchesRepliesToRequestsInTheOrderTheyWereSent()
    {
        capture.ExpectManual(0);
        capture.ExpectFetch(0);

        Assert.False(capture.Offer(Header));
        Assert.True(capture.Offer(Header));
        Assert.True(capture.Offer(Header));
    }

    [Fact]
    public void Offer_CapturesEmptyLinesInsideAReply()
    {
        var blank = new LogLine("", (int)LogFont.Yellow);
        capture.ExpectFetch(0);

        capture.Offer(Header);
        Assert.True(capture.Offer(blank));
        capture.Offer(Header);
        capture.Offer(blank);
        capture.EndOfTick();

        Assert.Equal([Header, blank], Assert.Single(replies).Shared);
    }

    [Fact]
    public void Offer_CapturesAReplyOfAnyLength()
    {
        const int longReplyLines = 64;
        var reply = Enumerable.Range(0, longReplyLines)
            .Select(line => line == 0 ? Header : new LogLine($"Row {line}", (int)LogFont.Yellow))
            .ToList();
        capture.ExpectFetch(0);

        foreach (var line in reply.Concat(reply))
        {
            Assert.True(capture.Offer(line));
        }

        capture.EndOfTick();

        Assert.Equal(reply, Assert.Single(replies).Shared);
    }

    [Fact]
    public void EndOfTick_EndsTheReplyBeingCaptured()
    {
        capture.ExpectFetch(0);
        capture.Offer(Header);
        capture.EndOfTick();

        Assert.False(capture.Offer(Chatter));
    }

    [Fact]
    public void EndOfTick_WaitsForBothReplies()
    {
        capture.ExpectFetch(0);
        capture.Offer(Header);
        capture.EndOfTick();

        Assert.Empty(replies);
        Assert.True(capture.HasPending);
    }

    [Fact]
    public void EndOfTick_ReleasesEverythingWhenTheRepliesShareNothing()
    {
        var otherHeader = new LogLine("Bronze Sword:", (int)LogFont.Lime);
        capture.ExpectFetch(0);

        capture.Offer(Header);
        capture.Offer(otherHeader);
        capture.EndOfTick();

        var reply = Assert.Single(replies);
        Assert.Empty(reply.Shared);
        Assert.Equal([Header, otherHeader], reply.Released);
    }

    [Fact]
    public void Abandon_ReleasesWhatWasCaughtAndForgetsTheRequests()
    {
        capture.ExpectFetch(0);
        capture.Offer(Header);
        capture.Offer(Chatter);

        var released = capture.Abandon();

        Assert.Equal([Header, Chatter], released);
        Assert.False(capture.HasPending);
        Assert.False(capture.Offer(Header));
        Assert.Empty(replies);
    }

    [Fact]
    public void DropManualsSentBefore_ForgetsLooksThatWereNeverAnswered()
    {
        capture.ExpectManual(0);
        capture.ExpectManual(10);

        capture.DropManualsSentBefore(5);

        Assert.True(capture.HasPending);
        capture.DropManualsSentBefore(11);
        Assert.False(capture.HasPending);
    }

    [Fact]
    public void DropManualsSentBefore_KeepsABackgroundLookWaiting()
    {
        capture.ExpectFetch(0);

        capture.DropManualsSentBefore(100);

        Assert.True(capture.HasPending);
    }
}
