using Fujitsu.eDoc.Core;
using Fujitsu.eDoc.Integrations.Datafordeler;
using Fujitsu.eDoc.Integrations.Datafordeler.Ejerfortegnelsen;
using Fujitsu.ExternRegister;
using System;
using System.Linq;

namespace Fujitsu.eDoc.BOM.Integrations
{
    internal class EjerfortegnelsenHandler
    {
        internal static void AddOwners(int bfeNumber, string caseRecno)
        {
            try
            {
                FUEjerfortegnelsen ef = new FUEjerfortegnelsen();
                string result = "";
                Ejerfortegnelse ejerfortegnelse = ef.GetEjerfortegnelse(bfeNumber.ToString(),
                                                                        FUEjerfortegnelsen.EjerfortegnelseType.EjerfortegnelseFortroligBeskyttet,
                                                                        FUEjerfortegnelsenUtils.EjerfortegnelseMethod.Ejere, ref result);

                foreach (Feature feature in ejerfortegnelse.features)
                {
                    if (feature.properties.ejendePerson != null)
                    {
                        Person person = feature.properties.ejendePerson.Person;
                        if (person != null)
                        {
                            // Det er besluttet at uanset primær kontakt eller ej, så er alle bare ejere
                            string contactCaseRoleRecno = string.Empty;
                            if (feature.properties.primaerKontakt)
                            {
                                //contactCaseRoleRecno = Fujitsu.ExternRegister.CitizenManager.GetContactCaseRoleRecno("Hovedejer");
                                contactCaseRoleRecno = Fujitsu.ExternRegister.CitizenManager.GetContactCaseRoleRecno("Ejer");
                            }
                            else
                            {
                                contactCaseRoleRecno = Fujitsu.ExternRegister.CitizenManager.GetContactCaseRoleRecno("Ejer");
                                //contactCaseRoleRecno = Fujitsu.ExternRegister.CitizenManager.GetContactCaseRoleRecno("Medejer");
                            }
                            AddPrivateOwner(person, caseRecno, contactCaseRoleRecno);
                        }
                    }
                    else if (feature.properties.ejendeVirksomhed != null)
                    {
                        var att = feature.properties.ejendeVirksomhed?.attributes;
                        if (att != null)
                        {
                            string contactCaseRoleRecno = BaseRegister.GetContactCaseRoleRecno("Ejer");
                            AddCompanyOwner(att, caseRecno, contactCaseRoleRecno);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.SimpleEventLogging("Fujitsu.eDoc.BOM.Integrations.EjerfortegnelsenHandler", "FuBOM",
                   $"Error adding owners: {ex.ToString()}", System.Diagnostics.EventLogEntryType.Error);
            }
        }

        internal static void AddAdministrators(int bfeNumber, string caseRecno)
        {
            try
            {
                FUEjerfortegnelsen ef = new FUEjerfortegnelsen();
                string result = "";
                Ejerfortegnelse ejerfortegnelse = ef.GetEjerfortegnelse(bfeNumber.ToString(),
                                                                        FUEjerfortegnelsen.EjerfortegnelseType.EjerfortegnelseFortroligBeskyttet,
                                                                        FUEjerfortegnelsenUtils.EjerfortegnelseMethod.Ejendomsadministrator, ref result);

                foreach (Feature feature in ejerfortegnelse.features)
                {
                    if (feature.properties.ejendePerson != null)
                    {
                        Person person = feature.properties.ejendePerson.Person;
                        if (person != null)
                        {
                            string contactCaseRoleRecno = BaseRegister.GetContactCaseRoleRecno("Ejendomsadministrator");
                            AddPrivateOwner(person, caseRecno, contactCaseRoleRecno);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.SimpleEventLogging("Fujitsu.eDoc.BOM.Integrations.EjerfortegnelsenHandler", "FuBOM",
                   $"Error adding Ejendomsadministrator: {ex.ToString()}", System.Diagnostics.EventLogEntryType.Error);
            }
        }

        private static void AddPrivateOwner(Person person, string caseRecno, string contactCaseRoleRecno)
        {
            try
            {
                var cprnumber = person.Personnumre.Where(p => p.Personnummer.status == "aktuel").FirstOrDefault();
                if (cprnumber != null && cprnumber.Personnummer != null)
                {
                    string contactRecno = CitizenManager.GetOrCreateCitizenFromExtern(cprnumber.Personnummer.personnummer, false);
                    if (string.IsNullOrEmpty(contactRecno) == false)
                    {
                        if (CitizenManager.IsContactOnCase(contactRecno, caseRecno, contactCaseRoleRecno) == false)
                        {
                            CitizenManager.AddCaseParticipant(contactRecno, caseRecno, person.Navn.adresseringsnavn, contactCaseRoleRecno);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.SimpleEventLogging("Fujitsu.eDoc.BOM.Integrations.EjerfortegnelsenHandler", "FuBOM",
                   $"Error adding owner: {ex.ToString()}", System.Diagnostics.EventLogEntryType.Error);
            }
        }

        private static void AddCompanyOwner(Attributes compnayAttributes, string caseRecno, string contactCaseRoleRecno)
        {
            try
            {
                var cvrnumber = compnayAttributes.CVRNummer;
                if (cvrnumber > 0)
                {
                    //string contactRecno = GetOrCreateInternalCorporate(compnayAttributes);
                    //if (string.IsNullOrEmpty(contactRecno) == false)
                    //{
                    //    if (CitizenManager.IsContactOnCase(contactRecno, caseRecno, contactCaseRoleRecno) == false)
                    //    {
                    //        CitizenManager.AddCaseParticipant(contactRecno, caseRecno, compnayAttributes.navn, contactCaseRoleRecno);
                    //    }
                    //}
                }

            }
            catch (Exception ex)
            {
                Common.SimpleEventLogging("Fujitsu.eDoc.BOM.Integrations.EjerfortegnelsenHandler", "FuBOM",
                   $"Error adding owner: {ex.ToString()}", System.Diagnostics.EventLogEntryType.Error);
            }
        }


    }
}