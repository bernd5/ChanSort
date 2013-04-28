﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChanSort.Loader.Samsung;

namespace Test.Loader
{
  [TestClass]
  public class SamsungTest : LoaderTestBase
  {
    #region InitExpectedSamsungData()
    private Dictionary<string, ExpectedData> InitExpectedSamsungData()
    {
      var expected = new[]
                       {
                         new ExpectedData(@"catmater_B\Clone.scm", 31, 272, 0, 0, 0) ,
                         new ExpectedData(@"easy2003_B\easy2003_B.scm", 0, 0, 1225, 0, 0) ,
                         new ExpectedData(@"_Manu_C\channel_list_LE40C650_1001.scm", 0, 9, 0, 0, 0) 
                       };

      var dict = new Dictionary<string, ExpectedData>(StringComparer.InvariantCultureIgnoreCase);
      foreach (var entry in expected)
        dict[entry.File] = entry;
      return dict;
    }

    #endregion

    #region TestSamsungScmLoader()
    [TestMethod]
    [DeploymentItem("ChanSort.Loader.Samsung\\ChanSort.Loader.Samsung.ini")]
    public void TestSamsungScmLoader()
    {
      var expectedData = this.InitExpectedSamsungData();
      ScmSerializerPlugin plugin = new ScmSerializerPlugin();

      StringBuilder errors = new StringBuilder();
      var list = this.FindAllFiles("TestFiles_Samsung", "*.scm");
      var models = new Dictionary<string, string>();
      foreach (var file in list)
      {
        Debug.Print("Testing " + file);
        try
        {
          var serializer = plugin.CreateSerializer(file) as ScmSerializer;
          Assert.IsNotNull(serializer, "No Serializer for " + file);

          serializer.Load();

          var fileName = Path.GetFileName(file) ?? "";          
          string key = fileName.StartsWith("channel_list_") ? fileName.Substring(13, fileName.IndexOf('_',14)-13) : fileName;
          
          string relPath = Path.GetFileName(Path.GetDirectoryName(file)) + "\\" + fileName;
          models[key] = relPath;

          Assert.IsFalse(serializer.DataRoot.IsEmpty, "No channels loaded from " + file);

          ExpectedData exp;
          key = Path.GetFileName(Path.GetDirectoryName(file)) + "\\" + Path.GetFileName(file);
          if (expectedData.TryGetValue(key, out exp))
          {
            var analogTv = serializer.DataRoot.GetChannelList(ChanSort.Api.SignalSource.AnalogC | ChanSort.Api.SignalSource.TvAndRadio);
            var dtvTv = serializer.DataRoot.GetChannelList(ChanSort.Api.SignalSource.DvbCT | ChanSort.Api.SignalSource.TvAndRadio);
            var satTv = serializer.DataRoot.GetChannelList(ChanSort.Api.SignalSource.DvbS | ChanSort.Api.SignalSource.TvAndRadio);
            expectedData.Remove(key);
            if (exp.AnalogChannels != 0 || analogTv != null)
              Assert.AreEqual(exp.AnalogChannels, analogTv.Channels.Count, file + ": analog");
            if (exp.DtvChannels != 0 || dtvTv != null)
              Assert.AreEqual(exp.DtvChannels, dtvTv.Channels.Count, file + ": DTV");
            if (exp.SatChannels != 0 || satTv != null)
              Assert.AreEqual(exp.SatChannels, satTv.Channels.Count, file + ": Sat");
          }
        }
        catch (Exception ex)
        {
          errors.AppendLine();
          errors.AppendLine();
          errors.AppendLine(file);
          errors.AppendLine(ex.ToString());
        }
      }

      foreach (var model in models.OrderBy(e => e.Key))
        Debug.WriteLine(model.Key + "\t" + model.Value);

      if (expectedData.Count > 0)
        Assert.Fail("Some files were not tested: " + expectedData.Keys.Aggregate((prev, cur) => prev + "," + cur));
      Assert.AreEqual("", errors.ToString());
    }
    #endregion
  }
}