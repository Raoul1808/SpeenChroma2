using System;

namespace SpeenChroma2
{
    public static class Util
    {
        public static string GetName(this ChromaNoteType noteType)
        {
            switch (noteType)
            {
                case ChromaNoteType.NoteA:
                    return "Note A";
                case ChromaNoteType.NoteB:
                    return "Note B";
                case ChromaNoteType.Beat:
                    return "Beat";
                case ChromaNoteType.SpinLeft:
                    return "Spin Left";
                case ChromaNoteType.SpinRight:
                    return "Spin Right";
                case ChromaNoteType.Scratch:
                    return "Scratch";
                case ChromaNoteType.Ancillary:
                    return "Ancillary";
                case ChromaNoteType.All:
                    return "All";
                default:
                    throw new ArgumentOutOfRangeException(nameof(noteType), noteType, null);
            }
        }

        public static NoteColorType ToNoteColorType(this ChromaNoteType noteType)
        {
            switch (noteType)
            {
                case ChromaNoteType.NoteA:
                    return NoteColorType.NoteA;
                case ChromaNoteType.NoteB:
                    return NoteColorType.NoteB;
                case ChromaNoteType.Beat:
                    return NoteColorType.Beat;
                case ChromaNoteType.SpinLeft:
                    return NoteColorType.SpinLeft;
                case ChromaNoteType.SpinRight:
                    return NoteColorType.SpinRight;
                case ChromaNoteType.Scratch:
                    return NoteColorType.Scratch;
                case ChromaNoteType.Ancillary:
                    return NoteColorType.Ancillary;
                case ChromaNoteType.All:
                    return NoteColorType.Default;
                default:
                    throw new ArgumentOutOfRangeException(nameof(noteType), noteType, null);
            }
        }
    }
}
