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

                if (triggers?.Count > 0)
                    NotificationSystemGUI.AddMessage("Loaded " + triggers.Count + " chroma triggers");
            }
            catch (Exception e)
            {
                NotificationSystemGUI.AddMessage("ERROR: " + e);
            }

            if (triggers == null || triggers.Count == 0)
                return;
            
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
            Main.Log("Applied " + totalCount + " triggers from " + log);
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

        private static float ParseFloat(string num)
        {
            return float.Parse(num, CultureInfo.InvariantCulture);
        }

        private static Dictionary<NoteColorType, List<ChromaTrigger>> LoadTriggersFromChromaFile(string path)
        {
            DefinedColors.Clear();
            UserColorDefinitions.Clear();
            var dict = new Dictionary<NoteColorType, List<ChromaTrigger>>();
            foreach (string line in File.ReadAllLines(path))
            {
                if (line.StartsWith("#"))
                    continue;

                var elems = line.Trim().ToLower().Split(' ');
                var trigger = new ChromaTrigger();
                NoteColorType noteType;
                
                // First line check
                if (elems.Length < 3)
                    continue;
                if (elems[0] == "start")
                {
                    trigger.Time = 0f;
                    trigger.Duration = 0f;
                    noteType = Util.GetNoteTypeForString(elems[1]);
                    if (DefinedColors.ContainsKey(noteType))
                        throw new Exception("Color already defined!");
                    string colStr = elems[2];
                    var col = GetColorNoDefault(colStr);
                    trigger.StartColor = col;
                    trigger.EndColor = col;
                    if (!dict.TryGetValue(noteType, out var list))
                    {
                        list = new List<ChromaTrigger>();
                        dict.Add(noteType, list);
                    }

                    list.Add(trigger);
                    DefinedColors.Add(noteType, col);
                    continue;
                }

                if (elems[0] == "set")
                {
                    string colorName = elems[1];
                    string colorHex = elems[2];
                    if (VariableNameChecker.Match(colorName).Success)
                        throw new Exception($"Invalid color variable name: {colorName}");
                    UserColorDefinitions[colorName] = HslColor.FromHexRgb(colorHex);
                    continue;
                }

                if (elems.Length < 4)
                    continue;

                if (elems[0] == "instant")
                {
                    noteType = Util.GetNoteTypeForString(elems[1]);
                    float time = ParseFloat(elems[2]);
                    string colStr = elems[3];
                    var col = GetColor(colStr, noteType);
                    trigger.Time = time;
                    trigger.Duration = 0f;
                    trigger.StartColor = col;
                    trigger.EndColor = col;
                    if (!dict.TryGetValue(noteType, out var list))
                    {
                        list = new List<ChromaTrigger>();
                        dict.Add(noteType, list);
                    }

                    list.Add(trigger);
                    continue;
                }

                if (elems.Length < 5)
                    continue;

                if (elems[0] == "swap")
                {
                    if (elems[1] == "instant")
                    {
                        float time = ParseFloat(elems[2]);
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
                        
                        var noteCol1 = list1.Last()?.EndColor ?? ChromaManager.GetDefaultColorForNoteType(note1);
                        var noteCol2 = list2.Last()?.EndColor ?? ChromaManager.GetDefaultColorForNoteType(note2);

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
                        continue;
                    }

                    if (elems.Length < 7)
                        continue;

                    if (elems[1] == "flash")
                    {
                        float time = ParseFloat(elems[2]);
                        float end = ParseFloat(elems[3]);
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
                        
                        var noteCol1 = list1.Last()?.EndColor ?? ChromaManager.GetDefaultColorForNoteType(note1);
                        var noteCol2 = list2.Last()?.EndColor ?? ChromaManager.GetDefaultColorForNoteType(note2);

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
                }

                noteType = Util.GetNoteTypeForString(elems[0]);
                float startTime = ParseFloat(elems[1]);
                float endTime = ParseFloat(elems[2]);
                string startCol = elems[3].ToLower();
                string endCol = elems[4].ToLower();

                trigger.Time = startTime;
                trigger.Duration = endTime - startTime;
                trigger.StartColor = GetColor(startCol, noteType);
                trigger.EndColor = GetColor(endCol, noteType);
                trigger.EnsureSmoothTransition();
                if (!dict.TryGetValue(noteType, out var list0))
                {
                    list0 = new List<ChromaTrigger>();
                    dict.Add(noteType, list0);
                }

                list0.Add(trigger);
            }

            if (dict.Count == 0)
                return dict;

            foreach (var pair in dict)
            {
                pair.Value.Sort((t1, t2) => t1.Time.CompareTo(t2.Time));
            }
            return dict;
        }
    }
}
