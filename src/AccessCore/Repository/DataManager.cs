using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AccessCore.SpExecuters;
using AccessCore.Repository.MapInfos;
using AccessCore.Helpers;

namespace AccessCore.Repository
{
    /// <summary>
    /// Class for managing data
    /// </summary>
    public class DataManager
    {
        #region fields

        /// <summary>
        /// Stored procedure executer
        /// </summary>
        private readonly ISpExecuter _spExecuter;

        /// <summary>
        /// Mapping information
        /// </summary>
        private readonly MapInfo _mapInfo;

        /// <summary>
        /// Cached parameter getter
        /// </summary>
        private readonly ConcurrentDictionary<Type, Delegate> _cachedParameterGetters;

        #endregion

        #region constructors

        /// <summary>
        /// Creates new instance of <see cref="DataManager"/>
        /// </summary>
        /// <param name="spExecuter">Stored procedures</param>
        /// <param name="mapInfo">Mapping information</param>
        public DataManager(ISpExecuter spExecuter, MapInfo mapInfo)
        {
            // setting fields
            this._spExecuter = spExecuter;
            this._mapInfo = mapInfo;

            // initializing
            this._cachedParameterGetters = new ConcurrentDictionary<Type, Delegate>();
        }

        #endregion

        #region public API

        /// <summary>
        /// Operates the database action asynchronously.
        /// </summary>
        /// <typeparam name="TResult">Type of result.</typeparam>
        /// <param name="operationName">Operation name.</param>
        /// <param name="parameters">parameters</param>
        /// <returns>result</returns>
        public async Task<object> OperateAsync<TResult>(
            string operationName, 
            IEnumerable<KeyValuePair<string, object>> parameters = null)
            where TResult : class
        {
            // getting operation info
            var operationInfo = this.GetOperationInfo(operationName);

            // getting parameters
            var spParams = default(IEnumerable<KeyValuePair<string, object>>);

            // constructing parameters
            if (parameters != null)
                spParams = this.ConstructParameters(operationInfo.ParametersMappInfo, parameters).ToList();
            else spParams = parameters;

            // executing specific operation
            if (operationInfo.ReturnDataType == ReturnDataType.Entity)
                return await this._spExecuter.ExecuteEntitySpAsync<TResult>(operationInfo.SpName, spParams);

            if (operationInfo.ReturnDataType == ReturnDataType.Enumerable)
                return await this._spExecuter.ExecuteSpAsync<TResult>(operationInfo.SpName, spParams);

            if (operationInfo.ReturnDataType == ReturnDataType.Scalar)
                return await this._spExecuter.ExecuteScalarSpAsync<TResult>(operationInfo.SpName, spParams);

            return await this._spExecuter.ExecuteSpNonQueryAsync(operationInfo.SpName, spParams);
        }

        /// <summary>
        /// Operates the database action with the given entity parameter.
        /// </summary>
        /// <typeparam name="TParamater">Type of parameter</typeparam>
        /// <typeparam name="TResult">Type of result</typeparam>
        /// <param name="operationName">operation name</param>
        /// <param name="parameter">parameter</param>
        /// <returns>result</returns>
        public async Task<object> OperateAsync<TParamater,TResult>(string operationName,TParamater parameter)
            where TResult:class
        {
            // executing
            return await this.OperateAsync<TResult>(
                operationName,
                this.GetParameters(parameter, operationName));
        }

        /// <summary>
        /// Operates the database action with the given first and second entity parameters.
        /// </summary>
        /// <typeparam name="TFirstParameter">Type of first parameter.</typeparam>
        /// <typeparam name="TSecondParameter">Type of second parameter.</typeparam>
        /// <typeparam name="TResult">Type of result.</typeparam>
        /// <param name="operationName">Operation name.</param>
        /// <param name="firstParameter">first parameter</param>
        /// <param name="secondParameter">second parameter</param>
        /// <returns>result of db action</returns>
        public async Task<object> OperateAsync<TFirstParameter,TSecondParameter,TResult>(
            string operationName,
            TFirstParameter firstParameter,
            TSecondParameter secondParameter)
            where TResult:class
        {
            var firstParameters = this.GetParameters(firstParameter, operationName);
            var secondParameters = this.GetParameters(secondParameter, operationName);

            return await this.OperateAsync<TResult>(
                operationName,
                firstParameters.Concat(secondParameters));
        }

        #endregion

        #region private helper methods

        /// <summary>
        /// Gets operation info with operation name.
        /// </summary>
        /// <param name="operationName">operation name</param>
        /// <returns>operation info</returns>
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
        /// Constructs parameters.
        /// </summary>
        /// <param name="mapInfo">mapping informtaion</param>
        /// <param name="parameters">parameter values</param>
        /// <returns>mapped pairs of parameters and values</returns>
        private IEnumerable<KeyValuePair<string, object>> ConstructParameters(
            Dictionary<string, string> mapInfo, IEnumerable<KeyValuePair<string, object>> parameters)
        {
            // returning parameters
            return parameters.Select(kv =>
                    new KeyValuePair<string, object>(mapInfo[kv.Key], kv.Value));
        }

        /// <summary>
        /// Gets parameters from entity.
        /// </summary>
        /// <typeparam name="TParameter">Type of parameter.</typeparam>
        /// <param name="parameter">parameter</param>
        /// <param name="opName">operation name</param>
        /// <returns>pairs of parameters and their values</returns>
        private IEnumerable<KeyValuePair<string, object>> GetParameters<TParameter>(
            TParameter parameter,
            string opName)
        {
            // getting type of complex parameter
            var type = parameter.GetType();

            // checking if parameter has primitive type
            // note that for DataManager primitive types are not only .NET primitive types but also string and decimal
            if (TypeHelper.IsPrimitive(type))
            {
                return new[]
                {
                    new KeyValuePair<string,object>("primitive",parameter)
                };
            }

            // getting getter
            var getter = this.GetParamaterGetter<TParameter>(opName);

            // executing getter
            return getter(parameter);
        }

        /// <summary>
        /// Gets parameter getter.
        /// </summary>
        /// <typeparam name="TParameter">Type of parameter</typeparam>
        /// <param name="opName">operation name</param>
        /// <returns>getter</returns>
        private Func<TParameter,List<KeyValuePair<string,object>>> GetParamaterGetter<TParameter>(string opName)
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

            var mapInfo = this._mapInfo.Parameters[opName];
            var properties = type.GetProperties()
                .Where(property => mapInfo.ContainsKey(property.Name));

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

            var propValue = default(MemberExpression);
            var value = default(UnaryExpression);
            var name = default(ConstantExpression);
            var init = default(NewExpression);
            var assign = default(BinaryExpression);
            var add = default(MethodCallExpression);

            // loop over the properties and add key value pair to list expressions
            foreach(var property in properties)
            {
                propValue   = Expression.Property(sourceExpr, property.Name);
                value       = Expression.Convert(propValue, typeof(object));
                name        = Expression.Constant(property.Name);
                init        = Expression.New(kvpCtor, name, value);
                assign      = Expression.Assign(kvp, init);
                add         = Expression.Call(list, addInfo,kvp);

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
            if (!this._cachedParameterGetters.TryAdd(type, getter))
                throw new Exception("CachedParameterGetters");

            // returning getter
            return getter;
        }

        #endregion
    }
}