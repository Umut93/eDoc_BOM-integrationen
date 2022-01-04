using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;


namespace Fujitsu.eDoc.BOM.Unittest
{
    [TestClass]
    public class BOMMessageHandlerTest
    {
        [TestMethod, TestCategory("OnBuild")]
        public void TestHandleNewMessages_CreateLatestHighWaterMark_When_NoRecordsFoundinFUBomMessage()
        {
            using (ShimsContext.Create())
            {
                //Arrange
                bool isSaved = false;

                Fujitsu.eDoc.BOM.Fakes.ShimBOMConfigHandler.GetBOMConfiguration = () => { return new BOMConfiguration(); };
                Fujitsu.eDoc.BOM.Fakes.ShimBOMMessageHandler.GetHighWatermark = () => { return null; };
                Fujitsu.eDoc.BOM.Fakes.ShimBOMConfiguration.AllInstances.GetStartDateTime = (cfg) => { return System.DateTime.MinValue; };

                Fujitsu.eDoc.BOM.Fakes.ShimBOMCaller.GetLatestHighWaterMark = () => { return Guid.NewGuid(); };
                Fujitsu.eDoc.BOM.Fakes.ShimBOMCaller.GetMunicipalityCVR = () => { return "86631628"; };

                Fujitsu.eDoc.BOM.Fakes.ShimBOMMessageHandler.SaveMessageBeskedNullableOfGuid = (msg, guid) => { isSaved = true; return "Latest HighWaterMarkCreated in DB"; };

                //Act
                System.Reflection.MethodInfo dynMethod = typeof(BOMMessageHandler).GetMethod("HandleNewMessages", BindingFlags.Static | BindingFlags.Public);
                dynMethod.Invoke(new object { }, new object[] { });

                //Assertion
                Assert.IsTrue(isSaved);
            }
        }
    }
}