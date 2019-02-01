using Clipbox.Core.Extensibility.Attributes;
using Clipbox.Core.Extensibility.Contracts;
using Clipbox.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Clipbox
{
    [Serializable]
    internal class FailureInfo
    {
        public long WorkingSet { get; set; }

        public int CpuCount { get; set; }

        public bool Is64BitOs { get; set; }

        public double OsVersion { get; set; }

        public Exception Exception { get; set; }

        public object ExceptionObject { get; set; }

        public string ExceptionObjectType { get; set; }

        public List<string> LoadedExtentions { get; set; }

        public string FailedExtentionType { get; set; }

        public double FailedExtentionVersion { get; set; }

        public string FailedExtentionAuthor { get; set; }

        public string FailedExtentionDescription { get; set; }

        public long UtcTicks { get; set; }

        public long LocalTicks { get; set; }

        public void LoadExceptionObject(object exceptionObject)
        {
            ExceptionObject = exceptionObject;
            ExceptionObjectType = exceptionObject.GetType().ToString();
            if (exceptionObject is Exception)
            {
                Exception = exceptionObject as Exception;
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

            return fi;
        }
        

        public void LoadFailedExtentionInfo(Type extType)
        {
            FailedExtentionType = extType.ToString();

            IExtentionInfo extInfo = null;

            if (ClipboxExtentionAttribute.IsDefinedFor(extType))
            {
                extInfo = ClipboxExtentionAttribute.GetExtentionInfo(extType);
                FailedExtentionVersion = extInfo.Version;
                FailedExtentionAuthor = extInfo.Author;
                FailedExtentionDescription = extInfo.Description;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Error: {ExceptionObjectType ?? "Unknown."} at {new DateTime(LocalTicks)} local time ({new DateTime(UtcTicks)} UTC.)");
            if (ExceptionObjectType != null)
            {
                sb.AppendLine(ExceptionObjectType);
            }
            sb.AppendLine($"OS Version: {OsVersion}; Is64BitOs: { Is64BitOs}; WorkingSet: {WorkingSet}; CpuCount: {CpuCount}");

            if (FailedExtentionType != null)
            {
                sb.AppendLine($"Failed extention type: {FailedExtentionType}; FailedExtentionVersion: {FailedExtentionVersion}; FailedExtentionAuthor: {FailedExtentionAuthor}; FailedExtentionDescription: {FailedExtentionDescription}");
            }

            sb.AppendLine($"Loaded extentions:");
            sb.AppendLine(string.Join(Environment.NewLine, LoadedExtentions));

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
