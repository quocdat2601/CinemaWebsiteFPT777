using MovieTheater.ViewModels;

namespace MovieTheater.Service
{
    public interface IScoreService
    {
        int GetCurrentScore(string accountId);
        List<ScoreHistoryViewModel> GetScoreHistory(string accountId, DateTime? fromDate = null, DateTime? toDate = null, string? historyType = null);
    }
}