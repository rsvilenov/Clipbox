using Clipbox.Infrastructure.Serialization;
using System;
using System.Collections.Generic;

namespace Clipbox.Services.DataObjects
{
    public class SettingsStringCollection
    {
        private List<string> _keyCollection;
        public SettingsStringCollection()
        {
            _keyCollection = new List<string>();
        }

        public bool IsInErrorState { get; private set; }

        public Exception Error { get; set; }

        public void Load(string xml)
        {
            try
            {
                _keyCollection = XmlSerialization.Deserialize<List<string>>(xml);
            }
            catch (Exception e)
            {
                Error = e;
                IsInErrorState = true;
            }
        }

        public bool Contains(string key)
        {
            return _keyCollection.Contains(key);
        }

        public void Add(string key)
        {
            _keyCollection.Add(key);
        }

        public void Clear()
        {
            _keyCollection.Clear();
        }

        public string GetXml()
        {
            string xml = null;

            try
            {
                xml = XmlSerialization.Serialize(_keyCollection);
            }
            catch (Exception e)
            {
                Error = e;
                IsInErrorState = true;
            }

            return xml;
        }
    }
}
