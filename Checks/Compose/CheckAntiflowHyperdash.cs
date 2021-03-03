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
    public class CheckAntiflowHyperdash : BeatmapCheck
    {
        private const float ThresholdWalkAfterBasic = 1.3f;
        private const float ThresholdDashAfterBasic = 1.2f;
        private const float ThresholdAfterHigh = 1.1f;
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Category = "Compose",
            Message = "Hyperdash followed by antiflow patterns.",
            Difficulties = new[] { Beatmap.Difficulty.Hard },
            Modes = new[] { Beatmap.Mode.Catch },
            Author = "Riana",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    <b>Platter</b> : 
                    Basic hyperdashes may be used in conjunction with antiflow patterns.
                    If used, the spacing should not exceed a distance snap of 1.2 times the trigger distance when followed by a walkable movement, or 1.1 times the trigger distance when followed by a basic dash.
                    Higher-snapped hyperdashes should not be followed by antiflow patterns.
                    If used, the spacing should not exceed a distance snap of 1.1 times the trigger distance and the movement after the hyperdash must be walkable."
                },
                {
                    "Reasoning",
                    @"
                    In platters usage of antiflow patterns after hyperdashes is not recommended, because the difficulty is meant to be an introduction to hypers."
                }
            }
        };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                { "AntiflowWalk",
                    new IssueTemplate(Issue.Level.Warning,
                            "{0} Basic hyper should not be longer than {1} times the trigger distance if it's followed by an antiflow walk, currently {2} times.",
                            "timestamp - ", "rule amount", "amount")
                        .WithCause(
                            "Too strong hyperdash is used in conjunction with an antiflow walk.")
                },
                { "AntiflowDash",
                    new IssueTemplate(Issue.Level.Warning,
                            "{0} Basic hyper should not be longer than {1} times the trigger distance if it's followed by an antiflow dash, currently {2} times.",
                            "timestamp - ", "rule amount", "amount")
                        .WithCause(
                            "Too strong hyperdash is used in conjunction with an antiflow dash.")
                },
                { "AntiflowHighDash",
                    new IssueTemplate(Issue.Level.Warning,
                            "{0} Basic hyper should not be followed by an antiflow higher-snapped dash.",
                            "timestamp - ")
                        .WithCause(
                            "Basic hyperdash is followed by an antiflow higher-snapped dash.")
                },
                { "AntiflowWalkHigh",
                    new IssueTemplate(Issue.Level.Warning,
                            "{0} Higher snapped hyper should not be longer than {1} times the trigger distance if it's followed by an antiflow pattern, currently {2} times.",
                            "timestamp - ", "rule amount", "amount")
                        .WithCause(
                            "Too strong higher snapped hyperdash is followed by an antiflow walk.")
                },
                { "AntiflowDashHigh",
                    new IssueTemplate(Issue.Level.Warning,
                            "{0} Higher snapped hyper should not be followed by an antiflow dash.",
                            "timestamp - ")
                        .WithCause(
                            "Higher snapped hyperdash is followed by an antiflow dash or hyperdash.")
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

            var catchDifficulty = (beatmap.difficultySettings.circleSize - 5.0) / 5.0;
            var fruitWidth = (float) (64.0 * (1.0 - 0.699999988079071 * catchDifficulty)) / 128f;
            var catcherWidth = 305f * fruitWidth * 0.7f;
            var quarterCatcherWidth = catcherWidth / 4;

            // We set i = 1 to skip the first object
            for (var i = 1; i < catchObjects.Count - 1; i++)
            {
                var currentObject = catchObjects[i];

                if (lastCheckedObject == null)
                {
                    lastCheckedObject = catchObjects[i - 1];
                }

                if (lastCheckedObject.MovementType == MovementType.HYPERDASH)
                {
                    var snap = (int)(currentObject.time - lastCheckedObject.time);
                    var distance = lastCheckedObject.X - currentObject.X;

                    var next_distance = currentObject.X - currentObject.Target.X;
                    // objects can be nearly stacked, give some leniency for those so they won't be considered antiflow
                    var leniency = Math.Min((float)snap / 8.0, quarterCatcherWidth);

                    // antiflow condition
                    if (distance * next_distance < 0 && Math.Abs(next_distance)> leniency) {
                        var abs_distance = Math.Abs(distance);
                        var hyperDistance = abs_distance + lastCheckedObject.DistanceToHyperDash;
                        var multiplier = (float)abs_distance / (float)hyperDistance;

                        if (snap < 250 && snap >= 125)
                        {
                            if (currentObject.MovementType == MovementType.WALK && multiplier > ThresholdAfterHigh){
                                yield return new Issue(
                                    GetTemplate("AntiflowWalkHigh"),
                                    beatmap,
                                    Timestamp.Get(lastCheckedObject.time),
                                    ThresholdAfterHigh,
                                    multiplier
                                ).ForDifficulties(Beatmap.Difficulty.Hard);
                            }
                            else if (currentObject.MovementType != MovementType.WALK) {
                                yield return new Issue(
                                    GetTemplate("AntiflowDashHigh"),
                                    beatmap,
                                    Timestamp.Get(lastCheckedObject.time)
                                ).ForDifficulties(Beatmap.Difficulty.Hard);
                            }
                        }

                        if (snap >= 250)
                        {
                            if (currentObject.MovementType == MovementType.WALK){
                                if (multiplier > ThresholdWalkAfterBasic){
                                    yield return new Issue(
                                        GetTemplate("AntiflowWalk"),
                                        beatmap,
                                        Timestamp.Get(lastCheckedObject.time),
                                        ThresholdWalkAfterBasic,
                                        multiplier
                                    ).ForDifficulties(Beatmap.Difficulty.Hard);
                                }
                            }
                            else {
                                var next_snap = (int)(currentObject.Target.time - currentObject.time);
                                if (next_snap < 125 && next_snap >= 64){
                                    yield return new Issue(
                                        GetTemplate("AntiflowHighDash"),
                                        beatmap,
                                        Timestamp.Get(lastCheckedObject.time)
                                    ).ForDifficulties(Beatmap.Difficulty.Hard);
                                }
                                else if (multiplier > ThresholdDashAfterBasic){
                                    yield return new Issue(
                                        GetTemplate("AntiflowDash"),
                                        beatmap,
                                        Timestamp.Get(lastCheckedObject.time),
                                        ThresholdDashAfterBasic,
                                        multiplier
                                    ).ForDifficulties(Beatmap.Difficulty.Hard);
                                }
                            }
                        }
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
