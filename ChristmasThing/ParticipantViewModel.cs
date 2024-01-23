namespace ChristmasThing
{
    public class ParticipantViewModel
    {
        private Participant _participant;
        public ParticipantViewModel(Participant participant)
        {
            _participant = participant;
        }

        public string Name => _participant.Name;
        public Guid Id => _participant.Id;
        public Guid? SignificantOtherId => _participant.SignificantOtherId;

    }
}