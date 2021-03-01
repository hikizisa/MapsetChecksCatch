using System;
using System.Collections.Generic;
using MapsetChecksCatch.Checks.General;
using MapsetChecksCatch.Helper;
using MapsetParser.objects;
using MapsetParser.statics;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;

namespace MapsetChecksCatch.Checks.Compose
{
    [Check]
    public class CheckConsecutiveHyperdash : BeatmapCheck
    {
        private const int ThresholdPlatter = 2;
        private const int ThresholdRain = 4;

        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Category = "Compose",
            Message = "Too many consecutive hyperdashes.",
            Difficulties = new[] { Beatmap.Difficulty.Hard, Beatmap.Difficulty.Insane },
            Modes = new[] { Beatmap.Mode.Catch },
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    <b>Rain</b> : 
                    Basic hyperdashes must not be used more than four times between consecutive fruits. 
                    <br/>
                    <b>Platter</b> : 
                    Basic hyperdashes must not be used more than two times between consecutive fruits."
                },
                {
                    "Reasoning",
                    @"
                    The amount of hyperdashes used in a difficulty should be increasing which each difficulty level.
                    In platters the maximum amount of hyperdashes is set to two because the difficulty is meant to be an introduction to hypers."
                }
            }
        };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                { "Consecutive",
                    new IssueTemplate(Issue.Level.Problem,
                            "{0} Too many consecutive hyperdashes were used and should be at most {1}, currently {2}.",
                            "timestamp - ", "rule amount", "amount")
                        .WithCause(
                            "Too many consecutive hyperdash are used.")
                }
            };
        }

        public IEnumerable<Issue> GetConsecutiveHyperdashIssues(Beatmap beatmap, int count, CatchHitObject lastObject)
        {
            if (count > ThresholdPlatter)
            {
                yield return new Issue(
                    GetTemplate("Consecutive"),
                    beatmap,
                    Timestamp.Get(lastObject.time),
                    ThresholdPlatter,
                    count
                ).ForDifficulties(Beatmap.Difficulty.Hard);
            }

            if (count > ThresholdRain)
            {
                yield return new Issue(
                    GetTemplate("Consecutive"),
                    beatmap,
                    Timestamp.Get(lastObject.time),
                    ThresholdRain,
                    count
                ).ForDifficulties(Beatmap.Difficulty.Insane);
            }
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            CheckBeatmapSetDistanceCalculation.SetBeatmaps.TryGetValue(beatmap.metadataSettings.version, out var catchObjects);

            var count = 0;
            CatchHitObject lastObject = null;
            var issues = new List<Issue>();
            foreach (var currentObject in catchObjects)
            {
                if (currentObject.MovementType == MovementType.HYPERDASH)
                {
                    count++;
                    lastObject = currentObject;
                }
                else
                {
                    issues.AddRange(GetConsecutiveHyperdashIssues(beatmap, count, lastObject));
                    count = 0;
                }

                foreach (var currentObjectExtra in currentObject.Extras)
                {
                    if (currentObjectExtra.MovementType == MovementType.HYPERDASH)
                    {
                        count++;
                        lastObject = currentObject;
                    }
                    else
                    {
                        issues.AddRange(GetConsecutiveHyperdashIssues(beatmap, count, lastObject));
                        count = 0;
                    }
                }
            }

            foreach(var issue in issues)
            {
                yield return issue;
            }
        }

        /**
         * Check if the current object is hypersnapped taking the current objects start point and the end point of the last object.
         *
         * Providing a difficulty level and if the last object was a hyper
         *
         * Allowed dash / hyperdash snapping:
         * Salad = 125
         * Platter = 125 / 62
         * Rain = 62
         */
        private static bool IsHigherSnapped(Beatmap.Difficulty difficulty, CatchHitObject currentObject, CatchHitObject lastObject)
        {
            var ms = currentObject.time - lastObject.time;

            return difficulty switch
            {
                Beatmap.Difficulty.Normal => (ms < 250),
                Beatmap.Difficulty.Hard => (ms < (lastObject.MovementType == MovementType.HYPERDASH ? 250 : 124)),
                Beatmap.Difficulty.Insane => (ms < (124)),
                _ => false
            };
        }
    }
}
