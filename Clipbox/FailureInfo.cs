using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Clipbox
{
    [Serializable]
    public class FailureInfo
    {
        [NonSerialized]
        private object _exceptionObject;
        
        [XmlIgnore]
        public object ExceptionObject
        {
            get { return _exceptionObject; }
            set { _exceptionObject = value; }
        }

        public string ExceptionObjectType { get; set; }

        public string ExceptionStr { get; set; }

        public long WorkingSet { get; set; }

        public int CpuCount { get; set; }

        public bool Is64BitOs { get; set; }

        public double OsVersion { get; set; }

        public float AvailableSystemMemoryMB { get; set; }

        public List<string> LoadedExtensions { get; set; }

        public string FailedExtensionType { get; set; }

        public double FailedExtensionVersion { get; set; }

        public string FailedExtensionAuthor { get; set; }

        public string FailedExtensionDescription { get; set; }

        public long UtcTicks { get; set; }

        public long LocalTicks { get; set; }

        public void LoadExceptionObject(object exceptionObject)
        {
            ExceptionObject = exceptionObject;
            ExceptionObjectType = exceptionObject.GetType().ToString();
            if (exceptionObject is Exception)
            {
                ExceptionStr = ((Exception)exceptionObject).ToString();
            }
        }

        public static FailureInfo CreateWithSystemSnapshot()
        {
            var fi = new FailureInfo
            {
                Is64BitOs = Environment.Is64BitOperatingSystem,
                CpuCount = Environment.ProcessorCount,
                WorkingSet = Environment.WorkingSet,
                LocalTicks = DateTime.Now.Ticks,
                UtcTicks = DateTime.UtcNow.Ticks
            };

            try { fi.OsVersion = OsVersionUtils.GetWindowsVersion(); }
            catch { }

            try
            {
                var performance = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes");
                fi.AvailableSystemMemoryMB = performance.NextValue();
            }
            catch { }

            return fi;
        }
        

        public void LoadFailedExtensionInfo(Type extType)
        {
            FailedExtensionType = extType.ToString();

            IExtensionInfo extInfo = null;

            if (ClipboxExtensionAttribute.IsDefinedFor(extType))
            {
                extInfo = ClipboxExtensionAttribute.GetExtensionInfo(extType);
                FailedExtensionVersion = extInfo.Version;
                FailedExtensionAuthor = extInfo.Author;
                FailedExtensionDescription = extInfo.Description;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Error: {ExceptionObjectType ?? "Unknown."} at {new DateTime(LocalTicks)} local time ({new DateTime(UtcTicks)} UTC.)");
            
            if (ExceptionObject != null)
            {
                sb.AppendLine(ExceptionObject.ToString());
            }

            sb.AppendLine($"OS Version: {OsVersion}; Is 64 Bit Os: { Is64BitOs}; Working Set: {WorkingSet}; Available Memory (MB): {AvailableSystemMemoryMB}; CpuCount: {CpuCount}");

            if (FailedExtensionType != null)
            {
                sb.AppendLine($"Failed extension type: {FailedExtensionType}; FailedExtensionVersion: {FailedExtensionVersion}; FailedExtensionAuthor: {FailedExtensionAuthor}; FailedExtensionDescription: {FailedExtensionDescription}");
            }

            sb.AppendLine($"Loaded extensions:");
            sb.AppendLine(string.Join(Environment.NewLine, LoadedExtensions ?? Enumerable.Empty<string>()));

            return sb.ToString();
        }

        public string ToXml()
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(FailureInfo));
            string xml = string.Empty;

            using (var sw = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sw))
                {
                    xmlSerializer.Serialize(writer, this);
                    xml = sw.ToString();
                }
            }

            return xml;
        }
    }
}
