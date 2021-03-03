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
    public class CheckHyperdashConjunction : BeatmapCheck
    {

        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Category = "Compose",
            Message = "Higher-snapped hyperdashes used in conjunction with other dashes or hyperdashes.",
            Difficulties = new[] { Beatmap.Difficulty.Hard, Beatmap.Difficulty.Insane },
            Modes = new[] { Beatmap.Mode.Catch },
            Author = "Riana",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    <b>Rain</b> : 
                    Higher-snapped hyperdashes must not be used in conjunction with higher-snapped dashes or any other hyperdashes.
                    <br/>
                    <b>Platter</b> : 
                    Higher-snapped hyperdashes must not be used in conjunction with any other dashes or hyperdashes."
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
                { "ConsecutiveHigherSnapPlatter",
                    new IssueTemplate(Issue.Level.Problem,
                            "{0} Higher-snapped hyperdash is used in conjunction with other dash or hyperdash.",
                            "timestamp - ")
                        .WithCause(
                            "Higher-snapped hyperdash used in conjunction with other dash or hyperdash.")
                },
                { "ConsecutiveHigherSnapRain",
                    new IssueTemplate(Issue.Level.Problem,
                            "{0} Higher-snapped hyperdash is used in conjunction with higher-snapped dash or any other hyperdash.",
                            "timestamp - ")
                        .WithCause(
                            "Higher-snapped hyperdash used in conjunction with other dash or hyperdash.")
                }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            CheckBeatmapSetDistanceCalculation.SetBeatmaps.TryGetValue(beatmap.metadataSettings.version, out var catchObjects);

            CatchHitObject lastObject = catchObjects[0];
            var issues = new List<Issue>();
            for (var i = 1; i < catchObjects.Count - 1; i++)
            {
                var currentObject = catchObjects[i];
                var markedHard = false;
                var markedInsane = false;

                if (lastObject.MovementType == MovementType.HYPERDASH && currentObject.MovementType != MovementType.WALK){
                    if (IsHigherSnapped(Beatmap.Difficulty.Hard, currentObject, lastObject)){
                        yield return new Issue(
                            GetTemplate("ConsecutiveHigherSnapPlatter"),
                            beatmap,
                            Timestamp.Get(currentObject.time)
                        ).ForDifficulties(Beatmap.Difficulty.Hard);
                        markedHard = true;
                    }
                    if (IsHigherSnapped(Beatmap.Difficulty.Insane, currentObject, lastObject) &&
                        (currentObject.MovementType == MovementType.HYPERDASH || IsHigherSnapped(Beatmap.Difficulty.Insane, currentObject.Target, currentObject))){
                        yield return new Issue(
                            GetTemplate("ConsecutiveHigherSnapRain"),
                            beatmap,
                            Timestamp.Get(currentObject.time)
                        ).ForDifficulties(Beatmap.Difficulty.Insane);
                        markedInsane = true;
                    }
                }

                if (currentObject.MovementType == MovementType.HYPERDASH && lastObject.MovementType != MovementType.WALK){
                    if (IsHigherSnapped(Beatmap.Difficulty.Insane, currentObject.Target, currentObject) && !markedHard){
                        yield return new Issue(
                            GetTemplate("ConsecutiveHigherSnapPlatter"),
                            beatmap,
                            Timestamp.Get(currentObject.time)
                        ).ForDifficulties(Beatmap.Difficulty.Hard);
                    }
                    if (IsHigherSnapped(Beatmap.Difficulty.Insane, currentObject.Target, currentObject) && !markedInsane &&
                        (lastObject.MovementType == MovementType.HYPERDASH || IsHigherSnapped(Beatmap.Difficulty.Insane, currentObject, lastObject))){
                        yield return new Issue(
                            GetTemplate("ConsecutiveHigherSnapRain"),
                            beatmap,
                            Timestamp.Get(currentObject.time)
                        ).ForDifficulties(Beatmap.Difficulty.Insane);
                    }
                }

                lastObject = currentObject;

                foreach (var currentObjectExtra in currentObject.Extras)
                {
                    // Within slider higher-snapped hyperdashes cannot be used, so it is not checked here
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
