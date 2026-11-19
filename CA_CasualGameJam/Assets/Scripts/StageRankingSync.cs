using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>Coalesces clear counts and keeps failed scores pending until a later attempt.</summary>
public sealed class StageRankingSync
{
    private readonly Func<int, CancellationToken, UniTask> _sendScore;
    private int _pendingScore;
    private int _sentScore;
    private bool _sending;

    public StageRankingSync(Func<int, CancellationToken, UniTask> sendScore)
    {
        _sendScore = sendScore ?? throw new ArgumentNullException(nameof(sendScore));
    }

    public void Submit(int clearedStageCount)
    {
        _pendingScore = Math.Max(_pendingScore, clearedStageCount);
    }

    public async UniTask FlushAsync(CancellationToken cancellationToken)
    {
        if (_sending || _pendingScore <= _sentScore) return;
        cancellationToken.ThrowIfCancellationRequested();
        int score = _pendingScore;
        _sending = true;
        try
        {
            await _sendScore(score, cancellationToken);
            _sentScore = score;
        }
        finally { _sending = false; }
    }
}
