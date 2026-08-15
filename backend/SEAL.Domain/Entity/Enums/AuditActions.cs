namespace SEAL_Domain.Entity.Enums
{
    public static class AuditActions
    {
        public const string DisqualifyTeam = "DisqualifyTeam";
        public const string CalculateRoundResults = "CalculateRoundResults";
        public const string PublishRoundResults = "PublishRoundResults";
        public const string UnpublishRoundResults = "UnpublishRoundResults";
        public const string SaveScoreSubmitted = "SaveScoreSubmitted";
        public const string AssignTemplateToTrack = "AssignTemplateToTrack";
    }

    public static class AuditEntityTypes
    {
        public const string Team = "Team";
        public const string Round = "Round";
        public const string Score = "Score";
        public const string Track = "Track";
    }
}
