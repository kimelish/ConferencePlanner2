﻿using ConferenceDTO;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FrontEnd.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IApiClient _apiClient;

        public IndexModel(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public IEnumerable<IGrouping<DateTimeOffset?, SessionResponse>> Sessions { get; set; }
        public IEnumerable<(int Offset, DayOfWeek? DayOfWeek)> DayOffsets { get; set; }
        public int CurrentDayOffset { get; set; }

        public async Task OnGet(int day = 0)
        {
            CurrentDayOffset = day;

            var sessions = await _apiClient.GetSessionsAsync();
            var startDate = sessions.Min(s => s.StartTime?.Date);

            DayOffsets = sessions.Select(s => s.StartTime?.Date)
                .Distinct()
                .OrderBy(d => d)
                .Select(day => ((int)Math.Floor((day.Value - startDate)?.TotalDays ?? 0), day?.DayOfWeek))
                .ToList();

            var filterDate = startDate?.AddDays(day);

            Sessions = sessions.Where(s => s.StartTime?.Date == filterDate)
                .OrderBy(s => s.TrackId)
                .GroupBy(s => s.StartTime)
                .OrderBy(g => g.Key);
        }
    }
}