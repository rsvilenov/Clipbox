using System;
using System.Linq;
using System.Reflection;

namespace Clipbox.Core.Extensibility.Attributes
{
    public class CloudSupportAttribute : Attribute
    {
        public CloudSupportAttribute(string cloudServiceName, string iconResourcePath)
        {
            _cloudServiceName = cloudServiceName;
            _iconResourcePath = iconResourcePath;
        }

        private readonly string _cloudServiceName;
        private readonly string _iconResourcePath;

        public static bool IsDefinedFor(Type type)
        {
            return GetCustomAttributeData(type) != null;
        }

        public static ICloudInfo GetCloudInfo(Type type)
        {
            CustomAttributeData extensionAttribute = GetCustomAttributeData(type);

            if (extensionAttribute == null)
            {
                throw new InvalidOperationException($"Attribute {nameof(CloudSupportAttribute)} has not been defined for type {type.Name}");
            }

            var cloudInfo = new CloudInfo
            {
                ExtensionObjectType = type,
                CloudServiceName = extensionAttribute.ConstructorArguments[0].Value as string,
            };

            if (extensionAttribute.ConstructorArguments[1].Value is string iconResPath)
            {
                if (iconResPath != string.Empty)
                {
                    using (var stream = type.Assembly
                        .GetManifestResourceStream(string.Format("{0}.Resources.Icons.{1}",
                        type.Assembly.GetName().Name,
                        iconResPath)))
                    {
                        if (stream != null)
                        {
                            byte[] ba = new byte[stream.Length];
                            stream.Read(ba, 0, ba.Length);
                            cloudInfo.IconImageData = ba;
                        }
                        else
                        {
                            throw new ArgumentException($"{nameof(iconResPath)} cannot be found in Resources/Icons. Is it's build action set to 'Embedded resource'?");
                        }
                    }
                }
            }

            return cloudInfo;
        }

        private static CustomAttributeData GetCustomAttributeData(Type type)
        {
            return CustomAttributeData
                            .GetCustomAttributes(type)
                            .Where(cad => cad.AttributeType.AssemblyQualifiedName == typeof(CloudSupportAttribute).AssemblyQualifiedName)
                            .FirstOrDefault();
        }

        private class CloudInfo : ICloudInfo
        {
            public Type ExtensionObjectType { get; set; }

            public string CloudServiceName { get; set; }

            public byte[] IconImageData { get; set; }
        }

    }
}
