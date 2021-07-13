using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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


    }
}
