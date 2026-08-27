using Microsoft.EnterpriseManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VaderConsulting.SystemCenter
{
    public class ManagementPackCreationOptions
    {
        public string ManagementPackDescription = "";
        public string ManagementPackFilename = "";
        public string ManagementPackName = "";
        public string ManagementPackID = "";
        public Version ManagementPackVersion = null;
        public bool ImportSCOMManagementPacks = false;
        public bool ImportSCSMManagementPacks = false;
        public bool UseDynamicDiscovery = false;
        public string ManagementPackEmptyTemplate = "";
        public string ManagementPackServersAndServicesTemplate = "";
        public string ManagementPackServersTemplate = "";
        public string ManagementPackServicesTemplate = "";
        public string ManagementPackFolderName = "";
        public bool AlwaysImportManagementPacks = false;
        public ManagementGroup TargetServer = null;

        public ServiceInfo ServiceInfo = new ServiceInfo();
        public ConnectionInfo ConnectionInfo = new ConnectionInfo();
    }
}
