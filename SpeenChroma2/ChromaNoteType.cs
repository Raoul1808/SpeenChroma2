using System;

namespace SpeenChroma2
{
    [Flags]
    public enum ChromaNoteType
    {
        NoteA = 0x01,
        NoteB = 0x02,
        Beat = 0x04,
        SpinLeft = 0x08,
        SpinRight = 0x10,
        Scratch = 0x20,
        Ancillary = 0x40,
        All = NoteA | NoteB | Beat | SpinLeft | SpinRight | Scratch | Ancillary,
    }
}
