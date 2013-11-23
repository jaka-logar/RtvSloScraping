using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Xml;

namespace RtvSlo.Core.Configuration
{
    public static class RtvSloConfig
    {
        public static string RepositoryUrl { get { return RtvSloConfig.Settings.RepositoryUrl; } }
        public static string RepositoryName { get { return RtvSloConfig.Settings.RepositoryName; } }
        public static string RtvSloUrl { get { return RtvSloConfig.Settings.RtvSloUrl; } }
        public static RtvSloArchiveConfig Archive { get { return RtvSloConfig.Settings.Archive; } }
        public static string RtvSloCommentsUrl { get { return RtvSloConfig.Settings.RtvSloCommentsUrl; } }
        public static string CultureInfo { get { return RtvSloConfig.Settings.CultureInfo; } }
        public static string RepositoryDomainName { get { return RtvSloConfig.Settings.RepositoryDomainName; } }
        public static string RtvSloName { get { return RtvSloConfig.Settings.RtvSloName; } }
        public static int RequestSleep { get { return RtvSloConfig.Settings.RequestSleep; } }
        public static int MainThreadSleep { get { return RtvSloConfig.Settings.MainThreadSleep; } }
        public static bool UseFullNamespaceUrl { get { return RtvSloConfig.Settings.UseFullNamespaceUrl; } }

        public static int Step1InactiveTimeoutMinutes { get { return RtvSloConfig.Settings.Step1InactiveTimeoutMinutes; } }
        public static int Step2InactiveTimeoutMinutes { get { return RtvSloConfig.Settings.Step2InactiveTimeoutMinutes; } }
        public static int Step3InactiveTimeoutMinutes { get { return RtvSloConfig.Settings.Step3InactiveTimeoutMinutes; } }
        public static int Step3UpdateOlderThanDays { get { return RtvSloConfig.Settings.Step3UpdateOlderThanDays; } }

        private static RtvSloConfigSection _settings = null;
        private static RtvSloConfigSection Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = ConfigurationManager.GetSection("RtvSloConfig") as RtvSloConfigSection;
                }

                return _settings;
            }
        }
    }

    public class RtvSloConfigSection : IConfigurationSectionHandler
    {
        public string RepositoryUrl { get; set; }
        public string RepositoryName { get; set; }
        public string RtvSloUrl { get; set; }
        public RtvSloArchiveConfig Archive { get; set; }
        public string RtvSloCommentsUrl { get; set; }
        public string CultureInfo { get; set; }
        public string RepositoryDomainName { get; set; }
        public string RtvSloName { get; set; }
        public int RequestSleep { get; set; }
        public int MainThreadSleep { get; set; }
        public bool UseFullNamespaceUrl { get; set; }

        public int Step1InactiveTimeoutMinutes { get; set; }
        public int Step2InactiveTimeoutMinutes { get; set; }
        public int Step3InactiveTimeoutMinutes { get; set; }
        public int Step3UpdateOlderThanDays { get; set; }

        public RtvSloConfigSection()
        {
            this.Archive = new RtvSloArchiveConfig();
        }

        public object Create(object parent, object configContext, XmlNode section)
        {
            RtvSloConfigSection config = new RtvSloConfigSection();

            config.RepositoryUrl = RtvSloConfigSection.GetStringConfigurationProperty(section, "Repository", attribute: "url");
            config.RepositoryName = RtvSloConfigSection.GetStringConfigurationProperty(section, "Repository", attribute: "name");
            config.RtvSloUrl = RtvSloConfigSection.GetStringConfigurationProperty(section, "RtvSlo", attribute: "url");
            config.RtvSloCommentsUrl = RtvSloConfigSection.GetStringConfigurationProperty(section, "RtvSlo", attribute: "comments");

            config.Archive.ArchiveUrl = RtvSloConfigSection.GetStringConfigurationProperty(section, "RtvSlo", attribute: "archive");
            config.CultureInfo = RtvSloConfigSection.GetStringConfigurationProperty(section, "CultureInfo");
            config.RepositoryDomainName = RtvSloConfigSection.GetStringConfigurationProperty(section, "Repository", attribute: "domainName");
            config.RtvSloName = RtvSloConfigSection.GetStringConfigurationProperty(section, "RtvSlo", attribute: "name");
            config.UseFullNamespaceUrl = bool.Parse(RtvSloConfigSection.GetStringConfigurationProperty(section, "UseFullNamespaceUrl"));

            config.Step1InactiveTimeoutMinutes = int.Parse(RtvSloConfigSection.GetStringConfigurationProperty(section, "Step1Settings", attribute: "inactiveTimeoutMinutes"));
            config.Step2InactiveTimeoutMinutes = int.Parse(RtvSloConfigSection.GetStringConfigurationProperty(section, "Step2Settings", attribute: "inactiveTimeoutMinutes"));
            config.Step3InactiveTimeoutMinutes = int.Parse(RtvSloConfigSection.GetStringConfigurationProperty(section, "Step3Settings", attribute: "inactiveTimeoutMinutes"));
            config.Step3UpdateOlderThanDays = int.Parse(RtvSloConfigSection.GetStringConfigurationProperty(section, "Step3Settings", attribute: "updateOlderThanDays"));

            config.RequestSleep = 200; /// fallback
            XmlNode requestSleep = section.SelectSingleNode("RequestSleep");
            if (requestSleep != null)
            {
                XmlAttribute attribute = requestSleep.Attributes["ms"];
                if (attribute != null)
                {
                    int sleep;
                    if (int.TryParse(attribute.Value, out sleep))
                    {
                        config.RequestSleep = sleep;
                    }
                }
            }

            config.MainThreadSleep = 10; /// fallback
            XmlNode mainThreadSleep = section.SelectSingleNode("MainThreadSleep");
            if (mainThreadSleep != null)
            {
                XmlAttribute attribute = mainThreadSleep.Attributes["s"];
                if (attribute != null)
                {
                    int sleep;
                    if (int.TryParse(attribute.Value, out sleep))
                    {
                        config.MainThreadSleep = sleep;
                    }
                }
            }

            XmlNode archiveProperties = section.SelectSingleNode("ArchiveProperties");
            if (archiveProperties != null)
            {
                DateTime fromDate = DateTime.Today;
                DateTime toDate = DateTime.Today;

                XmlAttribute attribute = archiveProperties.Attributes["fromDate"];
                if (attribute != null)
                {
                    if (!DateTime.TryParse(attribute.Value, out fromDate))
                    {
                        fromDate = DateTime.Today.AddDays(-15);
                    }
                }

                attribute = archiveProperties.Attributes["toDate"];
                if (attribute != null)
                {
                    if(!DateTime.TryParse(attribute.Value, out toDate)){
                        toDate = DateTime.Today.AddDays(-30);
                    }
                }

                config.Archive.FromDate = fromDate;
                config.Archive.ToDate = toDate;


                attribute = archiveProperties.Attributes["searchType"];
                if (attribute != null)
                {
                    config.Archive.SearchType = attribute.Value;
                }

                IList<string> sections = new List<string>();
                Regex regex = new Regex(@"^(section\w+)Code$", RegexOptions.Singleline);
                foreach (XmlAttribute attr in archiveProperties.Attributes)
                {
                    Match match = regex.Match(attr.Name);
                    if (match.Success)
                    {
                        string sectionName = match.Groups[1].Value;

                        XmlAttribute properyEnabled = archiveProperties.Attributes[sectionName];
                        bool enabled = false;
                        if (properyEnabled != null && bool.TryParse(properyEnabled.Value, out enabled) && enabled)
                        {
                            sections.Add(attr.Value);
                        }
                    }
                }

                config.Archive.Sections = string.Join(".", sections);
            }

            return config;
        }

        private static string GetStringConfigurationProperty(
            XmlNode section,
            string node,
            string attribute = "value",
            bool validate = true)
        {
            string result = null;

            XmlNode value = section.SelectSingleNode(node);
            if (value != null)
            {
                XmlAttribute attributeValue = value.Attributes[attribute];
                if (attributeValue != null)
                {
                    result = attributeValue.Value;
                }
            }

            if (validate && string.IsNullOrWhiteSpace(result))
            {
                throw new ConfigurationErrorsException(
                    string.Format("Missing configuration for node '{0}' and attribute name '{1}'",
                    node,
                    attribute));
            }

            return result;
        }
    }
}
