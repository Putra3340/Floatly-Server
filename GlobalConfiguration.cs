using DotNetEnv;

namespace Floaty_Music
{
    public static class GlobalConfiguration
    {
        public static string ConnectionString { get; set; } = "Data Source=DESKTOP-86R216N;Initial Catalog=FloatlyLib;Integrated Security=True;Encrypt=True;Trust Server Certificate=True";
        public static string WebRootPath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        public static string UploadsFolder { get; set; } = "uploads";
        public static string MusicFilePath { get; set; } = Path.Combine(WebRootPath, UploadsFolder, "music");
        public static string LyricsFilePath { get; set; } = Path.Combine(WebRootPath, UploadsFolder, "lyrics");
        public static string CoverImagePath { get; set; } = Path.Combine(WebRootPath, UploadsFolder, "cover");
        public static string BannerImagePath { get; set; } = Path.Combine(WebRootPath, UploadsFolder, "banner");

        public static string ArtistProfilePath { get; set; } = Path.Combine(WebRootPath, UploadsFolder, "artist");
        public static string AlbumCoverPath { get; set; } = Path.Combine(WebRootPath, UploadsFolder, "album");

        public static string ADMIN_USERNAME;
        public static string ADMIN_PASSWORD;
        public static string SMTP_SERVER;
        public static string SMTP_PORT;
        public static string SMTP_EMAIL;
        public static string SMTP_PASSWORD;
        public static string TOKEN_EXPIRED_IN_DAYS;
        public static string ServerStatus;
        public static string ServerDetail;

        public static bool isSQLSERVER = false;
        public static bool isSQLITE = false;
        public static bool isMySQL = false;
        public static void LoadConfig()
        {
            Env.Load();
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
            string dbtype = Env.GetString("DATABASE_TYPE", "SQLSERVER");
            if (dbtype.ToUpper() == "SQLITE")
            {
                isSQLITE = true;
            }else if(dbtype.ToUpper() == "SQLSERVER")
            {
                isSQLSERVER = true;
            }
            else if(dbtype.ToUpper() == "MYSQL")
            {
                isMySQL = true;
            }
            else
            {
                Console.WriteLine("Invalid DATABASE_TYPE, must be SQLITE or SQLSERVER, defaulting to SQLSERVER");
                isSQLSERVER = true;
            }

            if (!Directory.Exists(WebRootPath))
                Directory.CreateDirectory(WebRootPath);
            if (!Directory.Exists(Path.Combine(WebRootPath, UploadsFolder)))
                Directory.CreateDirectory(Path.Combine(WebRootPath, UploadsFolder));
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
        }
        public static string SaveFile(IFormFile file, string folder)
        {
            if (file == null) return null;
            var fileName = Path.GetRandomFileName() + Path.GetExtension(file.FileName);
            var relativePath = Path.Combine("uploads", folder, fileName).Replace("\\", "/");
            var absolutePath = Path.Combine(WebRootPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            using var stream = new FileStream(absolutePath, FileMode.Create);
            file.CopyTo(stream);
            return "/" + relativePath;
        }
    }
}
