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
    public class CheckConsecutiveDash : BeatmapCheck
    {
        private const int ThresholdSalad = 2;
        private const int ThresholdPlatter = 4;

        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Category = "Compose",
            Message = "Too many consecutive dashes.",
            Difficulties = new[] { Beatmap.Difficulty.Normal, Beatmap.Difficulty.Hard },
            Modes = new[] { Beatmap.Mode.Catch },
            Author = "Riana",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    <b>Platter</b> : 
                    Basic dashes must not be used more than four times between consecutive fruits.
                    <br/>
                    <b>Salad</b> : 
                    Basic dashes must not be used more than two times between consecutive fruits. "
                },
                {
                    "Reasoning",
                    @"
                    The amount of dashes used in a difficulty should be increasing which each difficulty level.
                    In salads the maximum amount of dashes is set to two because the difficulty is meant to be an introduction to dashes."
                }
            }
        };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                { "Consecutive",
                    new IssueTemplate(Issue.Level.Problem,
                            "{0} Too many consecutive dashes were used and should be at most {1}, currently {2}.",
                            "timestamp - ", "rule amount", "amount")
                        .WithCause(
                            "Too many consecutive dashes are used.")
                }
            };
        }

        public IEnumerable<Issue> GetConsecutiveHyperdashIssues(Beatmap beatmap, int count, CatchHitObject lastObject)
        {
            if (count > ThresholdSalad)
            {
                yield return new Issue(
                    GetTemplate("Consecutive"),
                    beatmap,
                    Timestamp.Get(lastObject.time),
                    ThresholdSalad,
                    count
                ).ForDifficulties(Beatmap.Difficulty.Normal);
            }

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
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            CheckBeatmapSetDistanceCalculation.SetBeatmaps.TryGetValue(beatmap.metadataSettings.version, out var catchObjects);

            var count = 0;
            CatchHitObject lastObject = null;
            var issues = new List<Issue>();
            foreach (var currentObject in catchObjects)
            {
                if (currentObject.MovementType == MovementType.DASH)
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
                    if (currentObjectExtra.MovementType == MovementType.DASH)
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
    }
}
