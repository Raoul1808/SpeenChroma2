using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpinTriggerHelper;

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
                if (StartColor.Hue == 0f && EndColor.Hue != 0f &&
                    (StartColor.Saturation == 0f || StartColor.Lightness == 1f))
                {
                    var col = StartColor;
                    col.Hue = EndColor.Hue;
                    StartColor = col;
                }

                if (EndColor.Hue == 0f && StartColor.Hue != 0f &&
                    (EndColor.Saturation == 0f || EndColor.Lightness == 1f))
                {
                    var col = EndColor;
                    col.Hue = StartColor.Hue;
                    EndColor = col;
                }
            }
        }

        public static void Setup()
        {
            RegisterEvents();
            TriggerManager.OnChartLoad += OnChartLoad;
        }

        private static void RegisterEvents()
        {
            (string, NoteColorType)[] pairs = {
                ("ChromaNoteA", NoteColorType.NoteA),
                ("ChromaNoteB", NoteColorType.NoteB),
                ("ChromaBeat", NoteColorType.Beat),
                ("ChromaSpinLeft", NoteColorType.SpinLeft),
                ("ChromaSpinRight", NoteColorType.SpinRight),
                ("ChromaScratch", NoteColorType.Scratch),
                ("ChromaAncillary", NoteColorType.Ancillary),
            };

            foreach (var pair in pairs)
            {
                TriggerManager.RegisterTriggerEvent(pair.Item1, (trigger, time) =>
                {
                    var chromaTrigger = (ChromaTrigger)trigger;
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

        private static void OnChartLoad(TrackData trackData)
        {
            string path = trackData.CustomFile?.FilePath;
            if (string.IsNullOrEmpty(path))
                return;
            string filename = Path.GetFileNameWithoutExtension(path);
            string directory = Directory.GetParent(path)?.FullName;
            if (string.IsNullOrEmpty(directory))
                return;
            string diffStr = trackData.difficultyType.ToString().ToUpper();
            string chromaPath = Path.Combine(directory, filename + ".chroma");
            string diffChromaPath = Path.Combine(directory, filename + "_" + diffStr + ".chroma");

            Dictionary<NoteColorType, List<ChromaTrigger>> triggers = null;
            if (File.Exists(diffChromaPath))
                triggers = LoadTriggers(diffChromaPath);
            else if (File.Exists(chromaPath))
                triggers = LoadTriggers(chromaPath);

            if (triggers == null || triggers.Count == 0)
                return;
            
            (string, NoteColorType)[] pairs = {
                ("ChromaNoteA", NoteColorType.NoteA),
                ("ChromaNoteB", NoteColorType.NoteB),
                ("ChromaBeat", NoteColorType.Beat),
                ("ChromaSpinLeft", NoteColorType.SpinLeft),
                ("ChromaSpinRight", NoteColorType.SpinRight),
                ("ChromaScratch", NoteColorType.Scratch),
                ("ChromaAncillary", NoteColorType.Ancillary),
            };

            int totalCount = 0;
            foreach (var pair in pairs)
            {
                TriggerManager.ClearTriggers(pair.Item1);
                if (triggers.TryGetValue(pair.Item2, out var list))
                {
                    TriggerManager.LoadTriggers(list.ToArray<ITrigger>(), pair.Item1);
                    totalCount += list.Count;
                }
            }
            Main.Log("Applied " + totalCount + " triggers from file " + filename + ".chroma");
        }

        private static Dictionary<NoteColorType, List<ChromaTrigger>> LoadTriggers(string path)
        {
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
                    string colStr = elems[2];
                    if (colStr == "default")
                    {
                        var col = ChromaManager.GetDefaultColorForNoteType(noteType);
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
                }

                // Second line check
                if (elems.Length < 5)
                    continue;

                noteType = Util.GetNoteTypeForString(elems[0]);
                float startTime = float.Parse(elems[1]);
                float endTime = float.Parse(elems[2]);
                string startCol = elems[3];
                string endCol = elems[4];

                trigger.Time = startTime;
                trigger.Duration = endTime - startTime;
                trigger.StartColor = startCol.ToLower() == "default" ? ChromaManager.GetDefaultColorForNoteType(noteType) : HslColor.FromHexRgb(startCol);
                trigger.EndColor = endCol.ToLower() == "default" ? ChromaManager.GetDefaultColorForNoteType(noteType) : HslColor.FromHexRgb(endCol);
                trigger.EnsureSmoothTransition();
                if (!dict.TryGetValue(noteType, out var list2))
                {
                    list2 = new List<ChromaTrigger>();
                    dict.Add(noteType, list2);
                }

                list2.Add(trigger);
            }

            foreach (var pair in dict)
            {
                pair.Value.Sort((t1, t2) => t1.Time.CompareTo(t2.Time));
            }
            return dict;
        }
    }
}