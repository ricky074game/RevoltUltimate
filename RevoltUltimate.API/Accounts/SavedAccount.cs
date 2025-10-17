namespace RevoltUltimate.API.Accounts
{
    public class SavedAccount
    {
        public required string Username { get; set; }
        public string? EncryptedSessionId { get; set; }
        public string? EncryptedSteamLoginSecure { get; set; }
    }
}