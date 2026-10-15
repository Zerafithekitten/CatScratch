using AionMeter.Core.Game;

namespace AionMeter.Tests;

public sealed class GameDataLoadTests
{
    [Fact]
    public void Loading_without_per_entry_clones_keeps_localization_overrides_and_owned_values()
    {
        var root = Path.Combine(Path.GetTempPath(), "catscratch-data-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(root,"skills")); Directory.CreateDirectory(Path.Combine(root,"npcs"));
            File.WriteAllText(Path.Combine(root,"skills","en.json"), "{\"100\":\"English\",\"101\":\"Fallback\"}");
            File.WriteAllText(Path.Combine(root,"skills","de.json"), "{\"100\":\"Deutsch\"}");
            File.WriteAllText(Path.Combine(root,"skills.json"), "{\"101\":\"Override\"}");
            File.WriteAllText(Path.Combine(root,"npcs","en.json"), "{\"500\":{\"name\":\"Boss\",\"isBoss\":true}}");
            File.WriteAllText(Path.Combine(root,"npcs","de.json"), "{\"500\":\"Bösewicht\"}");
            File.WriteAllText(Path.Combine(root,"servers.json"), "{\"servers\":{\"2305\":{\"en\":\"English server\",\"de\":\"Deutscher Server\"}}}");
            var data = GameData.Load(root,"de");
            GC.Collect();
            Assert.Equal("Deutsch",data.Skills[100]); Assert.Equal("Override",data.Skills[101]);
            Assert.Equal("Bösewicht",data.Npcs[500].Name); Assert.True(data.Npcs[500].IsBoss);
            Assert.Equal("Deutscher Server",data.Servers[2305]);
        }
        finally { Directory.Delete(root,true); }
    }
}
