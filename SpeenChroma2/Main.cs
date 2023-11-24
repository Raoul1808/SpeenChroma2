using BepInEx;

namespace SpeenChroma2
{
    [BepInPlugin(Guid, Name, Version)]
    public class Main : BaseUnityPlugin
    {
        private const string Guid = "srxd.raoul1808.speenchroma2";
        private const string Name = "Speen Chroma 2";
        private const string Version = "0.1.0";
        
        private void Awake()
        {
            Logger.LogMessage("Hi from Speen Chroma 2!");
        }
    }
}
