using System;
using System.IO;
using System.Xml.Serialization;

namespace Fujitsu.eDoc.BOM.ApplicationTransfer
{
    class Program
    {
        static void Main(string[] args)
        {
            // First update datalists
            try
            {
                if (CheckSetting("UpdateDataLists"))
                {
                    if (BOMConfigHandler.ShouldUpdateDataLists())
                    {
                        BOMConfigHandler.UpdateDataLists();
                    }
                }
            }
            catch (Exception ex)
            {
                Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.ApplicationTransfer", "FuBOM", "Code lists from BOM can not be updated:\n" + ex.ToString(), System.Diagnostics.EventLogEntryType.Error);
            }


            // Update new BOM messages
            try
            {
                if (CheckSetting("HandleMessages"))
                {
                    BOMMessageHandler.HandleNewMessages();
                }
            }
            catch (Exception ex)
            {
                Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.ApplicationTransfer", "FuBOM", "Error handling messages:\n" + ex.ToString(), System.Diagnostics.EventLogEntryType.Error);
            }


            // Update BOM with replyes
            try
            {
                if (CheckSetting("HandleQueue"))
                {
                    BOMQueueHandler.HandleFuBomQueues();
                }
            }
            catch (Exception ex)
            {
                Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.ApplicationTransfer", "FuBOM", "Error handling queued replies:\n" + ex.ToString(), System.Diagnostics.EventLogEntryType.Error);
            }

            // Get new applications
            try
            {
                if (CheckSetting("HandleNewApplications"))
                {
                    HandleNewApplications();
                }
            }
            catch (Exception ex)
            {
                Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.ApplicationTransfer", "FuBOM", "Error getting new applications:\n" + ex.ToString(), System.Diagnostics.EventLogEntryType.Error);
            }

        }

        private static void HandleNewApplications()
        {
            // Handle interrupted
            string[] interrupted = BOMCaseHandler.BOMSubmisstionInterrupted();
            for (int i = 0; i < interrupted.Length; i++)
            {
                string ansoegningid = interrupted[i];
                try
                {
                    FuIndsendelseType indsendelse = BOMCaller.GetApplication(ansoegningid);
                    BOMCaseHandler.HandleBOMApplication(indsendelse);
                }
                catch (Exception ex)
                {
                    Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.ApplicationTransfer", "FuBOM", string.Format("Error handling applications with id={0}:\n{1}", ansoegningid, ex.ToString()), System.Diagnostics.EventLogEntryType.Error);
                }
            }


            // Handle new from BOM
            BOMConfiguration cfg = BOMConfigHandler.GetBOMConfiguration();
            DateTime d = BOMCaseHandler.GetLatestBOMSubmisstionTime();

            if (d == DateTime.MinValue)
            {
                d = cfg.GetStartDateTime();
                if (d == DateTime.MinValue)
                {
                    if (!DateTime.TryParse(System.Configuration.ConfigurationManager.AppSettings["StartApplicationDate"], out d))
                    {
                        d = DateTime.Today;
                    }
                }
            }

            string[] SagOmraader = cfg.GetEnabledCaseAreaNames();

            string[] result = BOMCaller.GetApplicationOverview(d, SagOmraader);
            // array has new applications first, so loop in reverse order to transfger olderst first
            for (int i = result.Length - 1; i >= 0; i--)
            {
                string ansoegningid = "";
                try
                {
                    bool handled = BOMCaseHandler.BOMSubmisstionHandled(result[i]);
                    if (!handled)
                    {
                        ansoegningid = result[i];
                        FuIndsendelseType Fuindsendelse = BOMCaller.GetApplication(ansoegningid);

                        if (ShouldfHandleSubmission(Fuindsendelse.IndsendelseType))
                        {
                            LogApplication(Fuindsendelse.IndsendelseType);

                            BOMCaseHandler.HandleBOMApplication(Fuindsendelse);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Fujitsu.eDoc.Core.Common.SimpleEventLogging("Fujitsu.eDoc.BOM.ApplicationTransfer", "FuBOM", string.Format("Error handling applications with id={0}:\n{1}", ansoegningid, ex.ToString()), System.Diagnostics.EventLogEntryType.Error);
                }
            }
        }

        private static bool CheckSetting(string Key)
        {
            string val = System.Configuration.ConfigurationManager.AppSettings[Key];
            if (!string.IsNullOrEmpty(val))
            {
                if (val.ToLower() == "false")
                {
                    return false;
                }
            }
            return true;
        }

        private static string SerializeObject(BOM.BOMSagsbehandling.IndsendelseType toSerialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(toSerialize.GetType());

            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);
                return textWriter.ToString();
            }
        }


        private static void LogApplication(BOM.BOMSagsbehandling.IndsendelseType indsendelse)
        {
            try
            {
                if (CheckSetting("LogApplication"))
                {
                    string LogApplicationPath = System.Configuration.ConfigurationManager.AppSettings["LogApplicationPath"];
                    if (!LogApplicationPath.EndsWith(@"\"))
                    {
                        LogApplicationPath += @"\";
                    }
                    Directory.CreateDirectory(LogApplicationPath);

                    string text = SerializeObject(indsendelse);

                    string fileName = LogApplicationPath + indsendelse.IndsendelseID + ".xml";

                    StreamWriter sw = File.CreateText(fileName);
                    sw.Write(text);
                    sw.Close();
                }
            }
            catch { }
        }

        private static bool ShouldfHandleSubmission(BOM.BOMSagsbehandling.IndsendelseType indsendelse)
        {
            string IndsenderNavn = System.Configuration.ConfigurationManager.AppSettings["TestIndsenderNavn"];
            if (!string.IsNullOrEmpty(IndsenderNavn))
            {
                if (indsendelse.Indsender != null && indsendelse.Indsender.NavnTekst.StartsWith(IndsenderNavn))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
    }
}
