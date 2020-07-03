using BackEnd.Data;
using BackEnd.Infrastructure;
using ConferenceDTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendeesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AttendeesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{username}")]
        public async Task<ActionResult<AttendeeResponse>> Get(string username)
        {
            var attendee = await _context.Attendees
                .Include(a => a.SessionsAttendees).ThenInclude(sa => sa.Session)
                .SingleOrDefaultAsync(a => a.UserName == username);
            if (attendee == null) return NotFound();
            return attendee.MapAttendeeResponse();
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AttendeeResponse>> Post(ConferenceDTO.Attendee input)
        {
            var existingAttendee = await _context.Attendees
                //.Where(a => a.UserName == input.UserName)
                .FirstOrDefaultAsync(a => a.UserName == input.UserName);

            if (existingAttendee != null) return Conflict(input);

            var attendee = new Data.Attendee
            {
                FirstName = input.FirstName,
                LastName = input.LastName,
                UserName = input.UserName,
                EmailAddress = input.EmailAddress
            };

            _context.Attendees.Add(attendee);
            await _context.SaveChangesAsync();

            var result = attendee.MapAttendeeResponse();

            return CreatedAtAction(nameof(Get), new { username = result.UserName }, result);
        }

        [HttpPost("{username}/session/{sessionID}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<AttendeeResponse>> AddSession(string username, int sessionId)
        {
            var attendee = await _context.Attendees
                .Include(a => a.SessionsAttendees).ThenInclude(sa => sa.Session)
                .SingleOrDefaultAsync(a => a.UserName == username);

            if (attendee == null) return NotFound();

            var session = await _context.Sessions.FindAsync(sessionId);

            if (session == null) return BadRequest();

            attendee.SessionsAttendees.Add(new SessionAttendee
            {
                AttendeeId = attendee.Id,
                SessionId = sessionId
            });

            await _context.SaveChangesAsync();

            return attendee.MapAttendeeResponse();
        }

        [HttpDelete("{username}/session/{sessionId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> RemoveSession(string username, int sessionId)
        {
            var attendee = await _context.Attendees
                .Include(a => a.SessionsAttendees)
                .SingleOrDefaultAsync(a => a.UserName == username);

            if (attendee == null) return NotFound();

            var session = await _context.Sessions.FindAsync(sessionId);

            if (session == null) return BadRequest();

            var sessionAttendee = attendee.SessionsAttendees.FirstOrDefault(sa => sa.SessionId == sessionId);
            attendee.SessionsAttendees.Remove(sessionAttendee);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}