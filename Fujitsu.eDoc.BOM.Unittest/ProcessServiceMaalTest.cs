using Fujitsu.eDoc.BOM.BOMSagsbehandling;
using Fujitsu.eDoc.BOM.BOMSagsbehandling.Fakes;
using Fujitsu.eDoc.BOM.UpdateServiceMaal.ProcessEngine;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;

namespace Fujitsu.eDoc.BOM.Unittest
{
    [TestClass]
    public class ProcessServiceMaalTest
    {
        [TestMethod, TestCategory("OnBuild")]
        public void BOMCase_WithServiceGoal_ProcessSucceded()
        {
            using (ShimsContext.Create())
            {
                //Arrange
                bool isBOMCaseProccesed = false;

                Fujitsu.eDoc.Core.Fakes.ShimCommon.ExecuteQueryString = (xmlQuery) =>
                {
                    return $@"<?xml version='1.0'?>
                                            <RECORDS RECORDCOUNT='1'>
                                                <RECORD>
                                                    <ToBomCase.Recno>{0}</ToBomCase.Recno>
                                                    <ToBomCase.ApplicationId>{Guid.Empty}</ToBomCase.ApplicationId>
                                                    <ToBomCase.BomCaseId>{Guid.Empty}</ToBomCase.BomCaseId>
                                                </RECORD>
                                                </RECORDS>";
                };

                Fujitsu.eDoc.BOM.Fakes.ShimBOMCaller.GetSagsbehandlingServiceClient = () =>
                {
                    return new SagsbehandlingServiceClient(null);
                };

                //Constructor gets shimmed
                Fujitsu.eDoc.BOM.BOMSagsbehandling.Fakes.ShimSagsbehandlingServiceClient.ConstructorString = (@this, value) =>
                {
                    ShimSagsbehandlingServiceClient shim = new ShimSagsbehandlingServiceClient(@this)
                    {
                        LaesServiceMaalStatistikAnsoegningIdArray = (ansoegningIDs) => new ServiceMaalStatistikkerType
                        {
                            ServiceMaalStatistik = new ServiceMaalStatistikType1[] {
                            new ServiceMaalStatistikType1 { Statistik = new ServiceMaalStatistikTypeStatistik1{
                                SagsbehandlingForbrugtDage = 40, SagsbehandlingForbrugtDageSpecified = true, VisitationForbrugtDage = 21,
                                    VisitationForbrugtDageSpecified = true},
                                        BOMSagID = Guid.Empty.ToString(),
                                            Fritagelsesbegrundelse = new ServiceMaalStatistikTypeFritagelsesbegrundelse1{Kode = "BBR"},
                                            ServiceMaal = new ServiceMaalStatistikTypeServiceMaal1{ Dage = "20"} } }
                        }
                    };
                };

                Fujitsu.eDoc.Core.Fakes.ShimCommon.ExecuteSingleActionString = (xmlQuery) =>
                {
                    return string.Empty;
                };

                Fujitsu.eDoc.BOM.UpdateServiceMaal.ProcessEngine.Fakes.ShimProcessServiceMaal.AllInstances.LogProcessingCases = (list) =>
                {
                    if (list.ProcessedServiceGoals.Count == 1)
                    {
                        isBOMCaseProccesed = true;
                    }
                };


                //Act
                System.Reflection.MethodInfo dynMethod = typeof(ProcessServiceMaal).GetMethod("BatchServiceMaals", BindingFlags.Instance | BindingFlags.Public);
                ProcessServiceMaal serviceMaal = new ProcessServiceMaal();
                dynMethod.Invoke(serviceMaal, null);


                //Assertion
                Assert.IsTrue(isBOMCaseProccesed);
            }
        }




        [TestMethod, TestCategory("OnBuild")]
        public void BOMCase_WithoutServiceGoal_ProcessSucceded()
        {
            using (ShimsContext.Create())
            {
                //Arrange
                string status = "FAILED";

                Fujitsu.eDoc.Core.Fakes.ShimCommon.ExecuteQueryString = (xmlQuery) =>
                {
                    return $@"<?xml version='1.0'?>
                                            <RECORDS RECORDCOUNT='1'>
                                                <RECORD>
                                                    <ToBomCase.Recno>{0}</ToBomCase.Recno>
                                                    <ToBomCase.ApplicationId>{Guid.Empty}</ToBomCase.ApplicationId>
                                                    <ToBomCase.BomCaseId>{Guid.Empty}</ToBomCase.BomCaseId>
                                                </RECORD>
                                                </RECORDS>";
                };

                Fujitsu.eDoc.BOM.Fakes.ShimBOMCaller.GetSagsbehandlingServiceClient = () =>
                {
                    return new SagsbehandlingServiceClient(null);
                };

                //Constructor gets shimmed
                Fujitsu.eDoc.BOM.BOMSagsbehandling.Fakes.ShimSagsbehandlingServiceClient.ConstructorString = (@this, value) =>
                {
                    ShimSagsbehandlingServiceClient shim = new ShimSagsbehandlingServiceClient(@this)
                    {
                        LaesServiceMaalStatistikAnsoegningIdArray = (ansoegningIDs) => new ServiceMaalStatistikkerType
                        {
                            ServiceMaalStatistik = new ServiceMaalStatistikType1[] {
                            new ServiceMaalStatistikType1 { Statistik = new ServiceMaalStatistikTypeStatistik1{
                                SagsbehandlingForbrugtDage = 200, SagsbehandlingForbrugtDageSpecified = false, VisitationForbrugtDage = 300,
                                    VisitationForbrugtDageSpecified = true},
                                        BOMSagID = Guid.Empty.ToString(),
                                            Fritagelsesbegrundelse = new ServiceMaalStatistikTypeFritagelsesbegrundelse1{Kode = ""},
                                            ServiceMaal = new ServiceMaalStatistikTypeServiceMaal1{ Dage = ""} } }
                        }
                    };
                };

                Core.Fakes.ShimCommon.ExecuteSingleActionString = (xmlQuery) =>
                 {
                     if (xmlQuery.Contains(ServiceGoalStatus.Excluded))
                     {
                         status = "SUCCESS";
                     }
                     return string.Empty;
                 };

                Fujitsu.eDoc.BOM.UpdateServiceMaal.ProcessEngine.Fakes.ShimProcessServiceMaal.AllInstances.LogProcessingCases = (processedServiceGoals) =>
                {
                    //Skipping
                };

                //Act
                System.Reflection.MethodInfo dynMethod = typeof(ProcessServiceMaal).GetMethod("BatchServiceMaals", BindingFlags.Instance | BindingFlags.Public);
                ProcessServiceMaal serviceMaal = new ProcessServiceMaal();
                dynMethod.Invoke(serviceMaal, null);

                //Assertion
                Assert.AreEqual("SUCCESS", status);
            }
        }
    }
}
