using GdUnit4;
using TheLastGate.Planner;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Planner;

[TestSuite]
public class FocusJobTests
{
    private readonly List<Action> held = [];
    private readonly List<(int Input, CancellationToken Cancel)> searched = [];
    private FocusJob<int, string> job = null!;

    [BeforeTest]
    public void StartAJob()
    {
        held.Clear();
        searched.Clear();
        job = new FocusJob<int, string>(Search, held.Add);
    }

    public static void RunNow(Action search) => search();

    [TestCase]
    public void RunNow_AnswersAtOnce()
    {
        var now = new FocusJob<int, string>(Search, RunNow);

        AssertThat(now.For(3)).IsEqual("answer 3");
    }

    [TestCase]
    public void ASearchStillRunning_HasNoAnswerYet()
    {
        AssertThat(job.For(3)).IsNull();
        AssertThat(job.TakeFinished()).IsFalse();
    }

    [TestCase]
    public void AFinishedSearch_IsAnsweredAndReportedOnce()
    {
        job.For(3);

        RunHeld();

        AssertThat(job.TakeFinished()).IsTrue();
        AssertThat(job.TakeFinished()).IsFalse();
        AssertThat(job.For(3)).IsEqual("answer 3");
    }

    [TestCase]
    public void AskingAgainWhileItRuns_StartsNoSecondSearch()
    {
        job.For(3);
        job.For(3);

        AssertThat(held).HasSize(1);
    }

    [TestCase]
    public void ANewerInput_CancelsTheOlderSearchAndDropsItsAnswer()
    {
        job.For(3);
        job.For(4);

        held[0]();

        AssertThat(job.TakeFinished()).IsFalse();
        AssertThat(job.For(4)).IsNull();
        held[1]();
        AssertThat(searched[0].Cancel.IsCancellationRequested).IsTrue();
        AssertThat(job.For(4)).IsEqual("answer 4");
        AssertThat(job.For(3)).IsNull();
    }

    [TestCase]
    public void ASearchOnATask_IsAnsweredOnceItFinishes()
    {
        var onATask = new FocusJob<int, string>(Search, search => Task.Run(search));

        onATask.For(3);

        AssertThat(SpinWait.SpinUntil(onATask.TakeFinished, TimeSpan.FromSeconds(5))).IsTrue();
        AssertThat(onATask.For(3)).IsEqual("answer 3");
    }

    [TestCase]
    public void ASearchThatFails_ThrowsWhenItsAnswerIsAskedFor()
    {
        var failing = new FocusJob<int, string>((_, _) => throw new InvalidOperationException("broken"), held.Add);
        failing.For(3);

        RunHeld();

        AssertThat(failing.TakeFinished()).IsTrue();
        AssertThrown(() => failing.For(3)).IsInstanceOf<InvalidOperationException>().HasMessage("broken");
    }

    private string Search(int input, CancellationToken cancel)
    {
        searched.Add((input, cancel));
        return $"answer {input}";
    }

    private void RunHeld()
    {
        foreach (var search in held.ToArray())
        {
            search();
        }
    }
}
