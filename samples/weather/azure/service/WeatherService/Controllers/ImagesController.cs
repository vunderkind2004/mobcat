﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WeatherService.Controllers
{
    [Route("api/images")]
    [Produces("application/json")]
    [Authorize]
    public class ImagesController : ControllerBase
    {
        const string SearchImageDefaultWeather = "sunny";
        static HttpClient _client;
        readonly IHttpClientFactory _httpClientFactory;
        readonly IConfiguration _configuration;
        readonly ILogger _logger;
        readonly IDistributedCache _cache;

        HttpClient BingImagesSearchHttpClient => _client ?? (_client = _httpClientFactory.CreateClient(ServiceConstants.BingImagesSearchHttpClientIdentifier));

        public ImagesController(IConfiguration config, ILogger<ImagesController> logger, IHttpClientFactory httpClientFactory, IDistributedCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = config;
            _logger = logger;
            _cache = cache;
        }

        [HttpGet("{name}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> BackgroundByCityAndWeather(string name, [FromQuery]string country = default(string),[FromQuery]string weather = default(string))
        {
            name = name.ToLower().Trim();

            if (string.IsNullOrWhiteSpace(weather))
                weather = SearchImageDefaultWeather;
            else
                weather = weather.ToLower().Trim();

            if (!string.IsNullOrWhiteSpace(country))
                country = country.ToUpper().Trim();

            var searchName = !string.IsNullOrWhiteSpace(country) ? $"{name},{country}" : name;

            object responseObject = null;

            try
            {
                string json = string.Empty;
                var cacheName = !string.IsNullOrWhiteSpace(country) ? $"{name}-{country}" : name;
                var cacheKey = $"{ServiceConstants.CachePrefixImages}-{cacheName}-{weather}";
                var cachedResponse = await _cache.GetStringAsync(cacheKey);

                if (!string.IsNullOrWhiteSpace(cachedResponse))
                    json = cachedResponse;
                else
                    json = await BingImagesSearchHttpClient.GetStringAsync($"?q={searchName}+{weather}+weather&count=1&size=large&market=en-us");

                if (string.IsNullOrWhiteSpace(cachedResponse) && !string.IsNullOrWhiteSpace(json))
                    await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) });

                var jo = JObject.Parse(json);

                var imageResult = jo["value"]?[0];

                if (imageResult == null)
                    return new ContentResult()
                    {
                        StatusCode = (int)HttpStatusCode.InternalServerError,
                        Content = "no images found..."
                    };

                responseObject = new
                {
                    imageUrl = imageResult.Value<string>("contentUrl"),
                    name = imageResult.Value<string>("name"),
                    width = imageResult.Value<string>("width"),
                    height = imageResult.Value<string>("height")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }            

            return new JsonResult(responseObject);
        }
    }
}