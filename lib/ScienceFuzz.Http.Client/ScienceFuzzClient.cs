﻿using ScienceFuzz.Http.Client.Extensions;
using ScienceFuzz.Models.Shared;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ScienceFuzz.Http.Client
{
    public class ScienceFuzzClient
    {
        private readonly HttpClient _http;
        private readonly string _uriBase;

        public ScienceFuzzClient(HttpClient http, string uriBase)
        {
            _http = http;
            _uriBase = uriBase;
        }

        public async Task<string[]> GetScientistNames()
        {
            var scientistNames = await _http.GetJsonAsync<string[]>($"{_uriBase}/Scientists/Names");
            return scientistNames;
        }

        public async Task<IEnumerable<PublicationModel>> GetScientistPublications(string scientistName)
        {
            var publications = await _http.GetJsonAsync<IEnumerable<PublicationModel>>($"{_uriBase}/Scientists/{scientistName}/Publications");
            return publications;
        }

        public async Task<IEnumerable<ContributionModel>> GetDisciplineContributions(string scientistName)
        {
            var disciplineContributions = await _http.GetJsonAsync<IEnumerable<ContributionModel>>($"{_uriBase}/Scientists/{scientistName}/Disciplines/Contributions");
            return disciplineContributions;
        }

        public async Task<IEnumerable<ContributionModel>> GetDomainContributions(string scientistName)
        {
            var domainContributions = await _http.GetJsonAsync<IEnumerable<ContributionModel>>($"{_uriBase}/Scientists/{scientistName}/Domains/Contributions");
            return domainContributions;
        }

        public async Task<IEnumerable<TsneModel>> GetTsne()
        {
            var tsne = await _http.GetJsonAsync<IEnumerable<TsneModel>>($"{_uriBase}/Scientists/Tsne");
            return tsne;
        }

        public async Task<IEnumerable<KmeansModel>> GetKmeans()
        {
            var kmeans = await _http.GetJsonAsync<IEnumerable<KmeansModel>>($"{_uriBase}/Scientists/Kmeans");
            return kmeans;
        }
    }
}
