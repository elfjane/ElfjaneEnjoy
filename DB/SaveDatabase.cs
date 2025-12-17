using System.Collections.Generic;
using System.IO;
using System.Text.Json;


namespace ElfjaneEnjoy.DB
{
    internal class SaveDatabase
    {
        public static readonly string ConfigPath = Path.Combine(BepInEx.Paths.ConfigPath, "Notify");

        public static void SaveVBloodNotifyIgnoreConfig(Dictionary<string, bool> VBloodAnnounceIgnoreUsers)
        {
            var jsonOutPut = System.Text.Json.JsonSerializer.Serialize(VBloodAnnounceIgnoreUsers, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(ConfigPath, "vbloodannounce_ignore_users.json"), jsonOutPut);
        }
    }
}
