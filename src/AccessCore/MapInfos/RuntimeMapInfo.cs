using System;
using System.Reflection;
using System.Collections.Generic;
using AccessCore.Helpers;

namespace AccessCore.Repository.MapInfos
{
    /// <summary>
    /// Class for constructing map info in runtime to prevent
    /// work with any map construction files.
    /// </summary>
    public class RuntimeMapInfo : MapInfo
    {
        /// <summary>
        /// Descriptors of operation
        /// </summary>
        private Dictionary<string, OperationRegisterDescriptor> _descriptors;

        /// <summary>
        /// Creates new instance of <see cref="RuntimeMapInfo"/>
        /// </summary>
        public RuntimeMapInfo()
        {
            this._descriptors = new Dictionary<string, OperationRegisterDescriptor>();
        }

        /// <summary>
        /// Register operation.
        /// </summary>
        /// <typeparam name="TInput">Type of input.</typeparam>
        /// <param name="operationName">Operation Name.</param>
        /// <param name="returnDataType">Return Data type.</param>
        /// <param name="storedProcedureName">Stored procedure name.</param>
        /// <returns>object where the given operation is registered.</returns>
        /// <param name="primitiveSpParameterName">Name of stored procedure parameter which is primitive.</param>
        public RuntimeMapInfo RegisterOperation<TInput>(
            string operationName,
            string storedProcedureName = null,
            ReturnDataType returnDataType = ReturnDataType.NoReturnValue,
            string primitiveSpParameterName = null)
        {
            return this.RegisterOperation(
                operationName, 
                typeof(TInput), 
                returnDataType,
                storedProcedureName, 
                primitiveSpParameterName);
        }

        /// <summary>
        /// Registers operation for map info.
        /// </summary>
        /// <param name="operationName">Operation name.</param>
        /// <param name="inputType">Input type.</param>
        /// <param name="returnDataType">Return data type.</param>
        /// <param name="storedProcedureName">Stored Procedure name.</param>
        /// <param name="primitiveSpParameterName">Name of stored procedure parameter which is primitive.</param>
        /// <returns>object where the given operation is registered.</returns>
        public RuntimeMapInfo RegisterOperation(
            string operationName,
            Type inputType,
            ReturnDataType returnDataType = ReturnDataType.NoReturnValue,
            string storedProcedureName = null,
            string primitiveSpParameterName = null)
        {
            if(string.IsNullOrEmpty(operationName))
            {
                throw new ArgumentException("Operation name cannot be empty.");
            }

            if(this._descriptors.ContainsKey(operationName))
            {
                throw new ArgumentException("Operation is already registererd.");
            }
            
            if (TypeHelper.IsPrimitive(inputType) && string.IsNullOrEmpty(primitiveSpParameterName))
            {
                throw new ArgumentException("Input parameter of primitive type must provide name explicitly.");
            }

            if (string.IsNullOrEmpty(storedProcedureName))
            {
                storedProcedureName = "usp" + operationName;
            }

            var descriptor = new OperationRegisterDescriptor
            {
                OperationName = operationName,
                SpName = storedProcedureName,
                InputType = inputType,
                ReturnDataType = returnDataType
            };

            this._descriptors.Add(descriptor.OperationName, descriptor);

            return this;
        }

        /// <summary>
        /// Sets map info
        /// </summary>
        public override void SetMapInfo()
        {
            base.SetMapInfo();

            var descriptor = default(OperationRegisterDescriptor);
            var properties = default(IEnumerable<PropertyInfo>);
            var attribute = default(AcPropertyAttribute);
            var spName = default(string);
            var opNames = new Dictionary<string, string>();
            var parameters = new Dictionary<string, Dictionary<string, string>>();
            var returnValues = new Dictionary<string, ReturnDataType>();

            foreach(var kv in this._descriptors)
            {
                descriptor = kv.Value;
                opNames.Add(descriptor.OperationName, descriptor.SpName);
                returnValues.Add(descriptor.OperationName, descriptor.ReturnDataType);
                
                var input = new Dictionary<string, string>();

                if (TypeHelper.IsPrimitive(descriptor.InputType))
                {
                    input.Add("primitive", descriptor.SpPrimitiveParameterName);
                    parameters.Add(descriptor.OperationName, input);
                }
                else
                {
                    properties = TypeHelper.GetProperties(
                        descriptor.InputType, typeof(AcPropertyAttribute));

                    foreach(var property in properties)
                    {
                        attribute = property.GetCustomAttribute<AcPropertyAttribute>();

                        if (string.IsNullOrEmpty(attribute.SpParameterName))
                        {
                            spName = property.Name;
                        }
                        else
                        {
                            spName = attribute.SpParameterName;
                        }

                        input.Add(property.Name, spName);
                    }
                }

                parameters.Add(descriptor.OperationName, input);
            }

            this.ConstructMapInfo(opNames, returnValues, parameters);
        }
    }
}