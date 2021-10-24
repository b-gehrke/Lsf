namespace Lsf.Grading.Services
{
    public record Config
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? LoginCookie { get; set;  }
        public string BaseUrl { get; set; }
        public string SaveFile { get; set;  } = "gradingresults.json";
    }
}