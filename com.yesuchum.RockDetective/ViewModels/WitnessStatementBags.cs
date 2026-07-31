namespace com.yesuchum.RockDetective.ViewModels
{
    public class WitnessStatementInitBag
    {
        public string ParticipantIdKey { get; set; }

        public string CaseNumber { get; set; }

        public string CaseTitle { get; set; }

        public string RequestPrompt { get; set; }

        public string RequestorName { get; set; }

        public bool CanSubmit { get; set; }

        public bool IsAlreadySubmitted { get; set; }

        public bool IsDeclined { get; set; }

        public string ErrorMessage { get; set; }
    }
}
