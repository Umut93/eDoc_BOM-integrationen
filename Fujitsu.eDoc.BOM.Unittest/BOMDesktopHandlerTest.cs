using Fujitsu.eDoc.BOM.BOMSagsbehandling;
using Fujitsu.eDoc.BOMApplicationDesktopApp;
using Fujitsu.eDoc.BOMApplicationDesktopApp.Handler;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Fujitsu.eDoc.BOM.Unittest
{
    [TestClass]
    public class BOMDesktopHandlerTest
    {
        /// <summary>
        /// Create a BOM Case in eDoc from scratch when submission number 3 is only persisted in DB without being attached to anything
        /// </summary>
        [TestMethod, TestCategory("OnBuild")]
        public void Testbutton1_Click_ProcessSubmissions_When_MissingIneDoc()
        {
            using (ShimsContext.Create())
            {

                //Arrange
                int counter = 0;
                List<FUBOMSubmission> previousSubmissions = new List<FUBOMSubmission>();
                FUBOMSubmission submissionOne = new FUBOMSubmission(null, DateTime.Parse("2021-04-20 16:23:04.000"), 1, Guid.Parse("400a9cd2-fc8a-4a1a-96ec-26d6d96b833f"), Guid.Parse("5d9552ce-9aca-45b1-96ca-695018b5b71b"), "", BOMUtils.BOMCaseTransferStatusEnum.Processing, DateTime.Now, "");
                FUBOMSubmission submissionTwo = new FUBOMSubmission(null, DateTime.Parse("2021-04-21 20:17:57.000"), 2, Guid.Parse("4991215f-ecc8-4e13-a748-fbc7f91e04c4"), Guid.Parse("5d9552ce-9aca-45b1-96ca-695018b5b71b"), "", BOMUtils.BOMCaseTransferStatusEnum.Processing, DateTime.Now, "");

                previousSubmissions.Add(submissionOne);
                previousSubmissions.Add(submissionTwo);
                var orderedPreviousSubmissions = previousSubmissions.OrderBy(sub => sub.submissionNummer).ToList();

                ListViewItem listViewItem = new ListViewItem();
                ListView listView1 = new ListView();
                listView1.CheckBoxes = true;

                foreach (var item in orderedPreviousSubmissions)
                {
                    listViewItem = new ListViewItem(item.submissionNummer.ToString());
                    listViewItem.Tag = item;
                    listViewItem.Checked = true;
                    listViewItem.SubItems.Add(item.applicationId.ToString());
                    listViewItem.SubItems.Add(item.caseId.ToString());
                    listViewItem.SubItems.Add(item.submissionTime.ToString());
                    listView1.Items.Add(listViewItem);
                }

                ListView.CheckedListViewItemCollection checkedItems = new ListView.CheckedListViewItemCollection(listView1);
                System.Windows.Forms.Fakes.ShimListView.AllInstances.CheckedItemsGet = (z) => { return checkedItems; };
                Fujitsu.eDoc.BOMApplicationDesktopApp.Fakes.ShimForm1.AllInstances.IsAnyUnChecked = (b) => { return false; };
                Fujitsu.eDoc.BOMApplicationDesktopApp.Fakes.ShimForm1.AllInstances.GetApplicationFUBOMSubmission = (a, b) =>
                {
                    counter++; if (counter == 1)
                    {
                        return new BOM.BOMSagsbehandling.IndsendelseType
                        {
                            IndsendelseID = submissionOne.applicationId.ToString(),
                            BOMSag = new BOMSagsbehandling.BOMSagType { BOMSagID = submissionOne.caseId.ToString() },

                        };
                    }
                    else
                    {
                        return new BOMSagsbehandling.IndsendelseType
                        {

                            IndsendelseID = submissionTwo.applicationId.ToString(),
                            BOMSag = new BOMSagsbehandling.BOMSagType { BOMSagID = submissionTwo.caseId.ToString() },
                        };
                    }
                };

                var first = string.Empty;
                Fujitsu.eDoc.BOMApplicationDesktopApp.Handler.Fakes.ShimBOMDesktopHandler.AllInstances.HandleVeryFirstBOMApplicationIndsendelseTypeStringOut = (BOMDesktopHandler a, IndsendelseType b, out string output) => { output = "FirstCompleted"; first = output; return; };
                var second = string.Empty;
                Fujitsu.eDoc.BOMApplicationDesktopApp.Handler.Fakes.ShimBOMDesktopHandler.AllInstances.HandleFurtherSubmissionsIndsendelseTypeString = (a, b, c) => { second = "SecondCompleted"; return; };
                Fujitsu.eDoc.BOMApplicationDesktopApp.Fakes.ShimForm1.SetFUBOMRecnoOnAllUnAttachedListOfFUBOMSubmissionString = (a, b) => { listView1.Clear(); };
                System.Windows.Forms.Fakes.ShimListView.ShimListViewItemCollection.AllInstances.Clear = (a) => { };
                System.Windows.Forms.Fakes.ShimMessageBox.ShowString = (msg) => { return new DialogResult { }; };


                //Invoke method
                Form1 f = new Form1();
                System.Reflection.MethodInfo dynMethod = typeof(Form1).GetMethod("CreateBOMCase_Click", BindingFlags.Public | BindingFlags.Instance);
                dynMethod.Invoke(f, new object[] { new object { }, new System.EventArgs { } });


                //Assertion
                Assert.IsTrue(first == "FirstCompleted");
                Assert.IsTrue(second == "SecondCompleted");

            }
        }

    }
}

