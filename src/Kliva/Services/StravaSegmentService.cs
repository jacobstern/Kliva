﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Kliva.Helpers;
using Kliva.Models;
using Kliva.Services.Interfaces;

namespace Kliva.Services
{
    public class StravaSegmentService : IStravaSegmentService
    {
        private readonly ISettingsService _settingsService;
        private readonly StravaWebClient _stravaWebClient;

        private readonly ConcurrentDictionary<string, Task<List<SegmentSummary>>> _cachedStarredSegmentsTasks = new ConcurrentDictionary<string, Task<List<SegmentSummary>>>();

        //TODO: Glenn - How long before we invalidate an in memory cached segment? Maybe use MemoryCache? https://msdn.microsoft.com/en-us/library/system.runtime.caching.memorycache(v=vs.110).aspx
        private readonly ConcurrentDictionary<string, Task<Segment>> _cachedSegmentTasks = new ConcurrentDictionary<string, Task<Segment>>();

        public StravaSegmentService(ISettingsService settingsService, StravaWebClient stravaWebClient)
        {
            _settingsService = settingsService;
            _stravaWebClient = stravaWebClient;
        }

        private async Task<Segment> GetSegmentFromServiceAsync(string segmentId)
        {
            try
            {
                var accessToken = await _settingsService.GetStoredStravaAccessTokenAsync();
                var defaultDistanceUnitType = await _settingsService.GetStoredDistanceUnitTypeAsync();

                string getUrl = $"{Endpoints.Segment}/{segmentId}?access_token={accessToken}";
                string json = await _stravaWebClient.GetAsync(new Uri(getUrl));

                var segment = Unmarshaller<Segment>.Unmarshal(json);
                StravaService.SetMetricUnits(segment, defaultDistanceUnitType);

                return segment;
            }
            catch (Exception ex)
            {
                //TODO: Glenn - Use logger to log errors ( Google )
            }

            return null;
        }

        /// <summary>
        /// Gets all the starred segments of an Athlete.
        /// </summary>
        /// <returns>A list of segments that are starred by the athlete.</returns>
        private async Task<List<SegmentSummary>> GetStarredSegmentsFromServiceAsync(string athleteId)
        {
            try
            {
                var accessToken = await _settingsService.GetStoredStravaAccessTokenAsync();
                var defaultDistanceUnitType = await _settingsService.GetStoredDistanceUnitTypeAsync();

                string getUrl = $"{Endpoints.Athletes}/{athleteId}/segments/starred?access_token={accessToken}";
                string json = await _stravaWebClient.GetAsync(new Uri(getUrl));

                var segments = Unmarshaller<List<SegmentSummary>>.Unmarshal(json);
                foreach (SegmentSummary segment in segments)
                    StravaService.SetMetricUnits(segment, defaultDistanceUnitType);

                return segments;
            }
            catch (Exception ex)
            {
                //TODO: Glenn - Use logger to log errors ( Google )
            }

            return null;
        }

        public Task<Segment> GetSegmentAsync(string segmentId)
        {
            return _cachedSegmentTasks.GetOrAdd(segmentId, GetSegmentFromServiceAsync);
        }

        public async Task<List<SegmentSummary>> GetStarredSegmentsAsync()
        {
            //TODO: Glenn - Caching?
            try
            {
                var accessToken = await _settingsService.GetStoredStravaAccessTokenAsync();
                var defaultDistanceUnitType = await _settingsService.GetStoredDistanceUnitTypeAsync();

                string getUrl = $"{Endpoints.Starred}?access_token={accessToken}";
                string json = await _stravaWebClient.GetAsync(new Uri(getUrl));

                var segments = Unmarshaller<List<SegmentSummary>>.Unmarshal(json);
                foreach (SegmentSummary segment in segments)
                    StravaService.SetMetricUnits(segment, defaultDistanceUnitType);

                return segments;
            }
            catch (Exception ex)
            {
                //TODO: Glenn - Use logger to log errors ( Google )
            }

            return null;
        }

        public Task<List<SegmentSummary>> GetStarredSegmentsAsync(string athleteId)
        {
            return _cachedStarredSegmentsTasks.GetOrAdd(athleteId, GetStarredSegmentsFromServiceAsync);
        }
    }
}
