using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace Glacie.Testing
{
    internal struct ConfigurationReader
    {
        public static void Read(string path, Configuration configuration)
        {
            var document = XDocument.Load(path, LoadOptions.None);
            var reader = new ConfigurationReader(configuration);
            reader.ReadDocument(document);
        }

        private readonly Configuration _configuration;

        private ConfigurationReader(Configuration configuration)
        {
            _configuration = configuration;
        }

        public void ReadDocument(XDocument document)
        {
            var element = document.Root;
            if (element.Name.LocalName != "configuration") throw Error.InvalidOperation("Invalid configuration file.");

            foreach (var e in element.Elements())
            {
                switch (e.Name.LocalName)
                {
                    case "root":
                        ReadRoot(e);
                        break;

                    case "titanQuest":
                        ReadTitanQuest(e);
                        break;

                    case "titanQuestAnniversaryEdition":
                        ReadTitanQuestAnniversaryEdition(e);
                        break;

                    case "grimDawn":
                        ReadGrimDawn(e);
                        break;

                    default:
                        throw Error.InvalidOperation("Unknown element.");
                }
            }
        }

        private void ReadRoot(XElement e)
        {
            Check.That(e.Name.LocalName == "root");

            var valueAttribute = e.Attribute("value");
            if (valueAttribute != null)
            {
                var v = valueAttribute.Value;

                if (v == "true" || v == "1")
                {
                    _configuration.Root = true;
                }
                else if (v == "false" || v == "0")
                {
                    _configuration.Root = false;
                }
                else throw Error.InvalidOperation("Invalid value.");
            }
        }

        private void ReadGrimDawn(XElement e)
        {
            Check.That(e.Name.LocalName == "grimDawn");

            var pathAttribute = e.Attribute("path");
            if (pathAttribute != null)
            {
                _configuration.GrimDawnPath = pathAttribute.Value;
            }
        }

        private void ReadTitanQuest(XElement e)
        {
            Check.That(e.Name.LocalName == "titanQuest");

            var pathAttribute = e.Attribute("path");
            if (pathAttribute != null)
            {
                _configuration.TitanQuestPath = pathAttribute.Value;
            }
        }

        private void ReadTitanQuestAnniversaryEdition(XElement e)
        {
            Check.That(e.Name.LocalName == "titanQuestAnniversaryEdition");

            var pathAttribute = e.Attribute("path");
            if (pathAttribute != null)
            {
                _configuration.TitanQuestAnniversaryEditionPath = pathAttribute.Value;
            }
        }
    }
}
