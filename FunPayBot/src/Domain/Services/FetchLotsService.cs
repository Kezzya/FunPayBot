using FunPayBot.src.Application.DTOs.Responses;
using FunPayBot.src.Domain.Entities;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FunPayBot.src.Domain.Services
{
 

    public class FetchLotsService
    {
        private readonly HttpClient _pythonApiClient; // Клиент для вызова FastAPI
        private readonly FunPaySettings _funPaySettings; // Настройки с golden_key
        private readonly ILogger<FetchLotsService> _logger;

        public FetchLotsService(IHttpClientFactory httpClientFactory, FunPaySettings funPaySettings, ILogger<FetchLotsService> logger)
        {
            _pythonApiClient = httpClientFactory.CreateClient("PythonAPI");
            _funPaySettings = funPaySettings;
            _logger = logger;
        }

        public async Task<LotResponse[]> GetLotsAsync(int subcategoryId)
        {
            _logger.LogInformation("Fetching lots for subcategory {SubcategoryId}", subcategoryId);

            string url = $"lots/{subcategoryId}?golden_key={_funPaySettings.GoldenKey}";
            var response = await _pythonApiClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("FastAPI error while getting lots: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new Exception($"Failed to get lots for subcategory {subcategoryId}: {errorContent}");
            }

            var lots = await response.Content.ReadFromJsonAsync<LotResponse[]>();
            return lots ?? Array.Empty<LotResponse>();
        }

        public async Task<LotResponse[]> GetLotsByUserIdAsync(int userId, int? subcategoryId)
        {
            _logger.LogInformation("Fetching lots for user ID: {UserId}, subcategory: {SubcategoryId}", userId, subcategoryId);

            List<int> subcategoriesToProcess;

            if (subcategoryId == null || subcategoryId == -1)
            {
                subcategoriesToProcess = await GetUserSubcategoriesAsync(userId);
                _logger.LogInformation("Found {Count} subcategories for user {UserId}", subcategoriesToProcess.Count, userId);
            }
            else
            {
                subcategoriesToProcess = new List<int> { subcategoryId.Value };
            }

            var allLots = new List<LotResponse>();

            foreach (var subcat in subcategoriesToProcess)
            {
                try
                {
                    string url = $"lots-by-user/{subcat}/{userId}?golden_key={_funPaySettings.GoldenKey}";
                    var response = await _pythonApiClient.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError("FastAPI error while getting lots: {StatusCode} - {Error}", response.StatusCode, errorContent);
                        continue;
                    }

                    var userLots = await response.Content.ReadFromJsonAsync<LotResponse[]>();
                    if (userLots != null && userLots.Any())
                    {
                        foreach (var lot in userLots)
                        {
                            // Получить дополнительные детали лота
                            //var lotDetails = await GetLotDetailsAsync(lot.Id);
                            //if (lotDetails != null)
                            //{
                            //    lot.DetailedDescription = lotDetails.DetailedDescription;
                            //}
                            allLots.Add(lot);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching lots for subcategory {SubcategoryId} for user {UserId}", subcat, userId);
                }
            }

            _logger.LogInformation("Found {LotCount} lots total for user ID: {UserId}", allLots.Count, userId);
            return allLots.ToArray();
        }

        private async Task<List<int>> GetUserSubcategoriesAsync(int userId)
        {
            string url = $"get_user_subcategories/{userId}?golden_key={_funPaySettings.GoldenKey}";
            var response = await _pythonApiClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("FastAPI error while getting user subcategories: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new Exception($"Failed to get user subcategories: {errorContent}");
            }

            var subcategories = await response.Content.ReadFromJsonAsync<int[]>();
            return subcategories?.ToList() ?? new List<int>();
        }
    }
}
