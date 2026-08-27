// The SCOM SDK Binaries are installed when you install the SCOM Console.
// Located at:  C:\Program Files\System Center Operations Manager 2012\Console\SDK Binaries
using Microsoft.EnterpriseManagement;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;  // Required to get access to the ManagementPack class etc
using Microsoft.EnterpriseManagement.Configuration.IO;
using Microsoft.EnterpriseManagement.Monitoring;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;  // Powershell - C:\windows\assembly\GAC_MSIL\System.Management.Automation\1.0.0.0__31bf3856ad364e35\System.Management.Automation.dll
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using VaderConsulting.Helper;

namespace VaderConsulting.SystemCenter
{
    public class ServerUtilities
    {
        #region Constructors

        public ServerUtilities(string SCOMServer, string SCSMServer)
        {
            try
            {
                if (SCOMServer != null && SCOMServer.Length > 0)
                {
                    _SCOMServerConnection = new ManagementGroup(SCOMServer);
                }
                else
                {
                    Debug.WriteLine("[ERR1018] SCOM Server is not specified", "warning");
                }
            }
            catch
            {
                Debug.WriteLine("[ERR1019] Could not create connection to SCOM Server", "error");
            }

            try
            {
                if (SCSMServer != null && SCSMServer.Length > 0)
                {
                    _SCSMServerConnection = new ManagementGroup(SCSMServer);
                }
                else
                {
                    Debug.WriteLine("[ERR1020] SCSM Server is not specified", "warning");
                }
            }
            catch
            {
                Debug.WriteLine("[ERR1021] Could not create connection to SCSM Server", "error");
            }
        }

        #endregion

        #region Private Types

        //private string _SealedFilename = "";
        private ManagementPackFileStore _MPStore = new ManagementPackFileStore();
        private ManagementGroup _SCOMServerConnection;
        private ManagementGroup _SCSMServerConnection;
        //private string _ServiceComponentGroupRelationshipName;
        //private string _SystemComputerRelationshipName;
        private ManagementPack _TargetManagementPack;

        #endregion

        #region Private Methods

        /// <summary>
        /// Create Management pack from one of 4 MP Templates
        /// </summary>
        /// <param name="NewVersion">The new Version number</param>
        /// <param name="ComponentServerNames">The List of Component Servers</param>
        /// <param name="ComponentServiceNames">The List of Component Services</param>
        /// <param name="CreationOptions">Options when creating the MP</param>
        /// <param name="ErrorMessage">The resultant error message ("" if ok)</param>
        /// <param name="OutputXML">The resultant XML</param>
        /// <returns>Boolean indicating success (true) or failure (false)</returns>
        private bool CreateManagementPackXML(Version NewVersion, List<string> ComponentServerNames, List<string> ComponentServiceNames, ManagementPackCreationOptions CreationOptions, out string ErrorMessage, out string OutputXML)
        {
            bool Result = false;
            int LoopCounter = 0;

            ErrorMessage = "";

            switch (ComponentServerNames.Count)
            {
                case 0:
                    switch (ComponentServiceNames.Count)
                    {
                        case 0:
                            if (VaderConsulting.Helper.Properties.ShowDebugInformation)
                            {
                                Debug.WriteLine(".........[INF1144] Empty MP Template", "information");
                            }
                            OutputXML = CreationOptions.ManagementPackEmptyTemplate;
                            break;
                        default:
                            if (VaderConsulting.Helper.Properties.ShowDebugInformation)
                            {
                                Debug.WriteLine(".........[INF1145] Business Applications Template", "information");
                            }
                            OutputXML = CreationOptions.ManagementPackServicesTemplate;
                            break;
                    }
                    break;
                default:
                    switch (ComponentServiceNames.Count)
                    {
                        case 0:
                            if (VaderConsulting.Helper.Properties.ShowDebugInformation)
                            {
                                Debug.WriteLine(".........[INF1146] Servers Template", "information");
                            }
                            OutputXML = CreationOptions.ManagementPackServersTemplate;
                            break;
                        default:
                            if (VaderConsulting.Helper.Properties.ShowDebugInformation)
                            {
                                Debug.WriteLine(".........[INF1147] Servers and Business Applications Template", "information");
                            }
                            OutputXML = CreationOptions.ManagementPackServersAndServicesTemplate;
                            break;
                    }
                    break;
            }

            StringBuilder Services = new StringBuilder();

            foreach (string ServiceName in ComponentServiceNames)
            {
                LoopCounter++;

                // Search
                string ServiceID = GetServiceIDFromName(ServiceName);
                if (ServiceID != "")
                {
                    Services.Append("<MonitoringObjectId>{" + ServiceID + "}</MonitoringObjectId>\r\n");
                }
                else
                {
                    ErrorMessage = "[ERR1148] Could not find Component Service (" + ServiceName + ") in SCOM Database.  The Management Pack for " + CreationOptions.ServiceInfo.ServiceName + " cannot be created";
                    OutputXML = "";
                    return false;
                }
            }

            StringBuilder ServiceIncludeList = new StringBuilder();
            ServiceIncludeList.Append("<IncludeList>\r\n");
            ServiceIncludeList.Append(Services.ToString());
            ServiceIncludeList.Append("</IncludeList>\r\n");

            StringBuilder Servers = new StringBuilder();

            foreach (string ServerName in ComponentServerNames)
            {
                LoopCounter++;

                // Search
                string ServerID = GetServerIDFromName(ServerName);
                if (ServerID != "")
                {
                    Servers.Append("<MonitoringObjectId>{" + ServerID + "}</MonitoringObjectId>\r\n");
                }
                else
                {
                    ErrorMessage = "[ERR1149] Could not find Component Server (" + ServerName + ") in SCOM Database.  The Management Pack for " + CreationOptions.ServiceInfo.ServiceName + " cannot be created";
                    OutputXML = "";
                    return false;
                }
            }

            StringBuilder ServerIncludeList = new StringBuilder();
            ServerIncludeList.Append("<IncludeList>\r\n");
            ServerIncludeList.Append(Servers.ToString());
            ServerIncludeList.Append("</IncludeList>\r\n");


            // Start replacing the tags...
            OutputXML = OutputXML.Replace("#MANAGEMENTPACK_ID#", CreationOptions.ManagementPackID);
            OutputXML = OutputXML.Replace("#MANAGEMENTPACK_VERSION#", CreationOptions.ManagementPackVersion.ToString());
            OutputXML = OutputXML.Replace("#MANAGEMENTPACK_NAME#", CreationOptions.ManagementPackName);
            OutputXML = OutputXML.Replace("#MANAGEMENTPACK_SHORTNAME#", CreationOptions.ServiceInfo.ServiceName.Replace(" ", "")
                                                                                                               .Replace("(", "")
                                                                                                               .Replace(")", "")
                                                                                                               .Replace(@"/", "_")
                                                                                                               .Replace(@"\", "_")
                                                                                                               .Replace(@"-", "_"));

            OutputXML = OutputXML.Replace("#GUID1#", Guid.NewGuid().ToString());
            OutputXML = OutputXML.Replace("#GUID2#", Guid.NewGuid().ToString());
            OutputXML = OutputXML.Replace("#GUID3#", Guid.NewGuid().ToString());
            OutputXML = OutputXML.Replace("#SERVICE_NAME#", CreationOptions.ServiceInfo.ServiceName);

            OutputXML = OutputXML.Replace("#SERVER_INCLUDELIST#", ServerIncludeList.ToString());
            OutputXML = OutputXML.Replace("#SERVICE_INCLUDELIST#", ServiceIncludeList.ToString());
            OutputXML = OutputXML.Replace("#MANAGEMENTPACK_FOLDERNAME#", CreationOptions.ManagementPackFolderName);

            System.IO.File.WriteAllText(CreationOptions.ManagementPackFilename, OutputXML);

            try
            {
                _TargetManagementPack = new ManagementPack(CreationOptions.ManagementPackFilename);

                Result = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine("[ERR1022] An error occurred whilst creating the Management Pack.  The exception was: " + e.ToString(), "error");
            }

            return Result;
        }

        private ManagementPackCriteria GetCriteria(string Name)
        {
            string Query = "Name Like '%" + Name + "'";

            return new ManagementPackCriteria(Query);
        }

        private bool ImportManagementPack(ManagementPackCreationOptions CreationOptions)
        {
            bool SaveResult = false;
            bool ImportResult = false;

            try
            {
                string ManagementPackFilename = CreationOptions.ManagementPackFilename.Replace("#SERVICENAME#", CreationOptions.ServiceInfo.ServiceName.Replace(" ", "_").Replace("(", "").Replace(")", ""));

                _TargetManagementPack = new ManagementPack(ManagementPackFilename);

                Debug.WriteLine("......[INF1136] Saving...", "information");
                SaveResult = SaveManagementPack(ManagementPackFilename);
                Debug.WriteLine(".........[INF1137] Saved", "information");

                if ((CreationOptions.ImportSCOMManagementPacks || CreationOptions.ImportSCSMManagementPacks) && SaveResult)
                {
                    Debug.WriteLine("......[INF1138] Importing...", "information");
                    _TargetManagementPack = new ManagementPack(ManagementPackFilename);

                    try
                    {
                        CreationOptions.TargetServer.ManagementPacks.ImportManagementPack(_TargetManagementPack);
                        Debug.WriteLine(".........[INF1139] Imported", "information");

                        ImportResult = true;
                    }
                    catch (UnauthorizedAccessEnterpriseManagementException)
                    {
                        Debug.WriteLine(".........[ERR1140] Access denied", "error");
                        SaveResult = false;
                        ImportResult = false;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(".........[ERR1141] Unexpected error: " + e.ToString(), "error");
                        SaveResult = false;
                        ImportResult = false;
                    }
                    finally
                    {
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("......[ERR1142] Could not save Management Pack: " + e.ToString(), "error");
            }

            return SaveResult || ImportResult;
        }

        private bool SaveManagementPack(string Path)
        {
            bool Result = false;
            string MPDirectory = System.IO.Path.GetDirectoryName(Path);
            ManagementPackXmlWriter MPxmlWriter = new ManagementPackXmlWriter(MPDirectory);

            try
            {
                _TargetManagementPack.AcceptChanges();
            }

            catch (Exception e)
            {
                Debug.WriteLine("......[ERR1143] Error whilst performing ManagementPack.AcceptChanges().  The error was:\n" + e.ToString(), "error");
                return false;
            }

            try
            {
                MPxmlWriter.WriteManagementPack(_TargetManagementPack);
            }

            catch (Exception)
            {
                Debug.WriteLine("......[ERR1144] Error whilst writing ManagementPack XML to file", "error");
                return false;
            }

            Result = true;

            return Result;
        }

        private bool SealManagementPack(ManagementPackCreationOptions CreationOptions, ManagementPackSealOptions SealOptions)
        {
            bool Result = false;
            string ManagementPackFilename = CreationOptions.ManagementPackFilename.Replace("#SERVICENAME#", CreationOptions.ServiceInfo.ServiceName.Replace(" ", "_").Replace("(", "").Replace(")", ""));
            string DestinationPath = System.IO.Path.GetDirectoryName(CreationOptions.ManagementPackFilename);
            string SealedMPFilename = System.IO.Path.GetFileNameWithoutExtension(ManagementPackFilename) + ".mp";
            string FullSealedMPFilename = System.IO.Path.Combine(DestinationPath, SealedMPFilename);

            Debug.WriteLine("...Sealing...", "information");

            string Commandline = SealOptions.ManagementPackSealCommandLine.Replace("#MPFILENAME#", CreationOptions.ManagementPackFilename).Replace("#KEYFILEFILENAME#", SealOptions.KeyfileFilename).Replace("#KEYCOMPANYNAME#", SealOptions.KeyCompanyName).Replace("#MPPATH#", DestinationPath);

            InitialSessionState Session = InitialSessionState.CreateDefault();

            Session.ImportPSModule(new string[] { SealOptions.PowerShellModulePath });
            Runspace runspace = RunspaceFactory.CreateRunspace(Session);
            runspace.Open();

            PowerShell ps = PowerShell.Create();
            ps.Runspace = runspace;
            ps.AddScript(Commandline);
            ps.Invoke();

            if (ps.Streams.Error.Count > 0)
            {
                Result = false;
                Debug.WriteLine("Failure", "error");
            }
            else
            {
                //_SealedFilename = FullSealedMPFilename;
                Result = true;
                Debug.WriteLine("Success...", "information");
            }

            return Result;
        }

        #endregion

        #region Public Methods

        public bool CreateOrUpdateManagementPack(string Name, Version Version, List<string> ComponentServerNames, List<string> ComponentServiceNames, ManagementPackCreationOptions CreationOptions, out string ErrorMessage, out string ManagementPackXML)
        {
            //bool SCOMConnectionResult = false;
            //bool SCSMConnectionResult = false;
            bool Result = false;
            bool SaveResult = false;
            Version CurrentVersion = null;
            ManagementGroup CurrentTarget = null;
            bool CreateMP = false;

            ErrorMessage = "";
            ManagementPackXML = "";

            // There are 3 Versions to take into account..
            // 'Version' is the Version that we wish[ed] to use
            // 'CurrentVersion' is the version of the existing Management Pack
            // 'NewVersion' is the version of the Updated Management Pack

            // SCOM and SCSM
            for (int ServerConnectionLoop = 0; ServerConnectionLoop < 2; ServerConnectionLoop++)
            {
                switch (ServerConnectionLoop)
                {
                    case 0:
                        if (CreationOptions.ImportSCOMManagementPacks)
                        {
                            if (_SCOMServerConnection != null)
                            {
                                CurrentTarget = _SCOMServerConnection;
                                //SCOMConnectionResult = true;
                                CreateMP = true;
                                Debug.WriteLine("......SCOM...", "information");
                            }
                            else
                            {
                                CreateMP = false;
                                Debug.WriteLine("[ERR1124] Could not create connection to SCOM Server", "error");
                            }
                        }
                        else
                        {
                            CreateMP = false;
                            //Debug.WriteLine("User Options prevent import of Management Packs into SCOM");
                        }
                        break;
                    case 1:
                        if (CreationOptions.ImportSCSMManagementPacks)
                        {
                            if (_SCSMServerConnection != null)
                            {
                                CurrentTarget = _SCSMServerConnection;
                                //SCSMConnectionResult = true;
                                CreateMP = true;
                                Debug.WriteLine("......SCSM...", "information");
                            }
                            else
                            {
                                CreateMP = false;
                                Debug.WriteLine("[ERR1125] Could not create connection to SCSM Server", "error");
                            }
                        }
                        else
                        {
                            CreateMP = false;
                            //Debug.WriteLine("User Options prevent import of Management Packs into SCSM");
                        }
                        break;
                }

                CreationOptions.TargetServer = CurrentTarget;

                if (CreateMP)
                {
                    if (ManagementPackExists(CreationOptions.ManagementPackID, ref CurrentVersion, CurrentTarget))
                    {
                        if ((Version <= CurrentVersion) && (!CreationOptions.AlwaysImportManagementPacks))
                        {
                            //Nothing to do
                            Debug.WriteLine(".........[INF1126] Management Pack update not required... (" + Version + " <= " + CurrentVersion + ")", "information");
                            Result = true;
                            SaveResult = false;
                            ManagementPackXML = "";
                            //return Result;
                        }
                        else
                        {
                            if (CreationOptions.AlwaysImportManagementPacks)
                            {
                                Debug.WriteLine(".........[INF1239] Management Pack update required...(Always Import MP = true)", "information");
                            }
                            else
                            {
                                Debug.WriteLine(".........[INF1127] Management Pack update required...(" + Version + " > " + CurrentVersion + ")", "information");
                            }
                            Result = CreateManagementPackXML(Version, ComponentServerNames, ComponentServiceNames, CreationOptions, out ErrorMessage, out ManagementPackXML);
                            SaveResult = true;
                        }
                    }
                    else
                    {
                        Debug.WriteLine("......Creating new MP...", "information");
                        Result = CreateManagementPackXML(Version, ComponentServerNames, ComponentServiceNames, CreationOptions, out ErrorMessage, out ManagementPackXML);
                        SaveResult = true;
                    }

                    // Seal and/or Import the Management Pack
                    if (SaveResult && Result)
                    {
                        switch (ServerConnectionLoop)
                        {
                            case 0: // SCOM
                                if (CreationOptions.ImportSCOMManagementPacks)
                                {
                                    Result = ImportManagementPack(CreationOptions);

                                    if (Result)
                                    {
                                        Debug.WriteLine(".........[INF1128] Success", "information");
                                    }
                                    else
                                    {
                                        Debug.WriteLine(".........[ERR1129] Failure", "error");
                                    }
                                }
                                else
                                {
                                    //Debug.WriteLine("User Options prevent import of Management Packs into SCOM");
                                    //Result = false;
                                }
                                break;
                            case 1: // SCSM
                                if (CreationOptions.ImportSCSMManagementPacks)
                                {
                                    Result = ImportManagementPack(CreationOptions);

                                    if (Result)
                                    {
                                        Debug.WriteLine(".........[INF1130] Success", "information");
                                    }
                                    else
                                    {
                                        Debug.WriteLine(".........[ERR1131] Failure", "error");
                                    }
                                }
                                else
                                {
                                    //Debug.WriteLine("User Options prevent import of Management Packs into SCSM");
                                    //Result = false;
                                }
                                break;
                        }
                    }
                }
                else
                {
                    Result = true; // Could not connect to SCOM / SCSM -OR- import into this SC server is not configured
                }

                //if (Result)
                //{
                //    Debug.WriteLine(".........Success");
                //}
                //else
                //{
                //    Debug.WriteLine(".........Failure");
                //}
            }

            return Result;
        }

        //public string GetServerHealth_Old(String ServerName)
        //{
        //    string Health = "";

        //    MonitoringClassCriteria ServerClassCriteria = new MonitoringClassCriteria("Name = 'System.Computer'");

        //    try
        //    {
        //        ReadOnlyCollection<MonitoringClass> ServerMonitoringClasses = _SCOMServerConnection.GetMonitoringClasses(ServerClassCriteria);

        //        MonitoringObjectCriteria ServiceObjectCriteria = new MonitoringObjectCriteria("DisplayName like '" + ServerName + "%'", ServerMonitoringClasses[0]);
        //        ReadOnlyCollection<MonitoringObject> ServiceObjects = _SCOMServerConnection.GetMonitoringObjects(ServiceObjectCriteria);

        //        if (ServiceObjects.Count > 0)
        //        {
        //            Health = ServiceObjects[0].HealthState.ToString();

        //            if (!ServiceObjects[0].IsAvailable)
        //            {
        //                Health = "Error";
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {

        //    }
        //    return Health;

        //}

        public string GetServerHealth(string ServerName)
        {
            string Health = "";

            MonitoringObject Server = GetComputerMonitoringObject(ServerName);

            if (Server != null)
            {
                Health = Server.HealthState.ToString();

                if (!Server.IsAvailable)
                {
                    Health = "Error";
                }
            }


            //ManagementPackClassCriteria ServerClassCriteria = new ManagementPackClassCriteria("Name = 'System.Computer'");

            //try
            //{
            //    IList<ManagementPackClass> ServerMonitoringClasses = _SCOMServerConnection.EntityTypes.GetClasses(ServerClassCriteria);

            //    MonitoringObjectCriteria ServiceObjectCriteria = new MonitoringObjectCriteria("DisplayName like '" + ServerName + "%'", ServerMonitoringClasses[0]);
            //    IList<MonitoringObject> ServiceObjects = _SCOMServerConnection.EntityObjects.GetObjects(ServiceObjectCriteria, ObjectQueryOptions.Default);

                

            //    if (ServiceObjects.Count > 0)
            //    {
            //        Health = ServiceObjects[0].HealthState.ToString();

            //        if (!ServiceObjects[0].IsAvailable)
            //        {
            //            Health = "Error";
            //        }
            //    }
            //}
            //catch (Exception)
            //{

            //}

            return Health;
        }

        //public string GetServerIDFromName_Old(string ServerName)
        //{
        //    string ServerID = "";

        //    MonitoringClassCriteria ServerClassCriteria = new MonitoringClassCriteria("Name = 'System.Computer'");

        //    if (_SCOMServerConnection != null)
        //    {
        //        ReadOnlyCollection<MonitoringClass> ServerMonitoringClasses = _SCOMServerConnection.GetMonitoringClasses(ServerClassCriteria);

        //        MonitoringObjectCriteria ServerObjectCriteria = new MonitoringObjectCriteria("Name like '" + ServerName + "%'", ServerMonitoringClasses[0]);
        //        ReadOnlyCollection<MonitoringObject> ServiceObjects = _SCOMServerConnection.GetMonitoringObjects(ServerObjectCriteria);

        //        if (ServiceObjects.Count > 0)
        //        {
        //            ServerID = ServiceObjects[0].Id.ToString();
        //        }
        //    }
        //    else
        //    {
        //        Debug.WriteLine("[ERR1132] Could not connect to SCOM Server");
        //    }
        //    return ServerID;

        //}

        public string GetServerIDFromName(string ServerName)
        {
            string ServerID = "";

            MonitoringObject Server = GetComputerMonitoringObject(ServerName);

            if (Server != null)
            {
                ServerID = Server.Id.ToString();
            }

            //ManagementPackClassCriteria ServerClassCriteria = new ManagementPackClassCriteria("Name = 'System.Computer'");

            //if (_SCOMServerConnection != null)
            //{
            //    IList<ManagementPackClass> ServerMonitoringClasses = _SCOMServerConnection.EntityTypes.GetClasses(ServerClassCriteria);

            //    MonitoringObjectCriteria ServerObjectCriteria = new MonitoringObjectCriteria("Name like '" + ServerName + "%'", ServerMonitoringClasses[0]);
            //    IList<MonitoringObject> ServiceObjects = _SCOMServerConnection.EntityObjects.GetObjects(ServerObjectCriteria, ObjectQueryOptions.Default);

            //    if (ServiceObjects.Count > 0)
            //    {
            //        ServerID = ServiceObjects[0].Id.ToString();
            //    }
            //}
            //else
            //{
            //    Debug.WriteLine("[ERR1132] Could not connect to SCOM Server");
            //}
            return ServerID;

        }

        //public string GetObjectIDFromName_Old(string ObjectName, string ClassName, string AttributeName)
        //{
        //    string ObjectID = "";

        //    MonitoringClassCriteria ObjectClassCriteria = new MonitoringClassCriteria("Name = '" + ClassName + "'");

        //    if (_SCOMServerConnection != null)
        //    {
        //        ReadOnlyCollection<MonitoringClass> ObjectMonitoringClasses = _SCOMServerConnection.GetMonitoringClasses(ObjectClassCriteria);

        //        MonitoringObjectCriteria ObjectObjectCriteria = new MonitoringObjectCriteria(AttributeName + " like '" + ObjectName + "%'", ObjectMonitoringClasses[0]);
        //        ReadOnlyCollection<MonitoringObject> ObjectObjects = _SCOMServerConnection.GetMonitoringObjects(ObjectObjectCriteria);

        //        if (ObjectObjects.Count > 0)
        //        {
        //            ObjectID = ObjectObjects[0].Id.ToString();
        //        }
        //    }
        //    else
        //    {
        //        Debug.WriteLine("[ERR1133] Could not connect to SCOM Server");
        //    }

        //    return ObjectID;

        //}

        public string GetObjectIDFromName(string ObjectName)
        {
            string ObjectID = "";

            MonitoringObject Object = GetMonitoringObject(ObjectName);

            if (Object != null)
            {
                ObjectID = Object.Id.ToString();
            }

            //ManagementPackClassCriteria ObjectClassCriteria = new ManagementPackClassCriteria("Name = '" + ClassName + "'");

            //if (_SCOMServerConnection != null)
            //{
            //    IList<ManagementPackClass> ObjectMonitoringClasses = _SCOMServerConnection.EntityTypes.GetClasses(ObjectClassCriteria);

            //    MonitoringObjectCriteria ObjectObjectCriteria = new MonitoringObjectCriteria(AttributeName + " like '" + ObjectName + "%'", ObjectMonitoringClasses[0]);
            //    IList<MonitoringObject> ObjectObjects = _SCOMServerConnection.EntityObjects.GetObjects(ObjectObjectCriteria, ObjectQueryOptions.Default);

            //    if (ObjectObjects.Count > 0)
            //    {
            //        ObjectID = ObjectObjects[0].Id.ToString();
            //    }
            //}
            //else
            //{
            //    Debug.WriteLine("[ERR1133] Could not connect to SCOM Server");
            //}

            return ObjectID;

        }

        public string GetServiceIDFromName(string ServiceName)
        {
            string ServiceID = "";

            MonitoringObject Application = GetApplicationMonitoringObject(ServiceName);

            if (Application != null)
            {
                ServiceID = Application.Id.ToString();
            }

            ////MonitoringClassCriteria ServiceClassCriteria = new MonitoringClassCriteria("Name = 'Microsoft.SystemCenter.ServiceDesigner.GenericService'");  // DisplayName: Distributed Application Component
            //ManagementPackClassCriteria ServiceClassCriteria = new ManagementPackClassCriteria("Name = 'System.Service'");

            //try
            //{
            //    IList<ManagementPackClass> ServiceMonitoringClasses = _SCOMServerConnection.EntityTypes.GetClasses(ServiceClassCriteria);

            //    MonitoringObjectCriteria ServiceObjectCriteria = new MonitoringObjectCriteria("DisplayName Like '%" + ServiceName + "%'", ServiceMonitoringClasses[0]);
            //    IList<MonitoringObject> ServiceObjects = _SCOMServerConnection.EntityObjects.GetObjects(ServiceObjectCriteria, ObjectQueryOptions.Default);

            //    if (ServiceObjects.Count > 0)
            //    {
            //        ServiceID = ServiceObjects[0].Id.ToString();
            //    }
            //}
            //catch (ServerDisconnectedException)
            //{
            //    _SCOMServerConnection.Reconnect();
            //}
            //catch (Exception)
            //{
            //    Debug.WriteLine("[ERR1134] Could not connect to SCOM Server");
            //}

            return ServiceID;

        }

        public bool ManagementPackExists(string Name, ref Version CurrentVersion, ManagementGroup TargetServer)
        {
            IList<ManagementPack> ExistingPacks = null;
            ManagementPackCriteria Criteria = GetCriteria(Name);

            CurrentVersion = new Version(0, 0, 0, 0); // Use 0.0.0.0 as the default

            try
            {

                ExistingPacks = TargetServer.ManagementPacks.GetManagementPacks(Criteria);

                if (ExistingPacks.Count > 0)
                {
                    CurrentVersion = ExistingPacks[0].Version;

                    return true;
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("[ERR1135] Could not get determine if the Management Pack already exists");
            }

            return false;
        }

        //public void GetObjectByName(string Name, string ClassName)
        //{
        //    ManagementPackClassCriteria ObjectClassCriteria = new ManagementPackClassCriteria("Name = '" + ClassName + "'");
        //    IList<ManagementPackClass> ObjectMonitoringClasses = _SCSMServerConnection.EntityTypes.GetClasses(ObjectClassCriteria);

        //    if (ObjectMonitoringClasses.Count > 0)
        //    {
        //        MonitoringObjectCriteria ObjectObjectCriteria = new MonitoringObjectCriteria("DisplayName like '" + Name + "%'", ObjectMonitoringClasses[0]);
        //        ReadOnlyCollection<MonitoringObject> Result = _SCSMServerConnection.GetMonitoringObjects(ObjectObjectCriteria);

        //        if (Result.Count > 0)
        //        {
        //            foreach (EnterpriseManagementSimpleObject Property in Result[0].Values)
        //            {
        //                if (Property.Value != null)
        //                {
        //                    if (Property.Type.EnumType != null)
        //                    {
        //                        Debug.WriteLine(Property.Type.DisplayName + " (" + Property.Type.SystemType.ToString() + "[" + Property.Type.EnumType.Identifier + "]) = " + Property.Value.ToString());

        //                        IList<ManagementPackEnumeration> Enums = GetEnumValues(Property.Type.DisplayName, Property.Type.EnumType.Id.ToString());

        //                        List<ManagementPackEnumeration> Values = new List<ManagementPackEnumeration>();

        //                        foreach (ManagementPackEnumeration e in Enums)
        //                        {
        //                            //if(e.
        //                            Debug.WriteLine("   " + e.Name + " = " + e.Id);

        //                            IList<ManagementPackEnumeration> Children = _SCSMServerConnection.EntityTypes.GetChildEnumerations(e.Id, TraversalDepth.Recursive);

        //                            if (Children.Count > 0)
        //                            {
        //                                foreach (ManagementPackEnumeration Child in Children)
        //                                {
        //                                    Values.Add(Child);
        //                                }

        //                            }
        //                        }

        //                        if (Values.Count > 0)
        //                        {
        //                            //Values.Sort();

        //                            foreach (ManagementPackEnumeration Child in Values)
        //                            {
        //                                Debug.WriteLine("      " + Child.Parent.Id + "\t" + Child.DisplayName + "\t" + Child.Id.ToString());
        //                            }

        //                        }
        //                    }
        //                    else
        //                    {
        //                        Debug.WriteLine(Property.Type.DisplayName + " (" + Property.Type.SystemType.ToString() + ") = " + Property.Value.ToString());
        //                    }
        //                }
        //                else
        //                {
        //                    if (Property.Type.EnumType != null)
        //                    {
        //                        Debug.WriteLine(Property.Type.DisplayName + " (" + Property.Type.SystemType.ToString() + "[" + Property.Type.EnumType.Identifier + "]) = null");
        //                        Debug.WriteLine(Property.Type.EnumType.Identifier.Path[0]);

        //                        //GetEnumValues(Property.Type.DisplayName, Property.Type.EnumType.Identifier.Path[0]);
        //                        IList<ManagementPackEnumeration> Enums = GetEnumValues(Property.Type.DisplayName, Property.Type.EnumType.Id.ToString());

        //                        foreach (ManagementPackEnumeration e in Enums)
        //                        {
        //                            Debug.WriteLine("   " + e.Name + " = " + e.Id);
        //                        }
        //                    }
        //                    else
        //                    {
        //                        Debug.WriteLine(Property.Type.DisplayName + " (" + Property.Type.SystemType.ToString() + ") = null");
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        public void GetObjectByName(string Name)
        {
            MonitoringObject Object = GetMonitoringObject(Name);

                if (Object != null)
                {
                    foreach (EnterpriseManagementSimpleObject Property in Object.Values)
                    {
                        if (Property.Value != null)
                        {
                            if (Property.Type.EnumType != null)
                            {
                                Debug.WriteLine(Property.Type.DisplayName + " (" + Property.Type.SystemType.ToString() + "[" + Property.Type.EnumType.Identifier + "]) = " + Property.Value.ToString(), "information");

                                IList<ManagementPackEnumeration> Enums = GetEnumValues(Property.Type.DisplayName, Property.Type.EnumType.Id.ToString());

                                List<ManagementPackEnumeration> Values = new List<ManagementPackEnumeration>();

                                foreach (ManagementPackEnumeration e in Enums)
                                {
                                    //if(e.
                                    Debug.WriteLine("   " + e.Name + " = " + e.Id, "information");

                                    IList<ManagementPackEnumeration> Children = _SCSMServerConnection.EntityTypes.GetChildEnumerations(e.Id, TraversalDepth.Recursive);

                                    if (Children.Count > 0)
                                    {
                                        foreach (ManagementPackEnumeration Child in Children)
                                        {
                                            Values.Add(Child);
                                        }

                                    }
                                }

                                if (Values.Count > 0)
                                {
                                    //Values.Sort();

                                    foreach (ManagementPackEnumeration Child in Values)
                                    {
                                        Debug.WriteLine("      " + Child.Parent.Id + "\t" + Child.DisplayName + "\t" + Child.Id.ToString(), "information");
                                    }

                                }
                            }
                            else
                            {
                                Debug.WriteLine(Property.Type.DisplayName + " (" + Property.Type.SystemType.ToString() + ") = " + Property.Value.ToString(), "information");
                            }
                        }
                        else
                        {
                            if (Property.Type.EnumType != null)
                            {
                                Debug.WriteLine(Property.Type.DisplayName + " (" + Property.Type.SystemType.ToString() + "[" + Property.Type.EnumType.Identifier + "]) = null", "information");
                                Debug.WriteLine(Property.Type.EnumType.Identifier.Path[0], "information");

                                //GetEnumValues(Property.Type.DisplayName, Property.Type.EnumType.Identifier.Path[0]);
                                IList<ManagementPackEnumeration> Enums = GetEnumValues(Property.Type.DisplayName, Property.Type.EnumType.Id.ToString());

                                foreach (ManagementPackEnumeration e in Enums)
                                {
                                    Debug.WriteLine("   " + e.Name + " = " + e.Id);
                                }
                            }
                            else
                            {
                                Debug.WriteLine(Property.Type.DisplayName + " (" + Property.Type.SystemType.ToString() + ") = null", "information");
                            }
                        }
                    }
                }
        }

        public MonitoringObject GetComputerMonitoringObject(string computerFQDN)
        {
            ManagementPackClass windowsComputerClass = _SCOMServerConnection.EntityTypes.GetClass(SystemMonitoringClass.Computer);
            MonitoringObjectGenericCriteria monitoringObjectCriteria = new MonitoringObjectGenericCriteria(string.Format("Name like '{0}%'", computerFQDN));

            IObjectReader<MonitoringObject> reader = _SCOMServerConnection.EntityObjects.GetObjectReader<MonitoringObject>(monitoringObjectCriteria, windowsComputerClass, ObjectQueryOptions.Default);
            List<MonitoringObject> monitoringObjects = new List<MonitoringObject>();

            monitoringObjects.AddRange(reader);

            if (monitoringObjects.Count > 0)
            {
                return (monitoringObjects[0]);
            }
            else
            {
                return null;
            }

        }

        public MonitoringObject GetApplicationMonitoringObject(string ApplicationName)
        {
            ManagementPackClass ServiceClass = _SCOMServerConnection.EntityTypes.GetClass(SystemMonitoringClass.Service);
            MonitoringObjectGenericCriteria monitoringObjectCriteria = new MonitoringObjectGenericCriteria(string.Format("DisplayName like '{0}'", ApplicationName));

            IObjectReader<MonitoringObject> reader = _SCOMServerConnection.EntityObjects.GetObjectReader<MonitoringObject>(monitoringObjectCriteria, ServiceClass, ObjectQueryOptions.Default);
            List<MonitoringObject> monitoringObjects = new List<MonitoringObject>();

            monitoringObjects.AddRange(reader);

            if (monitoringObjects.Count > 0)
            {
                return (monitoringObjects[0]);
            }
            else
            {
                return null;
            }

        }

        public MonitoringObject GetMonitoringObject(string Name)
        {
            ManagementPackClass ServiceClass = _SCOMServerConnection.EntityTypes.GetClass(SystemMonitoringClass.Entity);
            MonitoringObjectGenericCriteria monitoringObjectCriteria = new MonitoringObjectGenericCriteria(string.Format("Name like '{0}%'", Name));

            IObjectReader<MonitoringObject> reader = _SCOMServerConnection.EntityObjects.GetObjectReader<MonitoringObject>(monitoringObjectCriteria, ServiceClass, ObjectQueryOptions.Default);
            List<MonitoringObject> monitoringObjects = new List<MonitoringObject>();

            monitoringObjects.AddRange(reader);

            if (monitoringObjects.Count > 0)
            {
                return (monitoringObjects[0]);
            }
            else
            {
                return null;
            }

        }

        public IList<ManagementPackEnumeration> GetEnumValues(string Name, string ClassID)
        {
            Debug.WriteLine("");

            ManagementPackEnumeration mpenum = _SCSMServerConnection.EntityTypes.GetEnumeration(Guid.Parse(ClassID));

            Debug.WriteLine("Description: " + mpenum.Description, "information");
            Debug.WriteLine("Display Name: " + mpenum.DisplayName, "information");

            IList<ManagementPackEnumeration> Enums = _SCSMServerConnection.EntityTypes.GetEnumerations()
                                                     .OrderBy(e => e.Name)
                                                     .Where(e => e.Parent != null && e.Parent.Id.ToString() == ClassID)
                                                     .ToList<ManagementPackEnumeration>();


            return Enums;
        }

        #endregion
    }
}
