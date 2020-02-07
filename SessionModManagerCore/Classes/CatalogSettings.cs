using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SessionModManagerCore.Classes
{
    public class CatalogSettings
    {        
        public List<string> CatalogUrls { get; set; }

        public CatalogSettings()
        {
            CatalogUrls = new List<string>();
        }
    }
}
