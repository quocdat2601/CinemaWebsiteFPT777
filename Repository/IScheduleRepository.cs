namespace MovieTheater.Repository
{
    public interface IScheduleRepository
    {
        List<string> GetAllScheduleTimes();
        List<DateTime> GetAllShowDates();
    }
}