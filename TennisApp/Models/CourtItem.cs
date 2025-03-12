using System;

namespace TennisApp.Models
{
    public class CourtItem
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsAvailable { get; set; }
    }
}
