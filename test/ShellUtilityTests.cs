using Xunit;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Ragnar.Tests;

public class ShellUtilityTests : TestBase
{
    private struct Sandbox : IDisposable
    {
        public string Path { get; }
        private readonly string _origDir;

        public Sandbox()
        {
            _origDir = System.IO.Directory.GetCurrentDirectory();
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RagnarSandbox_" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(Path);
            System.IO.Directory.SetCurrentDirectory(Path);
        }

        public void Dispose()
        {
            System.IO.Directory.SetCurrentDirectory(_origDir);
            try
            {
                if (System.IO.Directory.Exists(Path))
                    System.IO.Directory.Delete(Path, true);
            }
            catch {}
        }
    }

    [Fact]
    public void What_Dir_Returns_Current_Directory()
    {
        using var sandbox = new Sandbox();
        var (result, _) = Run("what-dir");
        var fileVal = Assert.IsType<File>(result);
        Assert.Equal(sandbox.Path.Replace("\\", "/").TrimEnd('/'), fileVal.Path.Replace("\\", "/").TrimEnd('/'));
    }

    [Fact]
    public void Pwd_Is_Alias_For_What_Dir()
    {
        using var sandbox = new Sandbox();
        var (result, _) = Run("pwd");
        var fileVal = Assert.IsType<File>(result);
        Assert.Equal(sandbox.Path.Replace("\\", "/").TrimEnd('/'), fileVal.Path.Replace("\\", "/").TrimEnd('/'));
    }
    
    [Fact]
    public void Cd_ChangesDirectory_And_ReturnsFilePath()
    {
        using var sandbox = new Sandbox();
        string sub = System.IO.Path.Combine(sandbox.Path, "sub");
        System.IO.Directory.CreateDirectory(sub);
        
        var (result, _) = Run("cd %sub");
        var fileVal = Assert.IsType<File>(result);
        Assert.Equal(sub.Replace("\\", "/").TrimEnd('/'), fileVal.Path.Replace("\\", "/").TrimEnd('/'));
        Assert.Equal(sub.Replace("\\", "/").TrimEnd('/'), System.IO.Directory.GetCurrentDirectory().Replace("\\", "/").TrimEnd('/'));
    }
    
    [Fact]
    public void Ls_ListsFiles_As_FileTypes()
    {
        using var sandbox = new Sandbox();
        System.IO.File.WriteAllText(System.IO.Path.Combine(sandbox.Path, "file.txt"), "hello");
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(sandbox.Path, "dir"));
        
        var (result, _) = Run("ls");
        var blockVal = Assert.IsType<Block>(result);
        var fileNames = blockVal.Children.Select(c => Assert.IsType<File>(c).Path).ToList();
        
        Assert.Contains("file.txt", fileNames);
        Assert.Contains("dir/", fileNames);
    }
    
    [Fact]
    public void Ls_FiltersHiddenFiles_UnlessAll()
    {
        using var sandbox = new Sandbox();
        System.IO.File.WriteAllText(System.IO.Path.Combine(sandbox.Path, "file.txt"), "hello");
        
        // Hidden file
        string hiddenPath = System.IO.Path.Combine(sandbox.Path, ".hidden.txt");
        System.IO.File.WriteAllText(hiddenPath, "secret");
        if (OperatingSystem.IsWindows())
        {
            System.IO.File.SetAttributes(hiddenPath, System.IO.FileAttributes.Hidden);
        }
        
        // Test ls default (hides .hidden.txt)
        var (result1, _) = Run("ls");
        var files1 = ((Block)result1).Children.Select(c => ((File)c).Path).ToList();
        Assert.Contains("file.txt", files1);
        Assert.DoesNotContain(".hidden.txt", files1);
        
        // Test ls/all (shows .hidden.txt)
        var (result2, _) = Run("ls/all");
        var files2 = ((Block)result2).Children.Select(c => ((File)c).Path).ToList();
        Assert.Contains("file.txt", files2);
        Assert.Contains(".hidden.txt", files2);
    }
    
    [Fact]
    public void Mkdir_CreatesDirectories_And_SupportsVerbose()
    {
        using var sandbox = new Sandbox();
        // Creates recursive directory levels
        var (res, output) = RunWithOutput("mkdir/verbose %level1/level2");
        
        Assert.True(System.IO.Directory.Exists(System.IO.Path.Combine(sandbox.Path, "level1", "level2")));
        Assert.Contains("Creating directory: level1/level2", output);
    }
    
    [Fact]
    public void Rmdir_DeletesEmptyDirectories_ThrowsOnNonEmpty()
    {
        using var sandbox = new Sandbox();
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(sandbox.Path, "empty"));
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(sandbox.Path, "non-empty"));
        System.IO.File.WriteAllText(System.IO.Path.Combine(sandbox.Path, "non-empty", "file.txt"), "xyz");
        
        // Test success deleting empty directory
        Run("rmdir %empty");
        Assert.False(System.IO.Directory.Exists(System.IO.Path.Combine(sandbox.Path, "empty")));
        
        // Test error deleting non-empty directory
        Assert.ThrowsAny<Exception>(() => Run("rmdir %non-empty"));
        Assert.True(System.IO.Directory.Exists(System.IO.Path.Combine(sandbox.Path, "non-empty")));
    }
    
    [Fact]
    public void Rm_DeletesFiles_And_RecursivelyDeletesDirectories()
    {
        using var sandbox = new Sandbox();
        System.IO.File.WriteAllText(System.IO.Path.Combine(sandbox.Path, "file.txt"), "xyz");
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(sandbox.Path, "dir"));
        System.IO.File.WriteAllText(System.IO.Path.Combine(sandbox.Path, "dir", "file2.txt"), "abc");
        
        // rm without /r doesn't delete directory
        RunWithOutput("rm %dir");
        Assert.True(System.IO.Directory.Exists(System.IO.Path.Combine(sandbox.Path, "dir")));
        
        // rm deletes file
        Run("rm %file.txt");
        Assert.False(System.IO.File.Exists(System.IO.Path.Combine(sandbox.Path, "file.txt")));
        
        // rm/recursive deletes directory
        Run("rm/recursive %dir");
        Assert.False(System.IO.Directory.Exists(System.IO.Path.Combine(sandbox.Path, "dir")));
    }

    [Fact]
    public void Rm_SupportsGlobs()
    {
        using var sandbox = new Sandbox();
        System.IO.File.WriteAllText(System.IO.Path.Combine(sandbox.Path, "test1.txt"), "1");
        System.IO.File.WriteAllText(System.IO.Path.Combine(sandbox.Path, "test2.txt"), "2");
        System.IO.File.WriteAllText(System.IO.Path.Combine(sandbox.Path, "ignore.log"), "3");
        
        Run("rm %test*.txt");
        
        Assert.False(System.IO.File.Exists(System.IO.Path.Combine(sandbox.Path, "test1.txt")));
        Assert.False(System.IO.File.Exists(System.IO.Path.Combine(sandbox.Path, "test2.txt")));
        Assert.True(System.IO.File.Exists(System.IO.Path.Combine(sandbox.Path, "ignore.log")));
    }
    
    [Fact]
    public void Pushd_And_Popd_ManagesDirectoryStack()
    {
        using var sandbox = new Sandbox();
        string d1 = System.IO.Path.Combine(sandbox.Path, "dir1");
        string d2 = System.IO.Path.Combine(sandbox.Path, "dir2");
        System.IO.Directory.CreateDirectory(d1);
        System.IO.Directory.CreateDirectory(d2);
        
        Run("pushd %dir1");
        Assert.Equal(d1.Replace("\\", "/").TrimEnd('/'), System.IO.Directory.GetCurrentDirectory().Replace("\\", "/").TrimEnd('/'));
        
        Run("pushd %../dir2");
        Assert.Equal(d2.Replace("\\", "/").TrimEnd('/'), System.IO.Directory.GetCurrentDirectory().Replace("\\", "/").TrimEnd('/'));
        
        Run("popd");
        Assert.Equal(d1.Replace("\\", "/").TrimEnd('/'), System.IO.Directory.GetCurrentDirectory().Replace("\\", "/").TrimEnd('/'));
        
        Run("popd");
        Assert.Equal(sandbox.Path.Replace("\\", "/").TrimEnd('/'), System.IO.Directory.GetCurrentDirectory().Replace("\\", "/").TrimEnd('/'));
    }

    [Fact]
    public void Mv_MovesFile_And_Directory_And_SupportsForce()
    {
        using var sandbox = new Sandbox();
        System.IO.File.WriteAllText(System.IO.Path.Combine(sandbox.Path, "foo.txt"), "content");
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(sandbox.Path, "sub"));
        
        // 1. Move file into directory: mv %foo.txt %sub/ -> sub/foo.txt
        Run("mv %foo.txt %sub/");
        Assert.False(System.IO.File.Exists(System.IO.Path.Combine(sandbox.Path, "foo.txt")));
        Assert.True(System.IO.File.Exists(System.IO.Path.Combine(sandbox.Path, "sub", "foo.txt")));
        
        // 2. Rename file: mv %sub/foo.txt %sub/bar.txt
        Run("mv %sub/foo.txt %sub/bar.txt");
        Assert.False(System.IO.File.Exists(System.IO.Path.Combine(sandbox.Path, "sub", "foo.txt")));
        Assert.True(System.IO.File.Exists(System.IO.Path.Combine(sandbox.Path, "sub", "bar.txt")));
        
        // 3. Move folder: mv %sub %sub2
        Run("mv %sub %sub2");
        Assert.False(System.IO.Directory.Exists(System.IO.Path.Combine(sandbox.Path, "sub")));
        Assert.True(System.IO.Directory.Exists(System.IO.Path.Combine(sandbox.Path, "sub2")));
        Assert.True(System.IO.File.Exists(System.IO.Path.Combine(sandbox.Path, "sub2", "bar.txt")));
        
        // 4. Test error on existing destination without force
        System.IO.File.WriteAllText(System.IO.Path.Combine(sandbox.Path, "foo.txt"), "fresh");
        Assert.ThrowsAny<Exception>(() => Run("mv %foo.txt %sub2/bar.txt"));
        
        // 5. Test overwrite with force
        Run("mv/force %foo.txt %sub2/bar.txt");
        Assert.False(System.IO.File.Exists(System.IO.Path.Combine(sandbox.Path, "foo.txt")));
        Assert.Equal("fresh", System.IO.File.ReadAllText(System.IO.Path.Combine(sandbox.Path, "sub2", "bar.txt")));
    }

    [Fact]
    public void Cp_CopiesFiles_SupportsForce_And_Globs()
    {
        using var sandbox = new Sandbox();
        System.IO.File.WriteAllText(System.IO.Path.Combine(sandbox.Path, "foo.txt"), "content");
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(sandbox.Path, "sub"));
        
        // 1. Copy file to new path
        Run("cp %foo.txt %bar.txt");
        Assert.True(System.IO.File.Exists(System.IO.Path.Combine(sandbox.Path, "foo.txt")));
        Assert.True(System.IO.File.Exists(System.IO.Path.Combine(sandbox.Path, "bar.txt")));
        Assert.Equal("content", System.IO.File.ReadAllText(System.IO.Path.Combine(sandbox.Path, "bar.txt")));
        
        // 2. Copy file into directory
        Run("cp %foo.txt %sub/");
        Assert.True(System.IO.File.Exists(System.IO.Path.Combine(sandbox.Path, "sub", "foo.txt")));
        
        // 3. Test overwrite error without /force
        System.IO.File.WriteAllText(System.IO.Path.Combine(sandbox.Path, "foo.txt"), "fresh");
        Assert.ThrowsAny<Exception>(() => Run("cp %foo.txt %bar.txt"));
        
        // 4. Test overwrite success with /force
        Run("cp/force %foo.txt %bar.txt");
        Assert.Equal("fresh", System.IO.File.ReadAllText(System.IO.Path.Combine(sandbox.Path, "bar.txt")));
        
        // 5. Test glob copy
        System.IO.File.WriteAllText(System.IO.Path.Combine(sandbox.Path, "test1.txt"), "t1");
        System.IO.File.WriteAllText(System.IO.Path.Combine(sandbox.Path, "test2.txt"), "t2");
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(sandbox.Path, "dest-dir"));
        
        Run("cp %test*.txt %dest-dir/");
        Assert.True(System.IO.File.Exists(System.IO.Path.Combine(sandbox.Path, "dest-dir", "test1.txt")));
        Assert.True(System.IO.File.Exists(System.IO.Path.Combine(sandbox.Path, "dest-dir", "test2.txt")));
        Assert.Equal("t1", System.IO.File.ReadAllText(System.IO.Path.Combine(sandbox.Path, "dest-dir", "test1.txt")));
    }

    [Fact]
    public void Zip_And_Unzip_CompressesAndExtractsFiles()
    {
        using var sandbox = new Sandbox();
        
        // Setup source directory and files
        string srcDir = System.IO.Path.Combine(sandbox.Path, "src-folder");
        System.IO.Directory.CreateDirectory(srcDir);
        System.IO.File.WriteAllText(System.IO.Path.Combine(srcDir, "a.txt"), "hello a");
        System.IO.File.WriteAllText(System.IO.Path.Combine(srcDir, "b.txt"), "hello b");

        // Setup destination directories
        string destDir = System.IO.Path.Combine(sandbox.Path, "extracted");
        
        // 1. Basic zip creation
        Run("zip %archive.zip %src-folder");
        Assert.True(System.IO.File.Exists(System.IO.Path.Combine(sandbox.Path, "archive.zip")));

        // 2. Unzip extraction
        Run("unzip %archive.zip %extracted");
        Assert.True(System.IO.File.Exists(System.IO.Path.Combine(destDir, "a.txt")));
        Assert.True(System.IO.File.Exists(System.IO.Path.Combine(destDir, "b.txt")));
        Assert.Equal("hello a", System.IO.File.ReadAllText(System.IO.Path.Combine(destDir, "a.txt")));

        // 3. Test overwrite error on zip without /force
        Assert.ThrowsAny<Exception>(() => Run("zip %archive.zip %src-folder"));

        // 4. Test overwrite success on zip with /force
        Run("zip/force %archive.zip %src-folder");

        // 5. Test overwrite error on unzip without /force
        Assert.ThrowsAny<Exception>(() => Run("unzip %archive.zip %extracted"));

        // 6. Test overwrite success on unzip with /force
        Run("unzip/force %archive.zip %extracted");

        // 7. Test verbose output
        var (_, zipOut) = RunWithOutput("zip/force/verbose %archive.zip %src-folder");
        Assert.Contains("Adding file: a.txt", zipOut);

        var (_, unzipOut) = RunWithOutput("unzip/force/verbose %archive.zip %extracted");
        Assert.Contains("Extracting: a.txt", unzipOut);
    }
}
