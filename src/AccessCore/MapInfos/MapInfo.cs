using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AccessCore.Repository.MapInfos
{
    /// <summary>
    /// Class for mappinng information
    /// </summary>
    public abstract class MapInfo 
    {
        /// <summary>
        /// Mapping info file path
        /// </summary>
        protected readonly string _path;

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
        /// Creates new instance of <see cref="MapInfo"/>
        /// </summary>
        /// <param name="path"></param>
        public MapInfo(string path)
        {
            this._path = path;
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