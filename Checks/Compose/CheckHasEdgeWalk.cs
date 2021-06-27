using System.Collections.Generic;
using System.Linq;
using MapsetChecksCatch.Checks.General;
using MapsetChecksCatch.Helper;
using MapsetParser.objects;
using MapsetParser.statics;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MapsetChecksCatch.Checks.Compose
{
    [Check]
    public class CheckHasEdgeWalk : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Category = "Compose",
            Message = "Too strong walks.",
            Modes = new[] { Beatmap.Mode.Catch },
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Too strong walks are quite harsh and are most of the time unintentionally placed on a lower difficulty."
                },
                {
                    "Reasoning",
                    @"
                    Too strong walks require fast reaction speed, newer players don't have this and will instead tap dash this distance."
                }
            }
        };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                { "EdgeWalkWarning",
                    new IssueTemplate(Issue.Level.Warning,
                            "{0} This object is a harsh walk and might be seen as ambiguous, consider reducing it.",
                            "timestamp - ")
                        .WithCause(
                            "A too strong walk is provided")
                },
                { "EdgeWalk",
                    new IssueTemplate(Issue.Level.Minor,
                            "{0} This object is a harsh walk and might be seen as ambiguous, consider reducing it.",
                            "timestamp - ")
                        .WithCause(
                            "A too strong walk is provided")
                },
                { "EdgeWalkWarningGeneral",
                    new IssueTemplate(Issue.Level.Warning,
                            "{0} .. This difficulty is using many harsh walks that might be seen as ambiguous, consider checking it overall.",
                            "timestamp(s) - ")
                        .WithCause(
                            "Too strong walk are provided")
                },
                { "EdgeWalkGeneral",
                    new IssueTemplate(Issue.Level.Minor,
                            "{0} .. This difficulty is using many harsh walks that might be seen as ambiguous, consider checking it overall.",
                            "timestamp(s) - ")
                        .WithCause(
                            "Too strong walks are provided")
                }
            };
        }

        private static Issue EdgeWalkIssue(IssueTemplate template, Beatmap beatmap, CatchHitObject currentObject, params Beatmap.Difficulty[] difficulties)
        {
            return new Issue(
                template,
                beatmap,
                Timestamp.Get(currentObject.time)
            ).ForDifficulties(difficulties);
        }

        private static Issue EdgeWalkGeneralIssue(IssueTemplate template, Beatmap beatmap, string stamp, params Beatmap.Difficulty[] difficulties)
        {
            return new Issue(
                template,
                beatmap,
                stamp
            ).ForDifficulties(difficulties);
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            CheckBeatmapSetDistanceCalculation.SetBeatmaps.TryGetValue(beatmap.metadataSettings.version, out var catchObjects);

            var issueObjects = new List<CatchHitObject>();

            //List<string> lines = new List<string>();

            int object_cnt = 0;

            foreach (var currentObject in catchObjects
                .Where(currentObject => currentObject.type != HitObject.Type.Spinner && currentObject.MovementType != MovementType.DASH))
            {
                object_cnt += 1;
                var dashDistance = currentObject.DistanceToDash;

                if (dashDistance > 0)
                {
                    issueObjects.Add(currentObject);
                }
                //if (currentObject.Target != null)
                //    lines.Add($"{currentObject.DistanceToDash}\t{currentObject.Target.time - currentObject.Origin.time}");

                if (currentObject.Extras == null) continue;

                foreach (var sliderExtra in currentObject.Extras)
                {
                    object_cnt += 1;
                    var sliderObjectDashDistance = sliderExtra.DistanceToDash;
                    //if (sliderExtra.Target != null)
                    //    lines.Add($"{sliderExtra.DistanceToDash}\t{sliderExtra.Target.time - sliderExtra.Origin.time}");

                    if (sliderExtra.MovementType != MovementType.DASH && sliderObjectDashDistance > 0)
                    {
                        issueObjects.Add(sliderExtra);
                    }
                }
            }

            var raisedObjects = new List<CatchHitObject>();

            foreach (var issueObject in issueObjects)
            {
                // Ambiguous distance for objects with very long gap is trivial. 500ms is 1.5 beat for 180bpm
                if (issueObject.DistanceToDash < Math.Max(15, (issueObject.Target.time - issueObject.Origin.time) / 15.0f)
                    && issueObject.Target.time - issueObject.Origin.time < 500)
                {
                    raisedObjects.Add(issueObject);
                }
            }

            if (raisedObjects.Count < Math.Max(10, 0.02 * object_cnt)) {
                foreach (var issueObject in raisedObjects)
                {
                    yield return EdgeWalkIssue(GetTemplate("EdgeWalk"), beatmap, issueObject,
                        Beatmap.Difficulty.Hard);
                    yield return EdgeWalkIssue(GetTemplate("EdgeWalkWarning"), beatmap, issueObject,
                        Beatmap.Difficulty.Normal);
                }
            }
            else
            {
                // raise general ambiguous distance issue
                List<string> stamps = new List<string>();

                foreach (var issueObject in raisedObjects.Take(10)) {
                    stamps.Add(Timestamp.Get(issueObject.time));
                }
                string catStamps = String.Join(" ", stamps);

                yield return EdgeWalkGeneralIssue(GetTemplate("EdgeWalkGeneral"), beatmap, catStamps,
                    Beatmap.Difficulty.Hard);
                yield return EdgeWalkGeneralIssue(GetTemplate("EdgeWalkWarningGeneral"), beatmap, catStamps,
                    Beatmap.Difficulty.Normal);
            }
            //File.WriteAllLines($"{beatmap.metadataSettings.version}.txt", lines.ToArray());
        }
    }
}
