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
    public class CheckDashCup : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Category = "Compose",
            Message = "Dash pattern is used.",
            Modes = new[] { Beatmap.Mode.Catch },
            Difficulties = new[] { Beatmap.Difficulty.Easy },
            Author = "Riana",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    For cup difficulty, dashes and hyperdashes of any kind are disallowed."
                },
                {
                    "Reason",
                    @"
                    To ensure an easy starting experience to beginner players. In order to test that out, it must be possible to achieve an SS rank on the difficulty without making use of the dash key."
                }
            }
        };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                { "StrongWalk",
                    new IssueTemplate(Issue.Level.Warning,
                            "{0} Strong walk is used.",
                            "timestamp - ")
                        .WithCause("Strong walk can be difficult on difficulty level.")
                },
                { "Dash",
                    new IssueTemplate(Issue.Level.Problem,
                            "{0} Dash is used.",
                            "timestamp - ")
                        .WithCause("Dash should not be used on difficulty level.")
                },
                { "HyperDash",
                    new IssueTemplate(Issue.Level.Problem,
                            "{0} Hyperdash is used.",
                            "timestamp - ")
                        .WithCause("Hyperdash should not be used on difficulty level.")
                }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            CheckBeatmapSetDistanceCalculation.SetBeatmaps.TryGetValue(beatmap.metadataSettings.version, out var catchObjects);

            //catchObjectManager.CalculateJumps(catchObjects, beatmap);

            if (catchObjects == null || catchObjects.Count == 0)
            {
                yield break;
            }

            var issueObjects = new List<CatchHitObject>();

            for (var i = 0; i < catchObjects.Count - 1; i++)
            {
                var currentObject = catchObjects[i];

                if (currentObject.DistanceToDash <= 0.15 * (currentObject.Target.time - currentObject.time)){
                    issueObjects.Add(currentObject);
                }

                if (currentObject.Extras == null) continue;

                //Check snaps for slider parts
                foreach (var sliderObjectExtra in currentObject.Extras)
                {
                    if (currentObject.DistanceToDash <= 0.15 * (currentObject.Target.time - currentObject.time)){
                        issueObjects.Add(currentObject);
                    }
                }
            }

            foreach (var currentObject in issueObjects){
                if (currentObject.MovementType == MovementType.DASH)
                {
                    yield return new Issue(
                        GetTemplate("Dash"),
                        beatmap,
                        Timestamp.Get(currentObject.time)
                    ).ForDifficulties(Beatmap.Difficulty.Easy);
                }
                else if (currentObject.MovementType == MovementType.HYPERDASH)
                {
                    yield return new Issue(
                        GetTemplate("HyperDash"),
                        beatmap,
                        Timestamp.Get(currentObject.time)
                    ).ForDifficulties(Beatmap.Difficulty.Easy);
                }
                else {
                    if (currentObject.DistanceToDash <= 0.15 * (currentObject.Target.time - currentObject.time)){
                        yield return new Issue(
                            GetTemplate("StrongWalk"),
                            beatmap,
                            Timestamp.Get(currentObject.time)
                        ).ForDifficulties(Beatmap.Difficulty.Easy);
                    }
                }
            }            
        }
    }
}
