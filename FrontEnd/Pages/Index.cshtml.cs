using ConferenceDTO;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FrontEnd.Pages
{
    public class IndexModel : PageModel
    {
        protected readonly IApiClient _apiClient;

        public IndexModel(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public IEnumerable<IGrouping<DateTimeOffset?, SessionResponse>> Sessions { get; set; }
        public IEnumerable<(int Offset, DayOfWeek? DayOfWeek)> DayOffsets { get; set; }
        public int CurrentDayOffset { get; set; }
        public bool IsAdmin { get; set; }

        [TempData]
        public string Message { get; set; }

        public bool ShowMessage => !string.IsNullOrEmpty(Message);

        public List<int> UserSessions { get; set; } = new List<int>();

        protected virtual Task<List<SessionResponse>> GetSessionsAsync()
        {
            return _apiClient.GetSessionsAsync();
        }

        public async Task OnGetAsync(int day = 0)
        {
            CurrentDayOffset = day;

            if (User.Identity.IsAuthenticated)
            {
                var userSessions = await _apiClient.GetSessionsByAttendeeAsync(User.Identity.Name);
                UserSessions = userSessions.Select(u => u.Id).ToList();
            }

            IsAdmin = User.IsAdmin();

            var sessions = await GetSessionsAsync();
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

        public async Task<IActionResult> OnPostAsync(int sessionId)
        {
            await _apiClient.AddSessionToAttendeeAsync(User.Identity.Name, sessionId);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveAsync(int sessionId)
        {
            await _apiClient.RemoveSessionFromAttendeeAsync(User.Identity.Name, sessionId);

            return RedirectToPage();
        }
    }
}