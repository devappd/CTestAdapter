using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using EnvDTE;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace CTestAdapter
{
  internal static class TestContainerHelper
  {
    public const string TestFileExtension = ".cmake";
    public const string TestFileName = "CTestTestfile";

    private const string FieldNameTestname = "testname";
    private const string FieldNameCfgRegex = "cfgregex";

    /**
     * @brief verifies the file extension is .cmake
     */
    public static bool IsTestContainerFile(string file)
    {
      try
      {
        return TestContainerHelper.TestFileExtension.Equals(
          Path.GetExtension(file),
          StringComparison.OrdinalIgnoreCase);
      }
      catch (Exception /*e*/)
      {
        // TODO do some messaging here or so ...
      }
      return false;
    }

    /**
     * @brief recursively collects all test container files that can be found
     */
    public static IEnumerable<string> CollectCTestTestfiles(string currentDir)
    {
      var file = new FileInfo(Path.Combine(currentDir, TestContainerHelper.TestFileName + TestContainerHelper.TestFileExtension));
      if (!file.Exists)
      {
        return Enumerable.Empty<string>();
      }
      var res = new List<string>();
      res.Add(file.FullName);
      return res;
    }

    public static string FindCTestExe(string basePath)
    {
      var file = new FileInfo(Path.Combine(basePath, Constants.CTestExecutableName));
      if (file.Exists)
      {
        return file.FullName;
      }
      var cdir = new DirectoryInfo(basePath);
      var subdirs = cdir.GetDirectories();
      foreach (var dir in subdirs)
      {
        var res = TestContainerHelper.FindCTestExe(dir.FullName);
        if (res != string.Empty)
        {
          return res;
        }
      }
      return string.Empty;
    }

    public static List<TestCase> FindAllTests(CTestAdapterConfig cfg, string source, IMessageLogger log)
    {
      List<TestCase> result = new List<TestCase>();
      if (!File.Exists(cfg.CTestExecutable))
      {
        return result;
      }
      if (!Directory.Exists(cfg.CacheDir))
      {
        return result;
      }
      var args = "-N --show-only=json-v1";
      if (!string.IsNullOrWhiteSpace(cfg.ActiveConfiguration))
      {
        args += " -C ";
        args += cfg.ActiveConfiguration;
      }
      var proc = new System.Diagnostics.Process
      {
        StartInfo = new ProcessStartInfo()
        {
          FileName = cfg.CTestExecutable,
          WorkingDirectory = cfg.CacheDir,
          Arguments = args,
          CreateNoWindow = true,
          RedirectStandardError = true,
          RedirectStandardOutput = true,
          WindowStyle = ProcessWindowStyle.Hidden,
          UseShellExecute = false,
          StandardOutputEncoding = Encoding.UTF8,
          StandardErrorEncoding = Encoding.UTF8
        }
      };
      try
      {
        proc.Start();
        var output = proc.StandardOutput.ReadToEnd();
        var ctestInfo = JsonSerializer.Deserialize<CTestInfo>(output);
        int index = 0;
        foreach(var test in ctestInfo.tests)
        {
          var testcase = new TestCase(test.name, CTestExecutor.ExecutorUri, source);
          if (ctestInfo.backtraceGraph.nodes.Length > test.backtrace)
          {
            var backtrace = ctestInfo.backtraceGraph.nodes[test.backtrace];
            if (ctestInfo.backtraceGraph.files.Length > backtrace.file)
            {
              var file = ctestInfo.backtraceGraph.files[backtrace.file];
              testcase.CodeFilePath = file;
              testcase.LineNumber = backtrace.line;
            }
          }
          testcase.DisplayName = String.Format("#{0}: {1}", ++index, test.name);
          testcase.LocalExtensionData = index;
          result.Add(testcase);
        }
      }
      catch(Exception e)
      {
        log.SendMessage(TestMessageLevel.Error, "Could not parse CTest JSON output: " + e.ToString());
        return result;
      }
      finally
      {
        proc.Dispose();
      }
      return result;
    }

    public static string FindCMakeCacheDirectory(string fileOrDirectory)
    {
      // if a file is given, remove the filename before processing
      if (File.Exists(fileOrDirectory))
      {
        var finfo = new FileInfo(fileOrDirectory);
        if (null != finfo.DirectoryName)
        {
          fileOrDirectory = finfo.DirectoryName;
        }
      }
      if (!File.Exists(Path.Combine(fileOrDirectory, Constants.CTestTestFileName)))
      {
        // we are not within a testable cmake build tree
        return string.Empty;
      }
      while (true)
      {
        var info = new DirectoryInfo(fileOrDirectory);
        if (File.Exists(info.FullName + "\\" + Constants.CMakeCacheFilename))
        {
          return info.FullName;
        }
        if (!info.Exists)
        {
          return string.Empty;
        }
        if (info.Parent == null)
        {
          return string.Empty;
        }
        fileOrDirectory = info.Parent.FullName;
      }
    }

    public static string ToLinkPath(string pathName)
    {
      return "file://" + pathName.Replace(" ", "%20");
    }
  }
}
