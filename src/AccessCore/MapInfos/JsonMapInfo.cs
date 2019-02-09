using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AccessCore.Repository.MapInfos
{
    /// <summary>
    /// Class for mapping SQL to models with JSON description
    /// </summary>
    public class JsonMapInfo : FileBasedMapInfo
    {
        /// <summary>
        /// Creates new instance of <see cref="JsonMapInfo"/>
        /// </summary>
        /// <param name="path">Path of the JSON file</param>
        public JsonMapInfo(string path) : base(path)
        {
        }

        /// <summary>
        /// Sets mapping information from JSON file.
        /// </summary>
        public override void SetMapInfo()
        {
            base.SetMapInfo();

            var json = File.ReadAllText(this._path);
            var operations = JsonConvert.DeserializeObject<List<Operation>>(json);
            var opNames = new Dictionary<string, string>();
            var returnValues = new Dictionary<string, ReturnDataType>();
            var parameters = new Dictionary<string, Dictionary<string, string>>();

            foreach(var operation in operations)
            {
                opNames.Add(operation.Name, operation.SpName);
                returnValues.Add(operation.Name,
                        (ReturnDataType)Enum.Parse(
                        typeof(ReturnDataType), operation.ReturnDataType));

                if(operation.Parameters != null)
                {
                    parameters.Add(operation.Name,
                        operation.Parameters.ToDictionary(
                            parameter => parameter.ParameterName,
                            parameter => parameter.SpParameterName));
                }
                else
                {
                    parameters.Add(operation.Name, null);
                }
            }

            this.ConstructMapInfo(opNames, returnValues, parameters);
        }
    }
}
