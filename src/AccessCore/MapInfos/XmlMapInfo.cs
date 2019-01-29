using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace AccessCore.Repository.MapInfos
{
    /// <summary>
    /// Class for mapping SQL to models with XML desciption
    /// </summary>
    public class XmlMapInfo : MapInfo
    {
        /// <summary>
        /// Creates new instance of <see cref="XmlMapInfo"/>
        /// </summary>
        /// <param name="path">path of XML file containing mapping information.</param>
        public XmlMapInfo(string path) : base(path)
        {
        }

        /// <summary>
        /// Sets map info from XML file
        /// </summary>
        public override void SetMapInfo()
        {
            base.SetMapInfo();

            // getting xml document
            var xml = XDocument.Load(this._path);

            // getting operations
            var operations = xml.Element("operations").Elements("operation");

            // variables for storing
            var opNames = new Dictionary<string, string>();
            var returnValues = new Dictionary<string, ReturnDataType>();
            var parameters = new Dictionary<string, Dictionary<string, string>>();

            // loop over operations
            foreach (var operation in operations)
            {
                // getting operation name
                var opName = operation.Attribute("name");

                // getting stored procedure name
                var spName = operation.Element("spName");

                // adding names
                opNames.Add(opName.Value, spName.Value);

                // getting return values and adding
                var returnDataType = operation.Element("returnDataType");

                returnValues.Add(opName.Value,
                    (ReturnDataType)Enum.Parse(typeof(ReturnDataType), returnDataType.Value));

                // getting parameters xml element
                var paramsXML = operation.Element("parameters");

                // getting parameters if they exist
                if (paramsXML != null)
                {
                    var paramsList = new Dictionary<string, string>();

                    // loop over parameters
                    foreach (var parameter in paramsXML.Elements("parameter"))
                    {
                        var paramName = parameter.Element("parameterName");

                        var spParamName = parameter.Element("spParameterName");

                        paramsList.Add(paramName.Value, spParamName.Value);
                    }

                    // add parameters
                    parameters.Add(opName.Value, paramsList);
                }
                // otherwise add null indicating that this operation doesn't have parameters
                else parameters.Add(opName.Value, null);
            }

            this.ConstructMapInfo(opNames, returnValues, parameters);
        }
    }
}
