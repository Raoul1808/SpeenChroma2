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
                    return "Beat Bar";
                case ChromaNoteType.SpinLeft:
                    return "Left Spin";
                case ChromaNoteType.SpinRight:
                    return "Right Spin";
                case ChromaNoteType.Scratch:
                    return "Scratch";
                case ChromaNoteType.Ancillary:
                    return "Highlights";
                case ChromaNoteType.All:
                    return "All";
                default:
                    throw new ArgumentOutOfRangeException(nameof(noteType), noteType, null);
            }
        }

        public static NoteColorType GetNoteTypeForString(string noteType)
        {
            switch (noteType.ToLower())
            {
                case "notea":
                    return NoteColorType.NoteA;
                case "noteb":
                    return NoteColorType.NoteB;
                case "beat":
                    return NoteColorType.Beat;
                case "spinleft":
                case "leftspin":
                    return NoteColorType.SpinLeft;
                case "spinright":
                case "rightspin":
                    return NoteColorType.SpinRight;
                case "scratch":
                    return NoteColorType.Scratch;
                case "ancillary":
                case "highlights":
                    return NoteColorType.Ancillary;
                default:
                    throw new Exception("No note type available for string '" + noteType + "'");
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
