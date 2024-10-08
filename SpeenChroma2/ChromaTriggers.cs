using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SpinCore.Triggers;
using SpinCore.Utility;

namespace SpeenChroma2
{
    public static class ChromaTriggers
    {
        private class ChromaTrigger : ITrigger
        {
            public float Time { get; set; }
            public float Duration { get; set; }
            public HslColor StartColor { get; set; }
            public HslColor EndColor { get; set; }

            public void EnsureSmoothTransition()
            {
                if (StartColor.H == 0f && EndColor.H != 0f &&
                    (StartColor.S == 0f || StartColor.L == 1f))
                {
                    var col = StartColor;
                    col.H = EndColor.H;
                    StartColor = col;
                }

                if (EndColor.H == 0f && StartColor.H != 0f &&
                    (EndColor.S == 0f || EndColor.L == 1f))
                {
                    var col = EndColor;
                    col.H = StartColor.H;
                    EndColor = col;
                }
            }
        }

        private struct ChromaTriggersData
        {
            public List<ChromaTrigger> NoteA { get; set; }
            public List<ChromaTrigger> NoteB { get; set; }
            public List<ChromaTrigger> Beat { get; set; }
            public List<ChromaTrigger> SpinLeft { get; set; }
            public List<ChromaTrigger> SpinRight { get; set; }
            public List<ChromaTrigger> Scratch { get; set; }
            public List<ChromaTrigger> Ancillary { get; set; }

            public Dictionary<NoteColorType, List<ChromaTrigger>> ToDictionary()
            {
                return new Dictionary<NoteColorType, List<ChromaTrigger>>
                {
                    { NoteColorType.NoteA, NoteA },
                    { NoteColorType.NoteB, NoteB },
                    { NoteColorType.Beat, Beat },
                    { NoteColorType.SpinLeft, SpinLeft },
                    { NoteColorType.SpinRight, SpinRight },
                    { NoteColorType.Scratch, Scratch },
                    { NoteColorType.Ancillary, Ancillary }
                };
            }
        }

        private static readonly (string, NoteColorType)[] KeyPairs = {
            ("ChromaNoteA", NoteColorType.NoteA),
            ("ChromaNoteB", NoteColorType.NoteB),
            ("ChromaBeat", NoteColorType.Beat),
            ("ChromaSpinLeft", NoteColorType.SpinLeft),
            ("ChromaSpinRight", NoteColorType.SpinRight),
            ("ChromaScratch", NoteColorType.Scratch),
            ("ChromaAncillary", NoteColorType.Ancillary),
        };

        private static readonly Dictionary<NoteColorType, HslColor> DefinedColors = new Dictionary<NoteColorType, HslColor>();
        private static readonly Dictionary<string, HslColor> UserColorDefinitions = new Dictionary<string, HslColor>();
        private static readonly Regex VariableNameChecker = new Regex(@"(default)|([^a-zA-Z0-9\-_]+)");

        public static void Setup()
        {
            RegisterEvents();
        }

        public static void ClearAll()
        {
            foreach (var pair in KeyPairs)
            {
                TriggerManager.ClearTriggers(pair.Item1);
            }
        }

        private static void RegisterEvents()
        {
            foreach (var pair in KeyPairs)
            {
                TriggerManager.RegisterTriggerEvent<ChromaTrigger>(pair.Item1, (chromaTrigger, time) =>
                {
                    if (!ChromaManager.EnableTriggers) return;
                    if (chromaTrigger.Duration == 0f)
                    {
                        ChromaManager.SetColorForNoteType(pair.Item2, chromaTrigger.EndColor);
                        return;
                    }
                    float timeOne = (time - chromaTrigger.Time) / chromaTrigger.Duration;
                    var col = HslColor.Lerp(chromaTrigger.StartColor, chromaTrigger.EndColor, timeOne);
                    ChromaManager.SetColorForNoteType(pair.Item2, col);
                });
            }
        }

        public static void LoadTriggers(PlayableTrackData playableTrackData)
        {
            if (!ChromaManager.EnableTriggers) return;
            if (playableTrackData.TrackDataList.Count == 0) return;
            var trackData = playableTrackData.TrackDataList[0];
            bool loadedFromFile = true;
            string path = trackData.CustomFile?.FilePath;
            if (string.IsNullOrEmpty(path))
                return;
            string filename = Path.GetFileNameWithoutExtension(path);
            string directory = Directory.GetParent(path)?.FullName;
            if (string.IsNullOrEmpty(directory))
                return;
            string diffStr = playableTrackData.Difficulty.ToString().ToUpper();
            string chromaPath = Path.Combine(directory, filename + ".chroma");
            string diffChromaPath = Path.Combine(directory, filename + "_" + diffStr + ".chroma");

            Dictionary<NoteColorType, List<ChromaTrigger>> triggers = null;
            try
            {
                if (File.Exists(diffChromaPath))
                    triggers = LoadTriggersFromChromaFile(diffChromaPath);
                else if (File.Exists(chromaPath))
                    triggers = LoadTriggersFromChromaFile(chromaPath);
                else
                {
                    triggers = LoadTriggersFromEmbeddedData(playableTrackData, diffStr);
                    loadedFromFile = false;
                }
            }
            catch (Exception e)
            {
                NotificationSystemGUI.AddMessage("An error occurred while loading triggers; check console for details");
                Log.Error($"An error occurred while loading triggers: {e}");
            }

            if (triggers == null || triggers.Count == 0)
                return;

            ChromaManager.AreTriggersLoaded = true;
            int totalCount = 0;
            foreach (var pair in KeyPairs)
            {
                TriggerManager.ClearTriggers(pair.Item1);
                if (!triggers.TryGetValue(pair.Item2, out var list))
                {
                    list = new List<ChromaTrigger>();
                    triggers.Add(pair.Item2, list);
                }

                // Force adding default values to prevent rainbow effect from happening
                // TODO: FIND CLEANER WAY TO DISABLE RAINBOW WHEN TRIGGERS ARE ACTIVE
                list.Add(new ChromaTrigger
                {
                    Time = -10f,
                    Duration = 0f,
                    StartColor = ChromaManager.GetDefaultColorForNoteType(pair.Item2),
                    EndColor = ChromaManager.GetDefaultColorForNoteType(pair.Item2),
                });
                TriggerManager.LoadTriggers(pair.Item1, list.ToArray<ITrigger>()); 
                totalCount += list.Count;
            }

            string log = loadedFromFile ? "file " + filename + ".chroma" : "embedded data";
            Log.Info("Applied " + totalCount + " triggers from " + log);
        }

        private static HslColor GetColorNoDefault(string color)
        {
            if (color.StartsWith("#"))
            {
                return HslColor.FromHexRgb(color);
            }

            return UserColorDefinitions.TryGetValue(color, out var colVar)
                ? colVar
                : throw new Exception($"Color Variable \"{color}\" is not defined");
        }

        private static HslColor GetColor(string color)
        {
            if (color.StartsWith("default"))
            {
                string noteColor = color.Substring(7);
                var noteType = Util.GetNoteTypeForString(noteColor);
                if (!DefinedColors.TryGetValue(noteType, out var col))
                    throw new Exception("Color isn't defined!");
                return col;
            }

            return GetColorNoDefault(color);
        }

        private static HslColor GetColor(string color, NoteColorType noteType)
        {
            if (color == "default")
            {
                if (!DefinedColors.TryGetValue(noteType, out var col))
                    throw new Exception("Color isn't defined!");
                return col;
            }

            return GetColor(color);
        }

        private static Dictionary<NoteColorType, List<ChromaTrigger>> LoadTriggersFromEmbeddedData(PlayableTrackData trackData, string diffStr)
        {
            var files = new List<IMultiAssetSaveFile>();
            trackData.GetCustomFiles(files);
            var file = files.First();
            if (file is null) return null;
            if (CustomChartHelper.TryGetCustomData(file, "SpeenChroma_ChromaTriggers_" + diffStr, out ChromaTriggersData diffTriggers))
            {
                return diffTriggers.ToDictionary();
            }
            if (CustomChartHelper.TryGetCustomData(file, "SpeenChroma_ChromaTriggers", out ChromaTriggersData allTriggers))
            {
                return allTriggers.ToDictionary();
            }
            return null;
        }

        private static Dictionary<NoteColorType, List<ChromaTrigger>> LoadTriggersFromChromaFile(string path)
        {
            DefinedColors.Clear();
            UserColorDefinitions.Clear();
            var dict = new Dictionary<NoteColorType, List<ChromaTrigger>>();

            int repeatDepth = 0;
            var repeatCounts = new List<int>();
            var currentRepeatIterations = new List<int>();
            var repeatLineBeginnings = new List<int>();
            var repeatIntervals = new List<float>();

            var chromaLines = File.ReadAllLines(path);

            float ParseFloat(string num) => float.Parse(num, CultureInfo.InvariantCulture);
            int ParseInt(string num) => int.Parse(num, CultureInfo.InvariantCulture);
            float ParseTimeFloat(string num)
            {
                float time = ParseFloat(num);
                if (repeatDepth <= 0)
                    return time;
                for (int i = 0; i < repeatDepth; i++)
                    time += repeatIntervals[i] * currentRepeatIterations[i];

                return time;
            }

            for (int lineNumber = 0; lineNumber < chromaLines.Length; lineNumber++)
            {
                string line = chromaLines[lineNumber];
                if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                    continue;

                var elems = line.Trim().ToLower().Split(null);
                if (elems.Length <= 0)
                    continue;

                switch (elems[0])
                {
                    case "start":
                    {
                        if (elems.Length < 3)
                            throw new Exception($"Missing arguments for Start trigger: {elems.Length}/3 supplied");
                        var noteType = Util.GetNoteTypeForString(elems[1]);
                        if (DefinedColors.ContainsKey(noteType))
                            throw new Exception("Color already defined!");
                        string colStr = elems[2];
                        var col = GetColorNoDefault(colStr);
                        var trigger = new ChromaTrigger
                        {
                            Time = 0f,
                            Duration = 0f,
                            StartColor = col,
                            EndColor = col
                        };
                        if (!dict.TryGetValue(noteType, out var list))
                        {
                            list = new List<ChromaTrigger>();
                            dict.Add(noteType, list);
                        }

                        list.Add(trigger);
                        DefinedColors.Add(noteType, col);
                        break;
                    }

                    case "set":
                    {
                        if (elems.Length < 3)
                            throw new Exception($"Missing arguments for Set trigger: {elems.Length}/3 supplied");
                        string colorName = elems[1];
                        string colorHex = elems[2];
                        if (VariableNameChecker.Match(colorName).Success)
                            throw new Exception($"Invalid color variable name: {colorName}");
                        UserColorDefinitions[colorName] = HslColor.FromHexRgb(colorHex);
                        break;
                    }

                    case "instant":
                    {
                        if (elems.Length < 4)
                            throw new Exception($"Missing arguments for Instant trigger: {elems.Length}/4 supplied");
                        var noteType = Util.GetNoteTypeForString(elems[1]);
                        float time = ParseTimeFloat(elems[2]);
                        string colStr = elems[3];
                        var col = GetColor(colStr, noteType);
                        var trigger = new ChromaTrigger
                        {
                            Time = time,
                            Duration = 0f,
                            StartColor = col,
                            EndColor = col
                        };
                        if (!dict.TryGetValue(noteType, out var list))
                        {
                            list = new List<ChromaTrigger>();
                            dict.Add(noteType, list);
                        }

                        list.Add(trigger);
                        break;
                    }

                    case "swap":
                    {
                        if (elems.Length < 2)
                            throw new Exception($"Missing arguments for Swap trigger: {elems.Length}/5-7 supplied");

                        HslColor GetPreviousOrDefaultColor(NoteColorType noteColor)
                        {
                            if (!DefinedColors.TryGetValue(noteColor, out var defaultCol))
                                throw new Exception("Cannot use swap at this time: no previous color data found");

                            return dict.TryGetValue(noteColor, out var list)
                                ? list.LastOrDefault()?.EndColor ?? defaultCol
                                : defaultCol;
                        }

                        if (elems[1] == "instant")
                        {
                            if (elems.Length < 5)
                                throw new Exception($"Missing arguments for Swap Instant trigger: {elems.Length}/5 supplied");

                            float time = ParseTimeFloat(elems[2]);
                            var note1 = Util.GetNoteTypeForString(elems[3]);
                            var note2 = Util.GetNoteTypeForString(elems[4]);

                            if (!dict.TryGetValue(note1, out var list1))
                            {
                                list1 = new List<ChromaTrigger>();
                                dict.Add(note1, list1);
                            }

                            if (!dict.TryGetValue(note2, out var list2))
                            {
                                list2 = new List<ChromaTrigger>();
                                dict.Add(note2, list2);
                            }

                            var noteCol1 = GetPreviousOrDefaultColor(note1);
                            var noteCol2 = GetPreviousOrDefaultColor(note2);

                            var trigger1 = new ChromaTrigger
                            {
                                Time = time,
                                Duration = 0f,
                                StartColor = noteCol2,
                                EndColor = noteCol2,
                            };
                            var trigger2 = new ChromaTrigger
                            {
                                Time = time,
                                Duration = 0f,
                                StartColor = noteCol1,
                                EndColor = noteCol1,
                            };

                            list1.Add(trigger1);
                            list2.Add(trigger2);
                            break;
                        }
                        
                        if (elems[1] == "flash")
                        {
                            if (elems.Length < 7)
                                throw new Exception($"Missing arguments for Swap Flash trigger: {elems.Length}/7 supplied");
                            float time = ParseTimeFloat(elems[2]);
                            float end = ParseTimeFloat(elems[3]);
                            var note1 = Util.GetNoteTypeForString(elems[4]);
                            var note2 = Util.GetNoteTypeForString(elems[5]);
                            var flashColor = GetColor(elems[6].ToLower());

                            if (!dict.TryGetValue(note1, out var list1))
                            {
                                list1 = new List<ChromaTrigger>();
                                dict.Add(note1, list1);
                            }

                            if (!dict.TryGetValue(note2, out var list2))
                            {
                                list2 = new List<ChromaTrigger>();
                                dict.Add(note2, list2);
                            }

                            var noteCol1 = GetPreviousOrDefaultColor(note1);
                            var noteCol2 = GetPreviousOrDefaultColor(note2);

                            var trigger1 = new ChromaTrigger
                            {
                                Time = time,
                                Duration = end - time,
                                StartColor = flashColor,
                                EndColor = noteCol2,
                            };
                            var trigger2 = new ChromaTrigger
                            {
                                Time = time,
                                Duration = end - time,
                                StartColor = flashColor,
                                EndColor = noteCol1,
                            };

                            list1.Add(trigger1);
                            list2.Add(trigger2);
                            continue;
                        }

                        throw new Exception($"Invalid swap subcommand: expected Instant/Flash, found {elems[1]}.");
                    }

                    case "repeat":
                    {
                        if (elems.Length < 4)
                            throw new Exception($"Missing arguments for Repeat block: {elems.Length}/4 supplied");

                        if (elems[2] != "interval")
                            throw new Exception("Invalid instruction. Usage: \"Repeat X interval Y\"");

                        repeatDepth++;
                        repeatCounts.Add(ParseInt(elems[1]));
                        repeatIntervals.Add(ParseFloat(elems[3]));
                        repeatLineBeginnings.Add(lineNumber);
                        currentRepeatIterations.Add(0);
                        break;
                    }

                    case "endrepeat":
                    {
                        if (repeatDepth <= 0)
                            throw new Exception("Unexpected EndRepeat instruction");
                        if (++currentRepeatIterations[repeatDepth - 1] != repeatCounts[repeatDepth - 1])
                        {
                            lineNumber = repeatLineBeginnings[repeatDepth - 1];
                            continue;
                        }

                        repeatDepth--;
                        repeatCounts.RemoveAt(repeatDepth);
                        currentRepeatIterations.RemoveAt(repeatDepth);
                        repeatIntervals.RemoveAt(repeatDepth);
                        repeatLineBeginnings.RemoveAt(repeatDepth);
                        break;
                    }

                    default:
                    {
                        if (elems.Length < 5)
                            throw new Exception($"Missing arguments for generic trigger: {elems.Length}/5 supplied");
                        var noteType = Util.GetNoteTypeForString(elems[0]);
                        float startTime = ParseTimeFloat(elems[1]);
                        float endTime = ParseTimeFloat(elems[2]);
                        string startCol = elems[3].ToLower();
                        string endCol = elems[4].ToLower();

                        var trigger = new ChromaTrigger
                        {
                            Time = startTime,
                            Duration = endTime - startTime,
                            StartColor = GetColor(startCol, noteType),
                            EndColor = GetColor(endCol, noteType)
                        };
                        if (!dict.TryGetValue(noteType, out var list))
                        {
                            list = new List<ChromaTrigger>();
                            dict.Add(noteType, list);
                        }

                        list.Add(trigger);
                        break;
                    }
                }
            }

            if (repeatDepth > 0)
                throw new Exception($"Missing {repeatDepth} EndRepeat instruction" + (repeatDepth == 1 ? "s" : ""));

            if (dict.Count == 0)
                return dict;

            foreach (var pair in dict)
            {
                pair.Value.Sort((t1, t2) => t1.Time.CompareTo(t2.Time));
                foreach (var trigger in pair.Value)
                {
                    trigger.EnsureSmoothTransition();
                }
            }
            return dict;
        }
    }
}
