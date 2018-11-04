﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using DatabaseAccess.SpExecuters;

namespace DatabaseAccess.Repository
{
    /// <summary>
    /// Class for managing data
    /// </summary>
    public class DataManager
    {
        /// <summary>
        /// Stored procedure executer
        /// </summary>
        private readonly ISpExecuter _spExecuter;

        /// <summary>
        /// Mapping information
        /// </summary>
        private readonly MapInfo _mapInfo;

        /// <summary>
        /// Dictionary of cached parameter getters
        /// </summary>
        private readonly Dictionary<Type, Delegate> _cachedParameterGetters;

        /// <summary>
        /// Creates new instance of <see cref="DataManager"/>
        /// </summary>
        /// <param name="spExecuter">Stored procedure exucuter</param>
        /// <param name="mapInfo">Mapping information</param>
        public DataManager(SpExecuter spExecuter, MapInfo mapInfo)
        {
            // setting fields
            this._spExecuter = spExecuter;
            this._mapInfo = mapInfo;

            // initializing
            this._cachedParameterGetters = new Dictionary<Type, Delegate>();
        }

        /// <summary>
        /// Operates the action.
        /// </summary>
        /// <typeparam name="TResult">Type of resuly</typeparam>
        /// <param name="operationName">Operation mapped name.</param>
        /// <param name="parameters">Parameters</param>
        /// <returns>Result</returns>
        public object Operate<TResult>(string operationName, IEnumerable<KeyValuePair<string, object>> parameters = null)
            where TResult : class
        {
            // getting operation info
            var operationInfo = this.GetOperationInfo(operationName);

            // getting parameters
            var spParams = null as IEnumerable<KeyValuePair<string, object>>;

            // constructing parameters
            if (parameters != null)
                spParams = this.ConstructParameters(operationInfo.ParametersMappInfo, parameters).ToList();
            else spParams = parameters;

            // executing specific operation
            if (operationInfo.ReturnDataType == ReturnDataType.Entity)
                return this._spExecuter.ExecuteEntitySp<TResult>(operationInfo.SpName, spParams);
            else if (operationInfo.ReturnDataType == ReturnDataType.Enumerable)
                return this._spExecuter.ExecuteSp<TResult>(operationInfo.SpName, spParams);
            else if (operationInfo.ReturnDataType == ReturnDataType.Scalar)
                return this._spExecuter.ExecuteScalarSp<object>(operationInfo.SpName, spParams);
            else
                return this._spExecuter.ExecuteSpNonQuery(operationInfo.SpName, spParams);
        }

        /// <summary>
        /// Operates the action asynchronosuly
        /// </summary>
        /// <typeparam name="TResult">Type of result</typeparam>
        /// <param name="operationName">Operation name</param>
        /// <param name="parameters">Parameters</param>
        /// <returns>Result</returns>
        public Task<object> OperateAsync<TResult>(string operationName, IEnumerable<KeyValuePair<string, object>> parameters = null)
            where TResult : class
        {
            // creating task
            var task = new Task<object>(() => this.Operate<TResult>(operationName, parameters));

            // starting task
            task.Start();

            // returning task
            return task;
        }

        /// <summary>
        /// Operates the action
        /// </summary>
        /// <typeparam name="TParamater">Type of complex parameter.</typeparam>
        /// <typeparam name="TResult">Type of result</typeparam>
        /// <param name="operationName">Operation name</param>
        /// <param name="paramater">Parameters</param>
        /// <returns>Result</returns>
        public object Operate<TParamater,TResult>(string operationName,TParamater paramater)
            where TResult:class
        {
            // getting parameters using reflection
            var parameters = this.GetParameters(paramater);

            // returning result
            return this.Operate<TResult>(operationName, parameters);
        }

        /// <summary>
        /// Operates the action asynchronously
        /// </summary>
        /// <typeparam name="TParameter">Type of parameter</typeparam>
        /// <typeparam name="TResult">Type of result</typeparam>
        /// <param name="operationName">Operation name</param>
        /// <param name="parameter">Parameter</param>
        /// <returns>result</returns>
        public Task<object> OperateAsync<TParameter,TResult>(string operationName,TParameter parameter)
            where TResult:class
        {
            // creating task
            var task = new Task<object>(() => this.Operate<TParameter,TResult>(operationName, parameter));

            // starting task
            task.Start();

            // returning task
            return task;
        }

        /// <summary>
        /// Operates the action
        /// </summary>
        /// <typeparam name="TFirstParameter">Type of first parameter</typeparam>
        /// <typeparam name="TSecondParameter">Type of second parameter</typeparam>
        /// <typeparam name="TResult">Type of result</typeparam>
        /// <param name="operationName">Operation name</param>
        /// <param name="firstParameter">First parameter</param>
        /// <param name="secondParameter">Second parameter</param>
        /// <returns>result</returns>
        public object Operate<TFirstParameter,TSecondParameter,TResult>(string operationName,TFirstParameter firstParameter,TSecondParameter secondParameter)
            where TResult:class
        {
            // getting parameters
            var parameters = this.GetParameters(firstParameter).ToList();

            // adding parameter
            parameters.AddRange(this.GetParameters(secondParameter));

            // returning result
            return this.Operate<TResult>(operationName, parameters);
        }

        /// <summary>
        /// Gets operation information
        /// </summary>
        /// <param name="operationName">Operation name</param>
        /// <returns>operation information</returns>
        private OperationInfo GetOperationInfo(string operationName)
        {
            // constructing and returning operation information
            return new OperationInfo
            {
                Name = operationName,
                SpName = this._mapInfo.OpNames[operationName],
                ReturnDataType = this._mapInfo.ReturnValues[operationName],
                ParametersMappInfo = this._mapInfo.Parameters[operationName]
            };
        }

        /// <summary>
        /// Gets parameters using mapping information
        /// </summary>
        /// <param name="mapInfo">Mapping information</param>
        /// <param name="parameters">Parameters</param>
        /// <returns>result</returns>
        private IEnumerable<KeyValuePair<string, object>> ConstructParameters(
            Dictionary<string, string> mapInfo, IEnumerable<KeyValuePair<string, object>> parameters)
        {
            // returning parameters
            return parameters.Select(kv =>
                    new KeyValuePair<string, object>(mapInfo[kv.Key], kv.Value));
        }

        /// <summary>
        /// Gets properties as operation parameters from complex parameter using reflection
        /// </summary>
        /// <typeparam name="TParameter">Type of parameter</typeparam>
        /// <param name="entity">entity</param>
        /// <returns>parameters</returns>
        private IEnumerable<KeyValuePair<string, object>> GetParameters<TParameter>(TParameter parameter)
        {
            // getting type of complex parameter
            var type = parameter.GetType();

            // checking if parameter has primitive type
            // note that for DataManager primitive types are not only .NET primitive types but also string and decimal
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime))
            {
                return new[]
                {
                    new KeyValuePair<string,object>("primitive",parameter)
                };
            }

            // getting getter
            var getter = this.GetParamaterGetter<TParameter>();

            // executing getter
            return getter(parameter);
        }

        /// <summary>
        /// Gets parameter getter if it exists in cached getters otherwise creates new one
        /// </summary>
        /// <typeparam name="TParameter">Type of parameter.</typeparam>
        /// <returns>getter</returns>
        private Func<TParameter,List<KeyValuePair<string,object>>> GetParamaterGetter<TParameter>()
        {
            // getting type of parameter
            var type = typeof(TParameter);

            // returning cached getter if it exists
            if (this._cachedParameterGetters.ContainsKey(type))
                return (Func<TParameter,List<KeyValuePair<string,object>>>)this._cachedParameterGetters[type];

            // gettoing type of list
            var listType = typeof(List<KeyValuePair<string, object>>);

            // getting type of KeyValuPair
            var kvpType = typeof(KeyValuePair<string, object>);

            // getting KeyValuePair constructor information
            var kvpCtor = kvpType.GetConstructor(new[] { typeof(string), typeof(object) });

            // getting List Add method information
            var addInfo = listType.GetMethod("Add");

            // getting properties
            var properties = type.GetProperties();

            // constructing KeyValuePair variable expression
            var kvp = Expression.Parameter(kvpType);

            // constructing list expression
            var list = Expression.Parameter(listType);

            // constructing list initializing expression
            var listInit = Expression.New(listType);

            // assign expression for assigning initialized list to list variable
            var listAssign = Expression.Assign(list, listInit);

            // constructing source expression
            var sourceExpr = Expression.Parameter(type);

            // creating list of expression for lambda expression body
            var expressions = new List<Expression>();

            // constructing block parameters
            var variables = new[] { kvp, list };

            // adding list assignment
            expressions.Add(listAssign);
            
            // loop over the properties and add key value pair to list expressions
            foreach(var property in properties)
            {
                var propValue = Expression.Property(sourceExpr, property.Name);

                var value = Expression.Convert(propValue, typeof(object));

                var name = Expression.Constant(property.Name);

                var init = Expression.New(kvpCtor, name, value);

                var assign = Expression.Assign(kvp, init);

                var add = Expression.Call(list, addInfo,kvp);

                expressions.Add(assign);
                expressions.Add(add);
            }

            // constructing label target
            var target = Expression.Label(listType);

            // constructing return expression
            var returnExpr = Expression.Return(target, list, listType);

            // constructing label expression
            var label = Expression.Label(target, Expression.Default(listType));

            // adding return expression and label to expressions list
            expressions.Add(returnExpr);
            expressions.Add(label);

            // constructing block of expressions
            var block = Expression.Block(variables, expressions);

            // creating lambda from expression body
            var lambda = Expression.Lambda<Func<TParameter, List<KeyValuePair<string, object>>>>(block, sourceExpr);

            // compiling lambda
            var getter = lambda.Compile();

            // adding compiled getter to cached getters
            this._cachedParameterGetters.Add(type, getter);

            // returning getter
            return getter;
        }
    }
}