using AionMeter.Core.Updates;

namespace AionMeter.Tests;

public sealed class UpdateCleanupTests
{
    [Fact]
    public void RemovesOnlyTheSpecifiedStageAndPreservesUserFiles()
    {
        var root=Path.Combine(Path.GetTempPath(),"catscratch-clean-"+Guid.NewGuid().ToString("N"));
        var updates=Path.Combine(root,"userdata","temp","updates");
        var stage=Path.Combine(updates,"stage-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(stage,"backup"));
        File.WriteAllText(Path.Combine(stage,"backup","old.dll"),"old");
        File.WriteAllText(Path.Combine(root,"userdata","settings.json"),"keep");
        File.WriteAllText(Path.Combine(updates,"CatScratch-win-x64.zip"),"package");
        File.WriteAllText(Path.Combine(updates,"notes.txt"),"keep");
        try
        {
            UpdateCleanup.RemoveStage(updates,stage);
            Assert.True(File.Exists(Path.Combine(stage,"backup","old.dll")));
            UpdateCleanup.PruneOlderStages(updates,DateTime.UtcNow.AddMinutes(1));
            Assert.True(Directory.Exists(stage));
            File.WriteAllText(Path.Combine(stage,"started.txt"),"New version started successfully");
            UpdateCleanup.RemoveStage(updates,stage);
            UpdateCleanup.RemoveDownload(updates,"CatScratch-win-x64.zip");
            Assert.False(Directory.Exists(stage));
            Assert.False(File.Exists(Path.Combine(updates,"CatScratch-win-x64.zip")));
            Assert.Equal("keep",File.ReadAllText(Path.Combine(root,"userdata","settings.json")));
            Assert.Equal("keep",File.ReadAllText(Path.Combine(updates,"notes.txt")));
            Assert.Throws<InvalidDataException>(()=>UpdateCleanup.RemoveStage(updates,root));
            Assert.Throws<InvalidDataException>(()=>UpdateCleanup.RemoveStage(updates,Path.Combine(updates,"stage-not-a-guid")));
            Assert.Throws<InvalidDataException>(()=>UpdateCleanup.RemoveDownload(updates,"notes.txt"));
        }
        finally { Directory.Delete(root,true); }
    }

    [Fact]
    public void FailedUpdateRetainsRecoveryFiles()
    {
        var root=Path.Combine(Path.GetTempPath(),"catscratch-clean-"+Guid.NewGuid().ToString("N"));
        var stage=Path.Combine(root,"stage-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stage); File.WriteAllText(Path.Combine(stage,"failure.txt"),"failed");
        try { UpdateCleanup.RemoveStage(root,stage); Assert.True(File.Exists(Path.Combine(stage,"failure.txt"))); }
        finally { Directory.Delete(root,true); }
    }

    [Fact]
    public void CleanupDoesNotRemoveANewerDownload()
    {
        var root=Path.Combine(Path.GetTempPath(),"catscratch-clean-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path=Path.Combine(root,"CatScratch-win-x64.zip");
        var cutoff=DateTime.UtcNow.AddMinutes(-1);
        File.WriteAllText(path,"new package");
        try { UpdateCleanup.RemoveDownload(root,"CatScratch-win-x64.zip",cutoff); Assert.Equal("new package",File.ReadAllText(path)); }
        finally { Directory.Delete(root,true); }
    }

    [Fact]
    public void PrunesOldStagesButRetainsFailureRecoveryAndUnrelatedFolders()
    {
        var root=Path.Combine(Path.GetTempPath(),"catscratch-clean-"+Guid.NewGuid().ToString("N"));
        var old=Path.Combine(root,"stage-"+Guid.NewGuid().ToString("N"));
        var failed=Path.Combine(root,"stage-"+Guid.NewGuid().ToString("N"));
        var unrelated=Path.Combine(root,"stage-not-an-update");
        Directory.CreateDirectory(old); Directory.CreateDirectory(failed); Directory.CreateDirectory(unrelated);
        File.WriteAllText(Path.Combine(failed,"failure.txt"),"retain recovery");
        try
        {
            UpdateCleanup.PruneOlderStages(root,DateTime.UtcNow.AddMinutes(1));
            Assert.False(Directory.Exists(old)); Assert.True(Directory.Exists(failed)); Assert.True(Directory.Exists(unrelated));
        }
        finally { Directory.Delete(root,true); }
    }
}
