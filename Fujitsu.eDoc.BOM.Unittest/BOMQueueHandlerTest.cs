using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Reflection;

namespace Fujitsu.eDoc.BOM.Unittest
{
    [TestClass]
    public class BOMQueueHandlerTest
    {
        /// <summary>
        /// This test is based on files that are temporarily were on my development machine which were meaningful to test on that specific time. 
        /// </summary>
        [TestMethod, TestCategory("OnBuild")]
        public void TestHandleFuBomQueues_ProcessedManualAndAutomaticBOMQueues_When_PendingMode()
        {
            using (ShimsContext.Create())
            {
                //Arrange
                int i = 0;
                Fujitsu.eDoc.Core.Fakes.ShimCommon.GetResourceXmlStringStringAssembly = (xmlDocument, resourcePath, assembly) =>
                {
                    i++;
                    if (i == 2 || i == 4)
                    {
                        return $@"<operation>
              <UPDATESTATEMENT NAMESPACE='SIRIUS' ENTITY='FuBOMCase' PRIMARYKEYVALUE='#RECNO#'>
                <METAITEM NAME='StatusCode'>
                  <VALUE></VALUE>
                </METAITEM>
                <METAITEM NAME='OtherAuthorityCode'>
                  <VALUE></VALUE>
                </METAITEM>
                <METAITEM NAME='PhaseCode'>
                  <VALUE></VALUE>
                </METAITEM>
                <METAITEM NAME='InitiativeDuty'>
                  <VALUE></VALUE>
                </METAITEM>
                <METAITEM NAME='DeadlineNotificationKode'>
                  <VALUE></VALUE>
                </METAITEM>
                <METAITEM NAME='Deadline'>
                  <VALUE></VALUE>
                </METAITEM>
                <METAITEM NAME='LastActivity'>
                  <VALUE></VALUE>
                </METAITEM>
              </UPDATESTATEMENT>
            </operation>";
                    }
                    else if (i == 3 || i == 6)
                    {
                        return $@"<operation>
              <UPDATESTATEMENT NAMESPACE='SIRIUS' ENTITY='FuBOMQueue'  PRIMARYKEYVALUE='#RECNO#'>
                <METAITEM NAME='MetaXML'>
                  <VALUE></VALUE>
                </METAITEM>
                <METAITEM NAME='Status'>
                  <VALUE></VALUE>
                </METAITEM>
                <METAITEM NAME='ErrorMsg'>
                  <VALUE></VALUE>
                </METAITEM>
              </UPDATESTATEMENT>
            </operation>";
                    }
                    return "";
                };

                //Test data
                Fujitsu.eDoc.Core.Fakes.ShimCommon.ExecuteQueryString = (xmlQuery) =>
                    {
                        return @"<RECORDS RECORDCOUNT='2'><RECORD COL_HEADER='0'><Recno>300593</Recno><MetaXML>&lt;?xml version='1.0' encoding='utf-16'?&gt;
&lt;BOMCaseUpdateType xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema'&gt;
  &lt;IsStamped&gt;0&lt;/IsStamped&gt;
  &lt;Ct_recno&gt;0&lt;/Ct_recno&gt;
  &lt;CaseRecno&gt;749093&lt;/CaseRecno&gt;
  &lt;ToBOMCase&gt;301715&lt;/ToBOMCase&gt;
  &lt;BOMCaseId&gt;58944cb9-07f5-48d2-b272-caacf109cf87&lt;/BOMCaseId&gt;
  &lt;Title&gt;Opdateret status&lt;/Title&gt;
  &lt;Date&gt;2021-04-12T11:19:01.7106323+02:00&lt;/Date&gt;
  &lt;Status&gt;
    &lt;SagStatusKode&gt;Bero&lt;/SagStatusKode&gt;
    &lt;InitiativPligtKode&gt;Myndighed&lt;/InitiativPligtKode&gt;
    &lt;FaseKode&gt;Bero&lt;/FaseKode&gt;
    &lt;FristNotifikationProfilKode&gt;Default&lt;/FristNotifikationProfilKode&gt;
    &lt;FristDato&gt;0001-01-01T00:00:00&lt;/FristDato&gt;
  &lt;/Status&gt;
  &lt;Changes&gt;
    &lt;ChangeLog&gt;
      &lt;LogdataName&gt;Status ændret&lt;/LogdataName&gt;
      &lt;LogdataFrom&gt;Bero&lt;/LogdataFrom&gt;
      &lt;LogdataTo&gt;Bero&lt;/LogdataTo&gt;
    &lt;/ChangeLog&gt;
    &lt;ChangeLog&gt;
      &lt;LogdataName&gt;Sagen afventer ændret&lt;/LogdataName&gt;
      &lt;LogdataFrom&gt;Myndighed&lt;/LogdataFrom&gt;
      &lt;LogdataTo&gt;Myndighed&lt;/LogdataTo&gt;
    &lt;/ChangeLog&gt;
    &lt;ChangeLog&gt;
      &lt;LogdataName&gt;Fase ændret&lt;/LogdataName&gt;
      &lt;LogdataFrom&gt;Bero&lt;/LogdataFrom&gt;
      &lt;LogdataTo&gt;Bero&lt;/LogdataTo&gt;
    &lt;/ChangeLog&gt;
  &lt;/Changes&gt;
  &lt;InitiativeDuty&gt;Viborg Kommune&lt;/InitiativeDuty&gt;
  &lt;OrgUnitRecno&gt;0&lt;/OrgUnitRecno&gt;
&lt;/BOMCaseUpdateType&gt;</MetaXML><Status>300001</Status><CreatedDate>2021-04-12 11:19:01</CreatedDate></RECORD><RECORD COL_HEADER='0'><Recno>300594</Recno><MetaXML>&lt;?xml version='1.0' encoding='utf-16'?&gt;
&lt;BOMCaseUpdateType xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema'&gt;
  &lt;IsStamped&gt;0&lt;/IsStamped&gt;
  &lt;Ct_recno&gt;386546&lt;/Ct_recno&gt;
  &lt;Initiator&gt;
    &lt;ContactRecno&gt;386546&lt;/ContactRecno&gt;
    &lt;Email&gt;denumk@edoc.int&lt;/Email&gt;
  &lt;/Initiator&gt;
  &lt;CaseRecno&gt;749093&lt;/CaseRecno&gt;
  &lt;ToBOMCase&gt;301715&lt;/ToBOMCase&gt;
  &lt;CaseType&gt;300004&lt;/CaseType&gt;
  &lt;CaseNumber&gt;21/000430&lt;/CaseNumber&gt;
  &lt;CaseTitle&gt;Garager, carporte, udhuse og lignende Asmild Vænge 6&lt;/CaseTitle&gt;
  &lt;BOMCaseId&gt;58944cb9-07f5-48d2-b272-caacf109cf87&lt;/BOMCaseId&gt;
  &lt;Title&gt;Manuel&lt;/Title&gt;
  &lt;Date&gt;2021-04-12T11:19:23.9992977+02:00&lt;/Date&gt;
  &lt;Status&gt;
    &lt;SagStatusKode&gt;Modtaget&lt;/SagStatusKode&gt;
    &lt;InitiativPligtKode&gt;Ansøger&lt;/InitiativPligtKode&gt;
    &lt;FaseKode&gt;Ansoeg&lt;/FaseKode&gt;
    &lt;FristNotifikationProfilKode&gt;Default&lt;/FristNotifikationProfilKode&gt;
    &lt;FristDato&gt;0001-01-01T00:00:00&lt;/FristDato&gt;
    &lt;StatusText /&gt;
    &lt;SagAndenMyndighedKode /&gt;
  &lt;/Status&gt;
  &lt;MainDocument&gt;
    &lt;DocumentIdentifier&gt;b7d1d033-3cf4-4d8e-8678-50044555d892&lt;/DocumentIdentifier&gt;
    &lt;Title&gt;Ansoegning&lt;/Title&gt;
    &lt;DocumentNumber&gt;21/000430-1&lt;/DocumentNumber&gt;
    &lt;FileRecno&gt;692481&lt;/FileRecno&gt;
    &lt;FileVersionRecno&gt;761046&lt;/FileVersionRecno&gt;
    &lt;DocumentRevisionRecno&gt;539604&lt;/DocumentRevisionRecno&gt;
    &lt;FileFullname&gt;\\DEV-SQL2016\eDocUsers_51Master\Upload\735ecd9e-4c3e-4eb5-bb60-a947c36f7783\0ab2b0ec-d496-4ba1-9198-675b68d04d11.PDF&lt;/FileFullname&gt;
    &lt;FileExtention&gt;PDF&lt;/FileExtention&gt;
    &lt;FileMimeType&gt;application/pdf&lt;/FileMimeType&gt;
  &lt;/MainDocument&gt;
  &lt;Changes&gt;
    &lt;ChangeLog&gt;
      &lt;LogdataName&gt;Status ændret&lt;/LogdataName&gt;
      &lt;LogdataFrom&gt;Bero - Sag i bero&lt;/LogdataFrom&gt;
      &lt;LogdataTo&gt;Ansøgningen er modtaget&lt;/LogdataTo&gt;
    &lt;/ChangeLog&gt;
    &lt;ChangeLog&gt;
      &lt;LogdataName&gt;Fase ændret&lt;/LogdataName&gt;
      &lt;LogdataFrom&gt;Bero - Sag i bero&lt;/LogdataFrom&gt;
      &lt;LogdataTo&gt;Ansøgning&lt;/LogdataTo&gt;
    &lt;/ChangeLog&gt;
    &lt;ChangeLog&gt;
      &lt;LogdataName&gt;Sagen afventer ændret&lt;/LogdataName&gt;
      &lt;LogdataFrom&gt;Myndighed&lt;/LogdataFrom&gt;
      &lt;LogdataTo&gt;Ansøger&lt;/LogdataTo&gt;
    &lt;/ChangeLog&gt;
  &lt;/Changes&gt;
  &lt;InitiativeDuty&gt;Viborg Kommune&lt;/InitiativeDuty&gt;
  &lt;OrgUnitRecno&gt;200057&lt;/OrgUnitRecno&gt;
&lt;/BOMCaseUpdateType&gt;</MetaXML><Status>300002</Status><CreatedDate>2021-04-12 11:19:24</CreatedDate></RECORD></RECORDS>";
                    };

                //Skip DB executions
                Fujitsu.eDoc.Core.Fakes.ShimCommon.ExecuteSingleActionString = (str) =>
                {
                    return string.Empty;
                };

                //Skip
                Fujitsu.eDoc.BOM.Fakes.ShimBOMQueueHandler.UpdateQueueItemStringStringBOMQueueStatusTypeString = (a, b, c, d) =>
                {

                };

                //Skip sending an email
                Fujitsu.eDoc.BOM.Fakes.ShimEmailHelper.SendFailedNotificationBOMCaseUpdateTypeString = (a, b) =>
                {

                };

                //Act
                System.Reflection.MethodInfo dynMethod = typeof(BOMQueueHandler).GetMethod("HandleFuBomQueues", BindingFlags.Static | BindingFlags.Public);
                dynMethod.Invoke(null, null);

            }
        }

        [TestMethod, TestCategory("OnBuild")]
        public void TestHandleFuBomQueues_AllFurtherQueuesInFaultedState_When_AtLeastOneIsFaulted()
        {
            using (ShimsContext.Create())
            {
                //bool CanBeSteppedin = true;

                //Arrange - 5 Queues initialized with diffrent statuses. FinishConverting|FinishConverting|Failed|Pending|Pending
                FuBomQueue queueOne = new FuBomQueue { Recno = "300593", MetaXML = "", CaseRecno = "749093", CreatedDate = "2021-04-12 12:46:12.370", BOMCaseUpdateType = Serialization.Deserialize<BOMCaseUpdateType>(@"<?xml version='1.0' encoding='utf-16'?>
<BOMCaseUpdateType xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
    <IsStamped>0</IsStamped>
    <Ct_recno>0</Ct_recno>
    <CaseRecno>749093</CaseRecno>
    <ToBOMCase>301715</ToBOMCase>
    <BOMCaseId>58944cb9-07f5-48d2-b272-caacf109cf87</BOMCaseId>
    <Title>Opdateret status</Title>
    <Date>2021-04-12T11:19:01.7106323+02:00</Date>
    <Status>
        <SagStatusKode>Bero</SagStatusKode>
        <InitiativPligtKode>Myndighed</InitiativPligtKode>
        <FaseKode>Bero</FaseKode>
        <FristNotifikationProfilKode>Default</FristNotifikationProfilKode>
        <FristDato>0001-01-01T00:00:00</FristDato>
    </Status>
    <DocumentationRequirements />
    <Attachments />
    <Changes>
        <ChangeLog>
            <LogdataName>Status ændret</LogdataName>
            <LogdataFrom>Bero</LogdataFrom>
            <LogdataTo>Bero</LogdataTo>
        </ChangeLog>
        <ChangeLog>
            <LogdataName>Sagen afventer ændret</LogdataName>
            <LogdataFrom>Myndighed</LogdataFrom>
            <LogdataTo>Myndighed</LogdataTo>
        </ChangeLog>
        <ChangeLog>
            <LogdataName>Fase ændret</LogdataName>
            <LogdataFrom>Bero</LogdataFrom>
            <LogdataTo>Bero</LogdataTo>
        </ChangeLog>
    </Changes>
    <InitiativeDuty>Viborg Kommune</InitiativeDuty>
    <OrgUnitRecno>0</OrgUnitRecno>
</BOMCaseUpdateType>"), BOMQueueStatusType = BOMQueueStatusType.FinishConverting };
                FuBomQueue queueTwo = new FuBomQueue { Recno = "300594", MetaXML = "", CaseRecno = "749093", CreatedDate = "2021-04-12 11:19:24.203", BOMCaseUpdateType = Serialization.Deserialize<BOMCaseUpdateType>(@"<?xml version='1.0' encoding='utf-16'?>
<BOMCaseUpdateType xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
    <IsStamped>0</IsStamped>
    <Ct_recno>386546</Ct_recno>
    <Initiator>
        <ContactRecno>386546</ContactRecno>
        <Email>denumk@edoc.int</Email>
    </Initiator>
    <CaseRecno>749093</CaseRecno>
    <ToBOMCase>301715</ToBOMCase>
    <CaseType>300004</CaseType>
    <CaseNumber>21/000430</CaseNumber>
    <CaseTitle>Garager, carporte, udhuse og lignende Asmild Vænge 6</CaseTitle>
    <BOMCaseId>58944cb9-07f5-48d2-b272-caacf109cf87</BOMCaseId>
    <Title>Manuelll 2</Title>
    <Date>2021-04-12T15:20:30.5158372+02:00</Date>
    <Status>
        <SagStatusKode>Modtaget</SagStatusKode>
        <InitiativPligtKode>Myndighed</InitiativPligtKode>
        <FaseKode>Ansoeg</FaseKode>
        <FristNotifikationProfilKode>Default</FristNotifikationProfilKode>
        <FristDato>0001-01-01T00:00:00</FristDato>
        <StatusText />
        <SagAndenMyndighedKode />
    </Status>
    <MainDocument>
        <DocumentIdentifier>5bdc572f-31fc-4c6b-adab-9ab626b3cd46</DocumentIdentifier>
        <Title>KonfliktRapport</Title>
        <DocumentNumber>21/000430-1</DocumentNumber>
        <FileRecno>692482</FileRecno>
        <FileVersionRecno>761047</FileVersionRecno>
        <DocumentRevisionRecno>539604</DocumentRevisionRecno>
        <FileFullname>\\DEV-SQL2016\eDocUsers_51Master\Upload\a2ed5986-87c6-47e6-8376-6ef5d7253a61\b7e0abc5-a435-40b3-985a-23d613af6429.PDF</FileFullname>
        <FileExtention>PDF</FileExtention>
        <FileMimeType>application/pdf</FileMimeType>
    </MainDocument>
    <Changes>
        <ChangeLog>
            <LogdataName>Status ændret</LogdataName>
            <LogdataFrom>Bero - Sag i bero</LogdataFrom>
            <LogdataTo>Ansøgningen er modtaget</LogdataTo>
        </ChangeLog>
        <ChangeLog>
            <LogdataName>Fase ændret</LogdataName>
            <LogdataFrom>Bero - Sag i bero</LogdataFrom>
            <LogdataTo>Ansøgning</LogdataTo>
        </ChangeLog>
    </Changes>
    <InitiativeDuty>Viborg Kommune</InitiativeDuty>
    <OrgUnitRecno>200057</OrgUnitRecno>
</BOMCaseUpdateType>"), BOMQueueStatusType = BOMQueueStatusType.FinishConverting };
                FuBomQueue queueThree = new FuBomQueue { Recno = "300595", MetaXML = "", CaseRecno = "749093", CreatedDate = "2021-04-12 15:20:34.383", BOMCaseUpdateType = Serialization.Deserialize<BOMCaseUpdateType>(@"<?xml version='1.0' encoding='utf-16'?>
<BOMCaseUpdateType xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
    <IsStamped>0</IsStamped>
    <Ct_recno>386546</Ct_recno>
    <Initiator>
        <ContactRecno>386546</ContactRecno>
        <Email>denumk@edoc.int</Email>
    </Initiator>
    <CaseRecno>749093</CaseRecno>
    <ToBOMCase>301715</ToBOMCase>
    <CaseType>300004</CaseType>
    <CaseNumber>21/000430</CaseNumber>
    <CaseTitle>Garager, carporte, udhuse og lignende Asmild Vænge 6</CaseTitle>
    <BOMCaseId>58944cb9-07f5-48d2-b272-caacf109cf87</BOMCaseId>
    <Title>Manuelll 2</Title>
    <Date>2021-04-12T15:20:30.5158372+02:00</Date>
    <Status>
        <SagStatusKode>Modtaget</SagStatusKode>
        <InitiativPligtKode>Myndighed</InitiativPligtKode>
        <FaseKode>Ansoeg</FaseKode>
        <FristNotifikationProfilKode>Default</FristNotifikationProfilKode>
        <FristDato>0001-01-01T00:00:00</FristDato>
        <StatusText />
        <SagAndenMyndighedKode />
    </Status>
    <MainDocument>
        <DocumentIdentifier>5bdc572f-31fc-4c6b-adab-9ab626b3cd46</DocumentIdentifier>
        <Title>KonfliktRapport</Title>
        <DocumentNumber>21/000430-1</DocumentNumber>
        <FileRecno>692482</FileRecno>
        <FileVersionRecno>761047</FileVersionRecno>
        <DocumentRevisionRecno>539604</DocumentRevisionRecno>
        <FileFullname>\\DEV-SQL2016\eDocUsers_51Master\Upload\a2ed5986-87c6-47e6-8376-6ef5d7253a61\b7e0abc5-a435-40b3-985a-23d613af6429.PDF</FileFullname>
        <FileExtention>PDF</FileExtention>
        <FileMimeType>application/pdf</FileMimeType>
    </MainDocument>
    <Changes>
        <ChangeLog>
            <LogdataName>Status ændret</LogdataName>
            <LogdataFrom>Bero - Sag i bero</LogdataFrom>
            <LogdataTo>Ansøgningen er modtaget</LogdataTo>
        </ChangeLog>
        <ChangeLog>
            <LogdataName>Fase ændret</LogdataName>
            <LogdataFrom>Bero - Sag i bero</LogdataFrom>
            <LogdataTo>Ansøgning</LogdataTo>
        </ChangeLog>
    </Changes>
    <InitiativeDuty>Viborg Kommune</InitiativeDuty>
    <OrgUnitRecno>200057</OrgUnitRecno>
</BOMCaseUpdateType>"), BOMQueueStatusType = BOMQueueStatusType.Failed };
                FuBomQueue queueFour = new FuBomQueue { Recno = "300596", MetaXML = "", CaseRecno = "749093", CreatedDate = "2021-04-12 15:21:34.383", BOMCaseUpdateType = Serialization.Deserialize<BOMCaseUpdateType>(@"<?xml version='1.0' encoding='utf-16'?>
<BOMCaseUpdateType xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
    <IsStamped>0</IsStamped>
    <Ct_recno>386546</Ct_recno>
    <Initiator>
        <ContactRecno>386546</ContactRecno>
        <Email>denumk@edoc.int</Email>
    </Initiator>
    <CaseRecno>749093</CaseRecno>
    <ToBOMCase>301715</ToBOMCase>
    <CaseType>300004</CaseType>
    <CaseNumber>21/000430</CaseNumber>
    <CaseTitle>Garager, carporte, udhuse og lignende Asmild Vænge 6</CaseTitle>
    <BOMCaseId>58944cb9-07f5-48d2-b272-caacf109cf87</BOMCaseId>
    <Title>Manuelll 2</Title>
    <Date>2021-04-12T15:20:30.5158372+02:00</Date>
    <Status>
        <SagStatusKode>Modtaget</SagStatusKode>
        <InitiativPligtKode>Myndighed</InitiativPligtKode>
        <FaseKode>Ansoeg</FaseKode>
        <FristNotifikationProfilKode>Default</FristNotifikationProfilKode>
        <FristDato>0001-01-01T00:00:00</FristDato>
        <StatusText />
        <SagAndenMyndighedKode />
    </Status>
    <MainDocument>
        <DocumentIdentifier>5bdc572f-31fc-4c6b-adab-9ab626b3cd46</DocumentIdentifier>
        <Title>KonfliktRapport</Title>
        <DocumentNumber>21/000430-1</DocumentNumber>
        <FileRecno>692482</FileRecno>
        <FileVersionRecno>761047</FileVersionRecno>
        <DocumentRevisionRecno>539604</DocumentRevisionRecno>
        <FileFullname>\\DEV-SQL2016\eDocUsers_51Master\Upload\a2ed5986-87c6-47e6-8376-6ef5d7253a61\b7e0abc5-a435-40b3-985a-23d613af6429.PDF</FileFullname>
        <FileExtention>PDF</FileExtention>
        <FileMimeType>application/pdf</FileMimeType>
    </MainDocument>
    <Changes>
        <ChangeLog>
            <LogdataName>Status ændret</LogdataName>
            <LogdataFrom>Bero - Sag i bero</LogdataFrom>
            <LogdataTo>Ansøgningen er modtaget</LogdataTo>
        </ChangeLog>
        <ChangeLog>
            <LogdataName>Fase ændret</LogdataName>
            <LogdataFrom>Bero - Sag i bero</LogdataFrom>
            <LogdataTo>Ansøgning</LogdataTo>
        </ChangeLog>
    </Changes>
    <InitiativeDuty>Viborg Kommune</InitiativeDuty>
    <OrgUnitRecno>200057</OrgUnitRecno>
</BOMCaseUpdateType>"), BOMQueueStatusType = BOMQueueStatusType.Pending };
                FuBomQueue queueFive = new FuBomQueue { Recno = "300597", MetaXML = "", CaseRecno = "749093", CreatedDate = "2021-04-12 15:22:34.383", BOMCaseUpdateType = Serialization.Deserialize<BOMCaseUpdateType>(@"<?xml version='1.0' encoding='utf-16'?>
<BOMCaseUpdateType xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
    <IsStamped>0</IsStamped>
    <Ct_recno>386546</Ct_recno>
    <Initiator>
        <ContactRecno>386546</ContactRecno>
        <Email>denumk@edoc.int</Email>
    </Initiator>
    <CaseRecno>749093</CaseRecno>
    <ToBOMCase>301715</ToBOMCase>
    <CaseType>300004</CaseType>
    <CaseNumber>21/000430</CaseNumber>
    <CaseTitle>Garager, carporte, udhuse og lignende Asmild Vænge 6</CaseTitle>
    <BOMCaseId>58944cb9-07f5-48d2-b272-caacf109cf87</BOMCaseId>
    <Title>Manuelll 2</Title>
    <Date>2021-04-12T15:20:30.5158372+02:00</Date>
    <Status>
        <SagStatusKode>Modtaget</SagStatusKode>
        <InitiativPligtKode>Myndighed</InitiativPligtKode>
        <FaseKode>Ansoeg</FaseKode>
        <FristNotifikationProfilKode>Default</FristNotifikationProfilKode>
        <FristDato>0001-01-01T00:00:00</FristDato>
        <StatusText />
        <SagAndenMyndighedKode />
    </Status>
    <MainDocument>
        <DocumentIdentifier>5bdc572f-31fc-4c6b-adab-9ab626b3cd46</DocumentIdentifier>
        <Title>KonfliktRapport</Title>
        <DocumentNumber>21/000430-1</DocumentNumber>
        <FileRecno>692482</FileRecno>
        <FileVersionRecno>761047</FileVersionRecno>
        <DocumentRevisionRecno>539604</DocumentRevisionRecno>
        <FileFullname>\\DEV-SQL2016\eDocUsers_51Master\Upload\a2ed5986-87c6-47e6-8376-6ef5d7253a61\b7e0abc5-a435-40b3-985a-23d613af6429.PDF</FileFullname>
        <FileExtention>PDF</FileExtention>
        <FileMimeType>application/pdf</FileMimeType>
    </MainDocument>
    <Changes>
        <ChangeLog>
            <LogdataName>Status ændret</LogdataName>
            <LogdataFrom>Bero - Sag i bero</LogdataFrom>
            <LogdataTo>Ansøgningen er modtaget</LogdataTo>
        </ChangeLog>
        <ChangeLog>
            <LogdataName>Fase ændret</LogdataName>
            <LogdataFrom>Bero - Sag i bero</LogdataFrom>
            <LogdataTo>Ansøgning</LogdataTo>
        </ChangeLog>
    </Changes>
    <InitiativeDuty>Viborg Kommune</InitiativeDuty>
    <OrgUnitRecno>200057</OrgUnitRecno>
</BOMCaseUpdateType>"), BOMQueueStatusType = BOMQueueStatusType.Pending };

                IList<FuBomQueue> fuBomQueueStatuses = new List<FuBomQueue>();
                fuBomQueueStatuses.Add(queueOne);
                fuBomQueueStatuses.Add(queueTwo);
                fuBomQueueStatuses.Add(queueThree);
                fuBomQueueStatuses.Add(queueFour);
                fuBomQueueStatuses.Add(queueFive);


                Fujitsu.eDoc.Core.Fakes.ShimCommon.ExecuteQueryString = (xmlQuery) =>
                {
                    return @"<RECORDS RECORDCOUNT='0'></RECORDS>";
                };

                Fujitsu.eDoc.BOM.Fakes.ShimBOMQueueHandler.GetFuBomQueueStatusParallelConcurrentBagOfFuBomQueue = (x) =>
                {
                    return fuBomQueueStatuses;
                };

                Fujitsu.eDoc.BOM.Fakes.ShimBOMQueueHandler.UpdateQueueItemStringStringBOMQueueStatusTypeString = (a, b, c, d) =>
                {

                };

                Fujitsu.eDoc.BOM.Fakes.ShimBOMCaseHandler.UpdateEdocCaseBOMCaseUpdateType = (z) => { };

                //Skip DB executions
                Fujitsu.eDoc.Core.Fakes.ShimCommon.ExecuteSingleActionString = (str) =>
                {
                    return string.Empty;
                };
                //Skip sending an email
                Fujitsu.eDoc.BOM.Fakes.ShimEmailHelper.SendFailedNotificationBOMCaseUpdateTypeString = (a, b) =>
                {

                };

                //Act
                System.Reflection.MethodInfo dynMethod = typeof(BOMQueueHandler).GetMethod("HandleFuBomQueues", BindingFlags.Static | BindingFlags.Public);
                dynMethod.Invoke(null, null);
            }
        }
    }
}