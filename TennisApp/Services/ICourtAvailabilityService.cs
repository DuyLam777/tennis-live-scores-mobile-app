using System;
using System.Collections.Generic;
using TennisApp.Models;

namespace TennisApp.Services
{
    public interface ICourtAvailabilityService
    {
        event EventHandler<List<CourtItem>> CourtAvailabilityChanged;
        List<CourtItem> GetCurrentCourts();
        Task StartListeningForCourtUpdatesAsync();
        Task StopListeningForCourtUpdatesAsync();
    }
}