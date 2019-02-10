using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AccessCore.Repository.MapInfos
{
    /// <summary>
    /// Class for mappinng information
    /// </summary>
    public class MapInfo
    {
        /// <summary>
        /// Boolean value indicating if the map info is constructed
        /// </summary>
        private bool _isConstructed;

        /// <summary>
        /// Gets or sets operations names
        /// </summary>
        public ReadOnlyDictionary<string, string> OpNames
        {
            get; private set;
        }

        /// <summary>
        /// Gets or sets return values
        /// </summary>
        public ReadOnlyDictionary<string, ReturnDataType> ReturnValues
        {
            get; private set;
        }

        /// <summary>
        /// Gets or sets parameters
        /// </summary>
        public ReadOnlyDictionary<string, Dictionary<string, string>> Parameters
        {
            get; private set;
        }

        /// <summary>
        /// Merges the given mapinfo to the current.
        /// </summary>
        /// <param name="mapInfo">Map information that will be merged.</param>
        /// <returns>merged map informations</returns>
        public MapInfo Merge(MapInfo mapInfo)
        {
            var opNames = new Dictionary<string, string>();
            var returnValues = new Dictionary<string, ReturnDataType>();
            var parameters = new Dictionary<string, Dictionary<string, string>>();

            var keys = this.OpNames.Keys;

            foreach (var key in keys)
            {
                opNames.Add(key, this.OpNames[key]);
                returnValues.Add(key, this.ReturnValues[key]);
                parameters.Add(key, this.Parameters[key]);
            }

            keys = mapInfo.OpNames.Keys;

            foreach (var key in keys)
            {
                if (!OpNames.ContainsKey(key))
                {
                    opNames.Add(key, mapInfo.OpNames[key]);
                    returnValues.Add(key, mapInfo.ReturnValues[key]);
                    parameters.Add(key, mapInfo.Parameters[key]);
                }
            }

            var result = new MapInfo();
            result._isConstructed = true;
            result.ConstructMapInfo(opNames, returnValues, parameters);
            return result;
        }

        /// <summary>
        /// Gets mapping information from file
        /// </summary>
        public virtual void SetMapInfo()
        {
            this.Check();
        }

        /// <summary>
        /// Constructs map info
        /// </summary>
        /// <param name="opNames">Operation names</param>
        /// <param name="returnValues">Return values</param>
        /// <param name="parameters">Parameters</param>
        protected void ConstructMapInfo(
            Dictionary<string, string> opNames,
            Dictionary<string, ReturnDataType> returnValues,
            Dictionary<string, Dictionary<string, string>> parameters)
        {
            this.OpNames = new ReadOnlyDictionary<string, string>(opNames);
            this.ReturnValues = new ReadOnlyDictionary<string, ReturnDataType>(returnValues);
            this.Parameters = new ReadOnlyDictionary<string, Dictionary<string, string>>(parameters);
        }

        /// <summary>
        /// Checks if the map info is already constructed
        /// </summary>
        private void Check()
        {
            if (this._isConstructed)
            {
                throw new InvalidOperationException("Already constructed");
            }

            this._isConstructed = true;
        }
    }
}