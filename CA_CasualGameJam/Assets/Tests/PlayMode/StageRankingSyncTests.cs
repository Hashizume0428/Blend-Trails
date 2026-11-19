using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("StageRanking")]
public class StageRankingSyncTests
{
    [UnityTest]
    public IEnumerator OnlyTheHighestPositivePendingScoreIsSent()
    {
        var sent = new List<int>();
        var sync = new StageRankingSync((score, token) => { sent.Add(score); return UniTask.CompletedTask; });
        sync.Submit(0);
        sync.Submit(-1);
        yield return sync.FlushAsync(CancellationToken.None).ToCoroutine();
        Assert.That(sent, Is.Empty);
        sync.Submit(60);
        sync.Submit(61);
        sync.Submit(59);
        yield return sync.FlushAsync(CancellationToken.None).ToCoroutine();
        sync.Submit(61);
        yield return sync.FlushAsync(CancellationToken.None).ToCoroutine();
        CollectionAssert.AreEqual(new[] { 61 }, sent);
    }

    [UnityTest]
    public IEnumerator ClearsDuringARequestRemainPendingAndRequestsNeverOverlap()
    {
        var sent = new List<int>();
        var completion = new UniTaskCompletionSource();
        var sync = new StageRankingSync((score, token) =>
        {
            sent.Add(score);
            return sent.Count == 1 ? completion.Task : UniTask.CompletedTask;
        });
        sync.Submit(1);
        UniTask first = sync.FlushAsync(CancellationToken.None);
        sync.Submit(2);
        yield return sync.FlushAsync(CancellationToken.None).ToCoroutine();
        CollectionAssert.AreEqual(new[] { 1 }, sent);
        completion.TrySetResult();
        yield return first.ToCoroutine();
        yield return sync.FlushAsync(CancellationToken.None).ToCoroutine();
        CollectionAssert.AreEqual(new[] { 1, 2 }, sent);
    }

    [UnityTest]
    public IEnumerator FailedSubmissionsRetryTheNewestScore()
    {
        var attempts = new List<int>();
        var sync = new StageRankingSync((score, token) =>
        {
            attempts.Add(score);
            return attempts.Count == 1 ? UniTask.FromException(new TimeoutException()) : UniTask.CompletedTask;
        });
        sync.Submit(4);
        Exception failure = null;
        yield return sync.FlushAsync(CancellationToken.None).ToCoroutine(exceptionHandler: exception => failure = exception);
        Assert.That(failure, Is.TypeOf<TimeoutException>());
        sync.Submit(5);
        yield return sync.FlushAsync(CancellationToken.None).ToCoroutine();
        yield return sync.FlushAsync(CancellationToken.None).ToCoroutine();
        CollectionAssert.AreEqual(new[] { 4, 5 }, attempts);
    }

    [UnityTest]
    public IEnumerator CancellationKeepsTheScoreForTheNextAttempt()
    {
        var sent = new List<int>();
        var sync = new StageRankingSync((score, token) => { sent.Add(score); return UniTask.CompletedTask; });
        sync.Submit(7);
        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            Exception failure = null;
            yield return sync.FlushAsync(cancellation.Token).ToCoroutine(exceptionHandler: exception => failure = exception);
            Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
        }
        Assert.That(sent, Is.Empty);
        yield return sync.FlushAsync(CancellationToken.None).ToCoroutine();
        CollectionAssert.AreEqual(new[] { 7 }, sent);
    }
}
