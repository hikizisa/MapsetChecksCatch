﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using MapsetChecksCatch.Helper;
using MapsetParser.objects;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;

namespace MapsetChecksCatch.Checks.General
{
    [Check]
    public class CheckBeatmapSetDistanceCalculation : GeneralCheck
    {
        public static readonly ConcurrentDictionary<string, List<CatchHitObject>> SetBeatmaps = new ConcurrentDictionary<string, List<CatchHitObject>>();

        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Resources",
            Message = "Calculating dash and hyperdash distances.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Calculate all the dash and hyperdash distances of this beatmap and cache it so it can later be used."
                },
                {
                    "Reasoning",
                    @"
                    Calculating the beatmap distances can be a heavy process and should only be calculated once."
                }
            }
        };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>();
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            beatmapSet.beatmaps.ForEach(beatmap =>
            {
                var calculatedBeatmap = BeatmapDistanceCalculator.Calculate(beatmap);
                SetBeatmaps.TryRemove(beatmap.metadataSettings.version, out var catchObjects);
                SetBeatmaps.TryAdd(beatmap.metadataSettings.version, calculatedBeatmap);
            });

            yield break;
        }
    }
}
