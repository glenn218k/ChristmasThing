using System;

namespace DataAccessLibrary.Models
{
    public class ParticipantModel
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string SignificantOtherId { get; set; }
        public string Name { get; set; }
    }
}