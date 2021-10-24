namespace Lsf.Grading.Services
{
    public record Config
    {
        public string? UserName { get; }
        public string? Password { get;  }
        public string? LoginCookie { get; }
        public string BaseUrl { get; } = null!;
        public string SaveFile { get; } = "gradingresults.json";
    }
}