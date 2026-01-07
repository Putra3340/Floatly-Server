using DotNetEnv;

namespace Floaty_Music
{
    public static class GlobalConfiguration
    {
        // Folder Names Must be Absolute Names
        public static string ConnectionString { get; set; } = "Data Source=DESKTOP-86R216N;Initial Catalog=FloatlyLib;Integrated Security=True;Encrypt=True;Trust Server Certificate=True";
        public static string WebRootPath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        public static string MusicFilePath { get; set; } = Path.Combine(WebRootPath, "uploads", "music");
        public static string LyricsFilePath { get; set; } = Path.Combine(WebRootPath, "uploads", "lyrics");
        public static string CoverImagePath { get; set; } = Path.Combine(WebRootPath, "uploads", "cover");
        public static string BannerImagePath { get; set; } = Path.Combine(WebRootPath, "uploads", "banner");
        public static string VideoPath { get; set; } = Path.Combine(WebRootPath, "uploads", "video");
        public static string YoutubePath { get; set; } = Path.Combine(WebRootPath, "uploads", "yt");

        public static string ArtistProfilePath { get; set; } = Path.Combine(WebRootPath, "uploads", "artist");
        public static string AlbumCoverPath { get; set; } = Path.Combine(WebRootPath, "uploads", "album");

        public static string ADMIN_USERNAME;
        public static string ADMIN_PASSWORD;
        public static string SMTP_SERVER;
        public static string SMTP_PORT;
        public static string SMTP_EMAIL;
        public static string SMTP_PASSWORD;
        public static string TOKEN_EXPIRED_IN_DAYS;
        public static string ServerStatus;
        public static string ServerDetail;
        public static string ServerKey;
        public static string ClientKey;

        public static bool isSQLSERVER = false;
        public static bool isSQLITE = false;
        public static bool isMySQL = false;
        public static void LoadConfig()
        {
            Env.Load();
            ServerKey = Env.GetString("SERVER_KEY", "FLOATLY_DEFAULT_SERVER_KEY");
            ClientKey = Env.GetString("CLIENT_KEY", "FLOATLY_DEFAULT_CLIENT_KEY");
            ConnectionString = Env.GetString("FLOATLY_CONNECTION", ConnectionString);
            ADMIN_USERNAME = Env.GetString("ADMIN_USERNAME", "admin");
            ADMIN_PASSWORD = Env.GetString("ADMIN_PASSWORD", "password");
            SMTP_SERVER = Env.GetString("SMTP_SERVER", "smtp.gmail.com");
            SMTP_PORT = Env.GetString("SMTP_PORT", "587");
            SMTP_EMAIL = Env.GetString("SMTP_EMAIL", "");
            SMTP_PASSWORD = Env.GetString("SMTP_PASSWORD", "");
            TOKEN_EXPIRED_IN_DAYS = Env.GetString("TOKEN_EXPIRED_IN_DAYS", "");
            ServerStatus = Env.GetString("SERVER_MESSAGE", "");
            ServerDetail = Env.GetString("SERVER_DETAIL", "");

            if (!Directory.Exists(WebRootPath))
                Directory.CreateDirectory(WebRootPath);
            if (!Directory.Exists(Path.Combine(WebRootPath, "uploads")))
                Directory.CreateDirectory(Path.Combine(WebRootPath, "uploads"));
            if (!Directory.Exists(MusicFilePath))
                Directory.CreateDirectory(MusicFilePath);
            if (!Directory.Exists(LyricsFilePath))
                Directory.CreateDirectory(LyricsFilePath);
            if (!Directory.Exists(CoverImagePath))
                Directory.CreateDirectory(CoverImagePath);
            if (!Directory.Exists(BannerImagePath))
                Directory.CreateDirectory(BannerImagePath);
            if (!Directory.Exists(ArtistProfilePath))
                Directory.CreateDirectory(ArtistProfilePath);
            if (!Directory.Exists(AlbumCoverPath))
                Directory.CreateDirectory(AlbumCoverPath);
            if (!Directory.Exists(VideoPath))
                Directory.CreateDirectory(VideoPath);
            if(!Directory.Exists(YoutubePath))
                Directory.CreateDirectory(YoutubePath);
        }
    }
}
