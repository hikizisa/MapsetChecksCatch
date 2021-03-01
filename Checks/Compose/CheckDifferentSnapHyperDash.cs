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
    public class CheckDifferentSnapHyperdash : BeatmapCheck
    {

        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Category = "Compose",
            Message = "Hyperdashes of different beat snap is used between consecutive fruits.",
            Difficulties = new[] { Beatmap.Difficulty.Hard, Beatmap.Difficulty.Insane },
            Modes = new[] { Beatmap.Mode.Catch },
            Author = "Riana",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    <b>Rain</b> : 
                    Basic hyperdashes of different beat snap should not be used between consecutive fruits.
                    <br/>
                    <b>Platter</b> : 
                    Hyperdashes of different beat snap (for example, a 1/2 hyperdash followed by a 1/4 hyperdash) must not be used between consecutive fruits."
                },
                {
                    "Reasoning",
                    @"
                    To be added."
                }
            }
        };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                { "ConsecutiveDifferentSnapPlatter",
                    new IssueTemplate(Issue.Level.Problem,
                            "{0} Basic Hyperdash followed by a different snapped hyperdash.",
                            "timestamp - ")
                        .WithCause(
                            "Basic hyperdash followed by a different snapped hyperdash.")
                },
                { "ConsecutiveDifferentSnapRain",
                    new IssueTemplate(Issue.Level.Warning,
                            "{0} Basic hyperdash followed by a different snapped hyperdash.",
                            "timestamp - ")
                        .WithCause(
                            "Basic hyperdash followed by a different snapped hyperdash.")
                }
            };
        }

        private IEnumerable<Issue> GetDifferentSnapHyperDashIssues(Beatmap beatmap, CatchHitObject currentObject, CatchHitObject lastObject){
            var lastObjectMsGap = (int) (currentObject.time - lastObject.time);
            var currentMsGap = (int) (currentObject.Target.time - currentObject.time);

            // add + 5 or - 5 to reduce false positives for ~1 ms wrongly snapped objects
            if ((lastObjectMsGap > currentMsGap + 5 || lastObjectMsGap < currentMsGap - 5) &&
                lastObject.MovementType == MovementType.HYPERDASH && currentObject.MovementType == MovementType.HYPERDASH)
            {
                // Even though it is not explicitly stated, higher snapped hyperdashes must not be used in conjunction with any other hyperdash
                // So it's redundant to check hyperdashes with different beat snap used in conjunction for higher snapped hyperdashes
                if (!IsHigherSnapped(Beatmap.Difficulty.Hard, currentObject, lastObject) &&
                    !IsHigherSnapped(Beatmap.Difficulty.Hard, currentObject.Target, currentObject)){
                    yield return new Issue(
                        GetTemplate("ConsecutiveDifferentSnapPlatter"),
                        beatmap,
                        Timestamp.Get(currentObject.time)
                    ).ForDifficulties(Beatmap.Difficulty.Hard);
                }
                
                if (!IsHigherSnapped(Beatmap.Difficulty.Insane, currentObject, lastObject) &&
                    !IsHigherSnapped(Beatmap.Difficulty.Insane, currentObject.Target, currentObject)){
                    yield return new Issue(
                        GetTemplate("ConsecutiveDifferentSnapRain"),
                        beatmap,
                        Timestamp.Get(currentObject.time)
                    ).ForDifficulties(Beatmap.Difficulty.Insane);
                }
            }
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            CheckBeatmapSetDistanceCalculation.SetBeatmaps.TryGetValue(beatmap.metadataSettings.version, out var catchObjects);

            if (catchObjects == null || catchObjects.Count == 0)
            {
                yield break;
            }

            CatchHitObject lastObject = catchObjects[0];
            var issues = new List<Issue>();
            for (var i = 1; i < catchObjects.Count - 1; i++)
            {
                var currentObject = catchObjects[i];
                issues.AddRange(GetDifferentSnapHyperDashIssues(beatmap, currentObject, lastObject));
                lastObject = currentObject;

                foreach (var currentObjectExtra in currentObject.Extras)
                {
                    issues.AddRange(GetDifferentSnapHyperDashIssues(beatmap, currentObjectExtra, lastObject));
                    lastObject = currentObjectExtra;
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
