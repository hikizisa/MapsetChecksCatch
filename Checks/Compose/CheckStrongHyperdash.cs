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
    public class CheckStrongHyperdash : BeatmapCheck
    {
        private const float ThresholdPlatterHyper = 1.5f;
        private const float ThresholdPlatterHigherHyper = 1.3f;
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Category = "Compose",
            Message = "Too strong hyperdash patterns.",
            Difficulties = new[] { Beatmap.Difficulty.Hard },
            Modes = new[] { Beatmap.Mode.Catch },
            Author = "Riana",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    <b>Platter</b> : 
                    Strong hyperdashes should not be used.
                    For basic hyperdashes, a limit of 1.5 times the trigger distance is recommended.
                    For higher-snapped hyperdashes, a limit of 1.3 times the trigger distance is recommended instead."
                },
                {
                    "Reasoning",
                    @"
                    Strong hyperdashes are easier to overshoot, and thus difficult.
                    In platters usage of strong hyperdashes is not recommended, because the difficulty is meant to be an introduction to hypers."
                }
            }
        };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                { "Strong",
                    new IssueTemplate(Issue.Level.Warning,
                            "{0} Basic hyper should not be longer than {1} times the trigger distance, currently {2}.",
                            "timestamp - ", "rule amount", "amount")
                        .WithCause(
                            "Too strong hyperdash is used.")
                },
                { "StrongHigherSnap",
                    new IssueTemplate(Issue.Level.Warning,
                            "{0} Higher snapped hyper should not be longer than {1} times the trigger distance, currently {2}.",
                            "timestamp - ", "rule amount", "amount")
                        .WithCause(
                            "Too strong higher snapped hyperdash is used.")
                }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            CheckBeatmapSetDistanceCalculation.SetBeatmaps.TryGetValue(beatmap.metadataSettings.version, out var catchObjects);

            CatchHitObject lastCheckedObject = null;

            if (catchObjects == null || catchObjects.Count == 0)
            {
                yield break;
            }

            // We set i = 1 to skip the first object
            for (var i = 1; i < catchObjects.Count; i++)
            {
                var currentObject = catchObjects[i];

                if (lastCheckedObject == null)
                {
                    lastCheckedObject = catchObjects[i - 1];
                }

                if (lastCheckedObject.MovementType == MovementType.HYPERDASH)
                {
                    var snap = (int)(currentObject.time - lastCheckedObject.time);
                    var distance = (int)(Math.Abs(lastCheckedObject.X - currentObject.X));
                    var hyperDistance = distance + lastCheckedObject.DistanceToHyperDash;
                    var multiplier = (float)distance / (float)hyperDistance;

                    if (snap < 250 && snap >= 125 && multiplier > ThresholdPlatterHigherHyper)
                    {
                        yield return new Issue(
                            GetTemplate("StrongHigherSnap"),
                            beatmap,
                            Timestamp.Get(currentObject.time),
                            ThresholdPlatterHigherHyper,
                            multiplier
                        ).ForDifficulties(Beatmap.Difficulty.Hard);
                    }

                    if (snap >= 250 && multiplier > ThresholdPlatterHyper)
                    {
                        yield return new Issue(
                            GetTemplate("Strong"),
                            beatmap,
                            Timestamp.Get(currentObject.time),
                            ThresholdPlatterHyper,
                            multiplier
                        ).ForDifficulties(Beatmap.Difficulty.Hard);
                    }
                }

                lastCheckedObject = currentObject;

                // If current object is a slider, update lastCheckedObject to sliderend
                // Sliderbody hyperdash is not allowed in platter, so it should be checked in separate module
                foreach (var currentObjectExtra in currentObject.Extras)
                {
                    lastCheckedObject = currentObjectExtra;
                }
            }
        }
    }
}
