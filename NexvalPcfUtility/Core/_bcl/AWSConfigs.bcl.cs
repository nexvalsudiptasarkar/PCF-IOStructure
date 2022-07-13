using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

using System.Configuration;
using System.Diagnostics;
using System.Linq;
using Amazon.Runtime.Internal.Util;
using Amazon.Util;
using System.Xml;
using System.Reflection;
using System.Text;

namespace Amazon
{

    public static partial class AWSConfigs
    {
        #region ApplicationName


        public static string ApplicationName
        {
            get { return _rootConfig.ApplicationName; }
            set { _rootConfig.ApplicationName = value; }
        }

        #endregion

        #region Config

        public static string GetConfig(string name)
        {
            NameValueCollection appConfig = ConfigurationManager.AppSettings;
            if (appConfig == null)
                return null;
            string value = appConfig[name];
            return value;
        }

        internal static T GetSection<T>(string sectionName)
            where T : class, new()
        {
            object section = ConfigurationManager.GetSection(sectionName);
            if (section == null)
                return new T();
            return section as T;
        }

        internal static bool XmlSectionExists(string sectionName)
        {
            var section = ConfigurationManager.GetSection(sectionName);
            var element = section as System.Xml.XmlElement;
            return (element != null);
        }

        #endregion

        #region TraceListeners
        private static Dictionary<string, List<TraceListener>> _traceListeners
            = new Dictionary<string, List<TraceListener>>(StringComparer.OrdinalIgnoreCase);

        public static void AddTraceListener(string source, TraceListener listener)
        {
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source cannot be null or empty", "source");
            if (null == listener)
                throw new ArgumentException("Listener cannot be null", "listener");

            lock (_traceListeners)
            {
                if (!_traceListeners.ContainsKey(source))
                    _traceListeners.Add(source, new List<TraceListener>());
                _traceListeners[source].Add(listener);
            }

            Logger.ClearLoggerCache();
        }
        public static void RemoveTraceListener(string source, string name)
        {
            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source cannot be null or empty", "source");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty", "name");

            lock (_traceListeners)
            {
                if (_traceListeners.ContainsKey(source))
                {
                    foreach (var l in _traceListeners[source])
                    {
                        if (l.Name.Equals(name, StringComparison.Ordinal))
                        {
                            _traceListeners[source].Remove(l);
                            break;
                        }
                    }
                }
            }

            Logger.ClearLoggerCache();
        }

        internal static TraceListener[] TraceListeners(string source)
        {
            lock (_traceListeners)
            {
                List<TraceListener> temp;

                if (_traceListeners.TryGetValue(source, out temp))
                {
                    return temp.ToArray();
                }

                return new TraceListener[0];
            }
        }

        #endregion

        #region Generate Config Template

        public static string GenerateConfigTemplate()
        {
            Assembly a = typeof(AWSConfigs).Assembly;
            Type t = a.GetType("Amazon.AWSSection");

            var xmlSettings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                IndentChars = "    ",
                NewLineOnAttributes = true,
                NewLineChars = "\n"
            };

            var sb = new StringBuilder();

            using (var xml = XmlWriter.Create(sb, xmlSettings))
            {
                FormatConfigSection(t, xml, tag: "aws");
            }

            return sb.ToString();
        }

        private static void FormatConfigSection(Type section, XmlWriter xml, string tag = null)
        {
            var props = section.GetProperties(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.DeclaredOnly);

            var attrs = props.Where(p => !IsConfigurationElement(p)).ToList();
            var subsections = props.Where(p => IsConfigurationElement(p)
                && !IsConfigurationElementCollection(p)).ToList();
            var collections = props.Where(p => IsConfigurationElementCollection(p)).ToList();

            xml.WriteStartElement(tag);
            foreach (var prop in attrs)
            {
                var name = ConfigurationPropertyName(prop);
                xml.WriteAttributeString(name, GetExampleForType(prop));
            }

            foreach (var prop in subsections)
            {
                var sectionName = ConfigurationPropertyName(prop);
                if (!string.IsNullOrEmpty(sectionName))
                    FormatConfigSection(prop.PropertyType, xml, sectionName);
            }

            foreach (var coll in collections)
            {
                FormatConfigurationListItems(coll, xml);
            }

            xml.WriteEndElement();
        }

        private static string GetExampleForType(PropertyInfo prop)
        {
            if (prop.PropertyType.Equals(typeof(bool?)))
                return "true | false";
            if (prop.PropertyType.Equals(typeof(int?)))
                return "1234";
            if (prop.PropertyType.Equals(typeof(String)))
                return "string value";
            if (prop.PropertyType.Equals(typeof(Type)))
                return "NameSpace.Class, Assembly";

            if (prop.PropertyType.IsEnum)
            {
                var members = Enum.GetNames(prop.PropertyType);
                var separator = IsFlagsEnum(prop) ? ", " : " | ";
                return string.Join(separator, members.ToArray());
            }

            return "( " + prop.PropertyType.FullName + " )";
        }

        private static void FormatConfigurationListItems(PropertyInfo section, XmlWriter xml)
        {
            var sectionName = ConfigurationPropertyName(section);
            var itemType = TypeOfConfigurationCollectionItem(section);

            var item = Activator.CreateInstance(section.PropertyType);
            var nameProperty = section.PropertyType.GetProperty("ItemPropertyName",
                                    BindingFlags.NonPublic | BindingFlags.Instance);
            var itemTagName = nameProperty.GetValue(item, null).ToString();

            FormatConfigSection(itemType, xml, itemTagName);
            FormatConfigSection(itemType, xml, itemTagName);
        }

        private static bool IsFlagsEnum(PropertyInfo prop)
        {
            return prop.PropertyType.GetCustomAttributes(typeof(FlagsAttribute), false).Any();
        }

        private static bool IsConfigurationElement(PropertyInfo prop)
        {
            return typeof(ConfigurationElement).IsAssignableFrom(prop.PropertyType);
        }

        private static bool IsConfigurationElementCollection(PropertyInfo prop)
        {
            return typeof(ConfigurationElementCollection).IsAssignableFrom(prop.PropertyType);
        }

        private static Type TypeOfConfigurationCollectionItem(PropertyInfo prop)
        {
            var configCollAttr = prop.PropertyType
                .GetCustomAttributes(typeof(ConfigurationCollectionAttribute), false)
                .First();
            return ((ConfigurationCollectionAttribute)configCollAttr).ItemType;
        }

        private static string ConfigurationPropertyName(PropertyInfo prop)
        {
            var configAttr = prop.GetCustomAttributes(typeof(ConfigurationPropertyAttribute), false)
                .FirstOrDefault() as ConfigurationPropertyAttribute;

            return null == configAttr ? prop.Name : configAttr.Name;
        }

        #endregion
    }
}
