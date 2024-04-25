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
            public bool Negative { get; set; }
        }

        public static void Setup()
        {
            TriggerManager.OnChartLoad += path =>
            {
                ITrigger[] triggers = {
                    new ChromaTrigger
                    {
                        Time = 2f,
                        Duration = 4f,
                        StartColor = new HslColor
                        {
                            Hue = 0.3f,
                            Saturation = 1f,
                            Lightness = 0.5f,
                        },
                        EndColor = new HslColor
                        {
                            Hue = 0.7f,
                            Saturation = 1f,
                            Lightness = 0.5f,
                        },
                        Negative = false,
                    }
                };
                TriggerManager.LoadTriggers(triggers, "ChromaNoteA");
            };
            
            TriggerManager.RegisterTriggerEvent("ChromaNoteA", (trigger, time) =>
            {
                var chromaTrigger = (ChromaTrigger)trigger;
                float timeOne = (time - chromaTrigger.Time) / chromaTrigger.Duration;
                var col = HslColor.Lerp(chromaTrigger.StartColor, chromaTrigger.EndColor, timeOne);
                ChromaManager.SetColorForNoteType(NoteColorType.NoteA, col);
            });
        }
    }
}
