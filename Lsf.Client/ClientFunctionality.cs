using System.Threading.Tasks;
using Lsf.Models;

namespace Lsf.Client
{
    public static class ClientFunctionality
    {
        public static async Task SetSemester(this LsfClient client, int year, SemesterType semesterType)
        {
            var part = semesterType == SemesterType.Summer ? 1 : 2;
            var url = client.Url(
                $"state=user&type=0&k_semester.semid={year}{part}&idcol=k_semester.semid&idval={year}{part}&purge=n&getglobal=semester");

            await client.GetStringAsync(url);
        }
    }
}