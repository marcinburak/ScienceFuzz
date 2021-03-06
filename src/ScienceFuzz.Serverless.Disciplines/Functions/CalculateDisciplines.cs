using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.WindowsAzure.Storage.Table;
using ScienceFuzz.Data;
using ScienceFuzz.Models.Shared;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ScienceFuzz.Serverless.Disciplines.Functions
{
    // TODO: Refactor
    // TODO: 404
    public static class CalculateDisciplines
    {
        [StorageAccount(ENV.STORAGE_CONNECTION)]
        [FunctionName(nameof(CalculateDisciplines))]
        public static async Task<IEnumerable<ContributionModel>> ExecuteAsync(
            [HttpTrigger(AuthorizationLevel.Function, HTTP.GET, Route = "Scientists/{scientistName}/Disciplines/Contributions")] HttpRequest httpRequest,
            [Table(CONST.STORAGE_TABLE_NAMES.PUBLICATIONS, Connection = ENV.STORAGE_CONNECTION)] CloudTable publicationsTable,
            [Table(CONST.STORAGE_TABLE_NAMES.DISCIPLINE_CONTRIBUTIONS, Connection = ENV.STORAGE_CONNECTION)] CloudTable disciplinesTable,
            string scientistName,
            Binder binder)
        {
            IEnumerable<string> disciplineNames;
            using (var stream = await binder.BindAsync<Stream>(new BlobAttribute($"{CONST.STORAGE_CONTAINER_NAMES.DISCIPLINES}/{CONST.FILE_NAMES.DISCIPLINE_LIST}", FileAccess.Read)))
            using (TextReader t = new StreamReader(stream))
            {
                disciplineNames = t.ReadToEnd().Split("\r\n");
            }

            var contributions = disciplineNames.Select(x => new ContributionModel
            {
                Name = x,
                Value = 0
            }).ToList();

            var publicationsQuery = new TableQuery<Publication>()
                .Where(TableQuery.GenerateFilterCondition(nameof(Publication.PartitionKey), QueryComparisons.Equal, scientistName))
                .Select(new string[] { nameof(Publication.RowKey), nameof(Publication.Count) });
            var publicationsQueryResult = await publicationsTable.ExecuteQuerySegmentedAsync(publicationsQuery, null);
            var publications = publicationsQueryResult.Results.Select(x => x as Publication).ToList();

            foreach (var publication in publications)
            {
                var disciplineNamesQuery = new TableQuery<DisciplineContribution>()
                    .Where(TableQuery.GenerateFilterCondition(nameof(DisciplineContribution.PartitionKey), QueryComparisons.Equal, publication.RowKey))
                    .Select(new string[] { nameof(DisciplineContribution.RowKey) });
                var disciplineNamesQueryResult = await disciplinesTable.ExecuteQuerySegmentedAsync(disciplineNamesQuery, null);
                var publicationDisciplineNames = disciplineNamesQueryResult.Results.Select(x => x.RowKey).ToList();

                foreach (var disciplineName in publicationDisciplineNames)
                {
                    var contribution = contributions.First(y => y.Name == disciplineName);
                    for (int i = 0; i < publication.Count; i++)
                    {
                        contribution.Value = S(contribution.Value, 0.001);
                    }
                }
            }

            return contributions;
        }



        private static double S(double x, double y)
        {
            return x + y - x * y;
        }
    }
}
