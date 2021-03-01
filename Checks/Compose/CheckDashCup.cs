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
            for (var i = 0; i < catchObjects.Count; i++)
            {
                var currentObject = catchObjects[i];

                if (currentObject.MovementType == MovementType.DASH)
                {
                    yield return new Issue(
                        GetTemplate("Dash"),
                        beatmap,
                        Timestamp.Get(currentObject.time)
                    ).ForDifficulties(Beatmap.Difficulty.Easy);
                }

                if (currentObject.MovementType == MovementType.HYPERDASH)
                {
                    yield return new Issue(
                        GetTemplate("HyperDash"),
                        beatmap,
                        Timestamp.Get(currentObject.time)
                    ).ForDifficulties(Beatmap.Difficulty.Easy);
                }

                //Check snaps for slider parts
                foreach (var sliderObjectExtra in currentObject.Extras)
                {
                    if (sliderObjectExtra.MovementType == MovementType.DASH)
                    {
                        yield return new Issue(
                            GetTemplate("Dash"),
                            beatmap,
                            Timestamp.Get(sliderObjectExtra.time)
                        ).ForDifficulties(Beatmap.Difficulty.Easy);
                    }

                    if (sliderObjectExtra.MovementType == MovementType.HYPERDASH)
                    {
                        yield return new Issue(
                            GetTemplate("HyperDash"),
                            beatmap,
                            Timestamp.Get(sliderObjectExtra.time)
                        ).ForDifficulties(Beatmap.Difficulty.Easy);
                    }
                }
            }
        }
    }
}
