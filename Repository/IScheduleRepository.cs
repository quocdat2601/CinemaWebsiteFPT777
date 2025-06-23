using MovieTheater.Models;
using MovieTheater.ViewModels;
using System;
using System.Collections.Generic;

namespace MovieTheater.Repository
{
    public interface IScheduleRepository
    {
        public List<Schedule> GetAllScheduleTimes();
        public AvailableSchedulesViewModel GetAvailableScheduleTimes(int cinemaRoomId, DateOnly showDate, int movieDurationMinutes, int cleaningTimeMinutes);
    }
}