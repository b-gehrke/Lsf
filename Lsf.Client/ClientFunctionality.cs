using System.Threading.Tasks;
using Lsf.Models;

namespace Lsf.Client
{
    public static class ClientFunctionality
    {
        public static async Task SetSemester(this LsfHttpClient httpClient, Semester semester)
        {
            var part = semester.SemesterType == SemesterType.Summer ? 1 : 2;
            var url = httpClient.Url(
                $"state=user&type=0&k_semester.semid={semester.Year}{part}&idcol=k_semester.semid&idval={semester.Year}{part}&purge=n&getglobal=semester");

            await httpClient.GetStringAsync(url);
        }
    }
}