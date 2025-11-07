namespace RevoltUltimate.API.Accounts
{
    public class SavedAccount
    {
        public string Username { get; set; }

        // Steam session data
        public string EncryptedSteamCookies { get; set; }

        // GOG session data
        public string EncryptedGOGAccessToken { get; set; }
        public string EncryptedGOGRefreshToken { get; set; }
        public string GogUserId { get; set; }
    }
}