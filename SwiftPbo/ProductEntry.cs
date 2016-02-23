using System;
using System.Collections.Generic;

namespace SwiftPbo
{
    [Serializable]
    public class ProductEntry
    {
        private String _prefix;
        private String _productName;
        private String _productVersion;
        private List<string> _addtional = new List<string>();

        public ProductEntry()
        {
            _prefix = _productName = _productVersion = "";
            Addtional = new List<string>();
        }
        public ProductEntry(string prefix, string productName, string productVersion, List<string> addList = null)
        {
            Prefix = prefix;
            ProductName = productName;
            ProductVersion = productVersion;
            if (addList != null)
                Addtional = addList;
        }

        public string Prefix
        {
            get { return _prefix; }
            set { _prefix = value; }
        }

        public string ProductName
        {
            get { return _productName; }
            set { _productName = value; }
        }

        public string ProductVersion
        {
            get { return _productVersion; }
            set { _productVersion = value; }
        }

        public List<string> Addtional
        {
            get { return _addtional; }
            set { _addtional = value; }
        }
    }
}