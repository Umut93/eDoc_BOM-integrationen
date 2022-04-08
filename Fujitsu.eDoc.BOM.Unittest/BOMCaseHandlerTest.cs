using Fujitsu.eDoc.BOM.BOMSagsbehandling;
using Fujitsu.eDoc.BOM.CaseHandler;
using Fujitsu.eDoc.BOM.CaseHandler.Fakes;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Policy;

namespace Fujitsu.eDoc.BOM.Unittest
{
    [TestClass]
    public class BOMCaseHandlerTest
    {
        [TestMethod, TestCategory("OnBuild")]
        public void TestBOMCaseExists_IsBOMCaseExisting_When_NOResultFromDB()
        {
            using (ShimsContext.Create())
            {
                //Arrange
                Fujitsu.eDoc.BOM.CaseHandler.BOMCase bomCase = new CaseHandler.BOMCase
                {
                    BOMSagID = Guid.Empty.ToString()
                };

                Fujitsu.eDoc.Core.Fakes.ShimCommon.GetResourceXmlStringStringAssembly = (xmlDocument, resourcePath, assembly) =>
            {
                return string.Empty;
            };

                //Skip DB call
                Fujitsu.eDoc.Core.Fakes.ShimCommon.ExecuteQueryString = (xmlQuery) =>
                {
                    return @"<?xml version='1.0'?> <RECORDS RECORDCOUNT = '0'/>";
                };

                //Act
                System.Reflection.MethodInfo dynMethod = typeof(BOMCaseHandler).GetMethod("BOMCaseExists", BindingFlags.Static | BindingFlags.NonPublic);
                bool result = (bool)dynMethod.Invoke(bomCase, new object[] { bomCase });

                //Assertion
                Assert.IsTrue(result);

            }
        }

        [TestMethod, TestCategory("OnBuild")]
        public void TestBOMCaseExists_IsBOMCaseExisting_When_BOMCaseExistsInDB()
        {
            using (ShimsContext.Create())
            {
                //Arrange
                Fujitsu.eDoc.BOM.CaseHandler.BOMCase bomCase = new CaseHandler.BOMCase
                {
                    BOMSagID = Guid.Empty.ToString(),
                    BOMSubmissionRecno = "123456789",
                };

                Fujitsu.eDoc.Core.Fakes.ShimCommon.GetResourceXmlStringStringAssembly = (xmlDocument, resourcePath, assembly) =>
                {
                    return string.Empty;
                };

                Fujitsu.eDoc.Core.Fakes.ShimCommon.ExecuteQueryString = (xmlQuery) =>
                {
                    return @"<?xml version='1.0'?>
                                            <RECORDS RECORDCOUNT='1'>
                                                <RECORD>
                                                    <Recno>123456789</Recno>
                                                </RECORD>
                                                </RECORDS>";
                };


                //Skip DB execution
                Fujitsu.eDoc.Core.Fakes.ShimCommon.ExecuteSingleActionString = (xmlQuery) =>
                {
                    return string.Empty;
                };

                //Act
                System.Reflection.MethodInfo dynMethod = typeof(BOMCaseHandler).GetMethod("BOMCaseExists", BindingFlags.Static | BindingFlags.NonPublic);
                bool result = (bool)dynMethod.Invoke(bomCase, new object[] { bomCase });

                //Assertion
                Assert.IsFalse(result);

            }

        }

        [TestMethod, TestCategory("OnBuild")]
        public void GetDocumentInfo_fileWithoutExtention_isInvalid()
        {
            using (ShimsContext.Create())
            {
                //Arrange
                BOMConfiguration config = new BOMConfiguration
                {
                    FileFormats = new System.Collections.Generic.List<string>
                    {
                    "pdf",
                    }
                };

                DokumentType doc = new BOMSagsbehandling.DokumentType();

                ShimBOMDocument.Constructor = (@this) =>
                 {
                     doc.DokumentEgenskaber = new DokumentEgenskaberType { BeskrivelseTekst = "Main Document", BrugervendtNoegleTekst = "File", BrevDato = DateTime.Now, TitelTekst = "File" };
                     doc.VariantListe = new VariantType[] { new VariantType { VariantTekst = String.Empty } };
                     doc.VariantListe[0].Del = new DelType[] { new DelType { IndholdTekst = new Uri("about:blank").ToString(), MimeTypeTekst = "application/octet-stream" } };
                 };


                //Act
                MethodInfo dynMethod = typeof(BOMCaseHandler).GetMethod("GetDocumentInfo", BindingFlags.Static | BindingFlags.NonPublic);
                BOMCaseHandler bOMCaseHandler = new BOMCaseHandler();
                BOMDocument result = (BOMDocument)dynMethod.Invoke(bOMCaseHandler, new object[] { config, doc });

                //Assertion
                Assert.IsFalse(result.IsFileTypeValid);

            }
        }


        [TestMethod, TestCategory("OnBuild")]
        public void GetDocumentInfo_fileWithExtention_Valid()
        {
            using (ShimsContext.Create())
            {
                //Arrange
                BOMConfiguration config = new BOMConfiguration
                {
                    FileFormats = new System.Collections.Generic.List<string>
                    {
                    "pdf",
                    }
                };

                DokumentType doc = new BOMSagsbehandling.DokumentType();

                ShimBOMDocument.Constructor = (@this) =>
                 {
                     doc.DokumentEgenskaber = new DokumentEgenskaberType { BeskrivelseTekst = "Test document", BrugervendtNoegleTekst = "Test.pdf", BrevDato = DateTime.Now, TitelTekst = "Test.pdf" };
                     doc.VariantListe = new VariantType[] { new VariantType { VariantTekst = "pdf" } };
                     doc.VariantListe[0].Del = new DelType[] { new DelType { IndholdTekst = new Uri("about:blank").ToString(), MimeTypeTekst = "application/pdf" } };
                 };


                //Act
                MethodInfo dynMethod = typeof(BOMCaseHandler).GetMethod("GetDocumentInfo", BindingFlags.Static | BindingFlags.NonPublic);
                BOMCaseHandler bOMCaseHandler = new BOMCaseHandler();
                BOMDocument result = (BOMDocument)dynMethod.Invoke(bOMCaseHandler, new object[] { config, doc });

                //Assertion
                Assert.IsTrue(result.IsFileTypeValid);
            }
        }

        [TestMethod, TestCategory("OnBuild")]
        public void HandleSubmission_ErrorArose_LogInEventViewer()
        {
            bool isLogged = false;

            using (ShimsContext.Create())
            {
                //Arrange
                Exception ex = new Exception("An error occured on the submission.");
                BOMConfiguration config = new BOMConfiguration { };
                BOMCase bOMCase = new BOMCase
                {
                    BOMSubmissionRecno = String.Empty,
                    AktivitetTypeKode = String.Empty,
                    OrgUnitRecno = String.Empty,
                    OurRefRecno = String.Empty,
                    ToCaseCategory = String.Empty,
                    ToProgressPlan = String.Empty,
                    ToCaseType = String.Empty,
                    AccessCode = String.Empty,
                    AccessGroup = String.Empty,
                    configuration = new BOMConfiguration
                    {
                        ActivityTypes = new List<BOMConfiguration.BOMActivityTypeConfigurationItem> { },
                    }

                };

                FuIndsendelseType indsendelseType = new FuIndsendelseType(new ServiceMaalStatistikType(), new IndsendelseType());


                Fujitsu.eDoc.BOM.Fakes.ShimBOMCaseHandler.GetBOMCaseDataBOMConfigurationFuIndsendelseType = (configuration, BOMApllication) => { return bOMCase; };
                Fujitsu.eDoc.BOM.Fakes.ShimBOMCaseHandler.HandlingHasExpiredBOMConfigurationBOMCase = (configuration, bomcase) => { return false; };
                Fujitsu.eDoc.BOM.Fakes.ShimBOMCaseHandler.FindExistingCaseBOMCase = (bomcase) => { return false; };
                Fujitsu.eDoc.BOM.Fakes.ShimBOMCaseHandler.IsOldBOMCaseBOMCase = (bomcase) => { return false; };
                Fujitsu.eDoc.BOM.Fakes.ShimBOMCaseHandler.BOMCaseExistsBOMCase = (bomcase) => { return false; };
                Fujitsu.eDoc.BOM.CaseHandler.Fakes.ShimBOMCase.AllInstances.CanCreateEDocCase = (a) => { return true; };
                Fujitsu.eDoc.BOM.Fakes.ShimBOMCaseHandler.GetEstateBOMCase = (bomcase) =>
                {
                    throw ex;

                };

                Fujitsu.eDoc.Core.Fakes.ShimCommon.GetResourceXmlStringStringAssembly = (xmlDocument, resourcePath, assembly) =>
                {
                    return string.Empty;
                };

                Fujitsu.eDoc.Core.Fakes.ShimCommon.SimpleEventLoggingStringStringStringEventLogEntryType = (source, slog, sEvent, eventType) =>
                {

                    source = typeof(BOMCaseHandler).FullName;
                    slog = "FuBOM";
                    sEvent = ex.ToString();
                    eventType = System.Diagnostics.EventLogEntryType.Error;

                    isLogged = true;

                };

                Fujitsu.eDoc.Core.Fakes.ShimCommon.ExecuteSingleActionString = (xmlQuery) =>
                {
                    return String.Empty;
                };


                //Act
                MethodInfo dynMethod = typeof(BOMCaseHandler).GetMethods().Where(x => x.Name == "HandleBOMApplication").Last();
                BOMCaseHandler bOMCaseHandler = new BOMCaseHandler();
                dynMethod.Invoke(bOMCaseHandler, new object[] { config, indsendelseType });

                Assert.IsTrue(isLogged);

            }
        }
    }
}
