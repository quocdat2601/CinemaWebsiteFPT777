using MovieTheater.Models;
using System;
using System.Collections.Generic;

namespace MovieTheater.ViewModels
{
    public class ShowtimeManagementViewModel
    {
        public DateOnly SelectedDate { get; set; }
        public List<DateOnly> AvailableDates { get; set; } = new List<DateOnly>();
        public List<Schedule> AvailableSchedules { get; set; } = new List<Schedule>();
        public List<MovieShow> MovieShows { get; set; } = new List<MovieShow>();
    }
} 