// <copyright file="QueryCMISService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>


/*
   var resFilenet = this.filenetService.FindFilenetBy<OPGE>(await this.CreateQueryConfiguration(dto));

   
     /// <summary>
     /// Crea una configuración de consulta con los valores predeterminados del método GetDocument original.
     /// </summary>
     /// <returns>Configuración de consulta con los filtros y parámetros originales.</returns>
     private async Task<QueryConfiguration> CreateQueryConfiguration(DocumentalFlujoTransaccionBusqueda_DTO dto)
     {
         var config = await this.GetConfiguracion(dto);
         var dtoPropiedades = Helper.PropiedadesConValores(dto);
         var filters = Helper.BuildFilters(config, dtoPropiedades, dto);
         var sortDirection = !string.IsNullOrEmpty(dto.SortDirection) && dto.SortDirection == "ASC";

         return new QueryConfiguration
         {
             Page = dto.PageNumber,
             PageSize = dto.PageSize,
             SelectFields = config.ColumnaExtendida.Select(s => s.nombre).Concat(new[] { "Id" }).ToArray(),
             Filters = filters,
             OrderByFields = new List<QueryOrderBy>
             {
                 new QueryOrderBy(string.IsNullOrEmpty(dto.SortColumn) ? "FechaInstruccion" : dto.SortColumn, sortDirection),
             },
         };
     }

     
 /// <summary>
 /// Busca documentos en Filenet utilizando una configuración de consulta que incluye criterios y parámetros de paginación.
 /// Este método es genérico y puede trabajar con cualquier clase que tenga un constructor sin parámetros.
 /// </summary>
 /// <param name="queryConfig">Objeto que contiene la configuración de la consulta, incluyendo filtros, número de página y tamaño de página.</param>
 /// <typeparam name="T">El tipo de datos contenidos en el conjunto de resultados.</typeparam>
 /// <returns>Un objeto Filenet_Map con el código de estado y los datos encontrados (si existen).</returns>
 public Filenet_Map FindFilenetBy<T>(QueryConfiguration queryConfig)
      where T : class, new()
 {
     try
     {
         this.ValidateConnection();

         this.filenetCMISService.Connection(this.connectionFilenet);
         var result = this.GetDocument<T>(queryConfig.PageSize, queryConfig.Page, queryConfig);

         if (result != null)
         {
             return new Filenet_Map
             {
                 Codigo = "200",
                 Datos = result,
             };
         }

         return new Filenet_Map
         {
             Codigo = "404",
         };
     }
     catch (Exception ex)
     {
         throw new Exception($"Error FindFilenetBy {ex.Message}", ex);
     }
 }


   /// <summary>
  /// Ejecuta una consulta CMIS dinámica utilizando la configuración proporcionada.
  /// </summary>
  /// <typeparam name="T">Tipo de objeto a retornar.</typeparam>
  /// <param name="pageSize">Tamaño de página para la paginación.</param>
  /// <param name="page">Número de página a consultar.</param>
  /// <param name="queryConfig">Configuración de la consulta con filtros, ordenamiento y campos.</param>
  /// <returns>Resultado paginado de la consulta.</returns>
  private PagedResult<T> GetDocument<T>(int pageSize, int page, QueryConfiguration queryConfig)
      where T : class, new()
  {
      var query = new QueryCMISService<T>(this.filenetCMISService.CreateCmisSession());

      // Aplicar cláusula Select
      if (queryConfig.SelectFields != null && queryConfig.SelectFields.Length > 0)
      {
          query = query.Select(queryConfig.SelectFields);
      }

      // Aplicar cláusulas Where
      if (queryConfig.Filters != null && queryConfig.Filters.Count > 0)
      {
          foreach (var filter in queryConfig.Filters)
          {
              query = this.ApplyWhereClause(query, filter);
          }
      }

      // Aplicar cláusulas OrderBy
      if (queryConfig.OrderByFields != null && queryConfig.OrderByFields.Count > 0)
      {
          foreach (var orderBy in queryConfig.OrderByFields)
          {
              query = query.OrderBy(orderBy.PropertyName, orderBy.Descending);
          }
      }

      return query
          .PageSize(pageSize)
          .Page(page)
          .ExecutePagedQuery();
  }

  /// <summary>
  /// Aplica una cláusula Where específica a la consulta CMIS basada en el filtro proporcionado.
  /// </summary>
  /// <typeparam name="T">Tipo de objeto de la consulta.</typeparam>
  /// <param name="query">La consulta CMIS a la cual aplicar el filtro.</param>
  /// <param name="filter">El filtro a aplicar.</param>
  /// <returns>La consulta con el filtro aplicado.</returns>
  private QueryCMISService<T> ApplyWhereClause<T>(QueryCMISService<T> query, QueryFilter filter)
      where T : class, new()
  {
      switch (filter.Operator.ToUpper())
      {
          case "= TIMESTAMP":
          case ">= TIMESTAMP":
          case "<= TIMESTAMP":
          case "> TIMESTAMP":
          case "< TIMESTAMP":
              return query.Where(filter.PropertyName, filter.Value.ToString(), filter.Operator);

          case "=":
          case "<>":
          case ">=":
          case "<=":
          case ">":
          case "<":
          case "LIKE":
              return query.Where(filter.PropertyName, filter.Value.ToString(), filter.Operator);

          default:
              // Por defecto, se aplica igualdad si el operador no es reconocido
              return query.Where(filter.PropertyName, filter.Value.ToString());
      }
  }



*/

namespace Core.Cmis.Documento
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using DotCMIS.Client;
    using DotCMIS.Data;

    /// <summary>
    /// Generic query service for CMIS (Content Management Interoperability Services) operations.
    /// Provides a fluent interface for building and executing CMIS queries with type-safe results.
    /// </summary>
    /// <typeparam name="T">The type to which query results will be mapped. Must be a reference type with a parameterless constructor.</typeparam>
    public class QueryCMISService<T>
        where T : class, new()
    {
        /// <summary>
        /// The CMIS session instance used for executing queries.
        /// </summary>
        private readonly ISession session;

        /// <summary>
        /// Collection of query conditions (WHERE clauses) to be applied to the CMIS query.
        /// </summary>
        private readonly List<QueryCondition> conditions = new List<QueryCondition>();

        /// <summary>
        /// Collection of properties to be selected in the CMIS query (SELECT clause).
        /// </summary>
        private readonly List<string> selectedProperties = new List<string>();

        /// <summary>
        /// Collection of ORDER BY clauses for sorting results.
        /// </summary>
        private readonly List<string> orderByClause = new List<string>();

        /// <summary>
        /// The page size for pagination. Default is 50 records per page.
        /// </summary>
        private int pageSize = 100;

        /// <summary>
        /// The current page number (0-based). Default is 0.
        /// </summary>
        private int pageNumber = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryCMISService{T}"/> class.
        /// </summary>
        /// <param name="session">The CMIS session to use for query execution.</param>
        /// <exception cref="ArgumentNullException">Thrown when session is null.</exception>
        public QueryCMISService(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>
        /// Specifies the properties to be selected in the CMIS query.
        /// If not called, all properties (*) will be selected.
        /// </summary>
        /// <param name="properties">Array of property names to include in the SELECT clause.</param>
        /// <returns>The current instance of <see cref="QueryCMISService{T}"/> for method chaining.</returns>
        public QueryCMISService<T> Select(params string[] properties)
        {
            this.selectedProperties.AddRange(properties);
            return this;
        }

        /// <summary>
        /// Adds a condition to the WHERE clause of the CMIS query.
        /// </summary>
        /// <param name="property">The name of the property to filter on.</param>
        /// <param name="value">The value to compare against.</param>
        /// <param name="operator">The comparison operator to use (default is "=").</param>
        /// <returns>The current instance of <see cref="QueryCMISService{T}"/> for method chaining.</returns>
        public QueryCMISService<T> Where(string property, object value, string @operator = "=")
        {
            this.conditions.Add(new QueryCondition(property, value, @operator));
            return this;
        }

        /// <summary>
        /// Adds a condition to filter for properties that are not null.
        /// Equivalent to calling Where(property, null, "IS NOT").
        /// </summary>
        /// <param name="property">The name of the property to check for non-null values.</param>
        /// <returns>The current instance of <see cref="QueryCMISService{T}"/> for method chaining.</returns>
        public QueryCMISService<T> WhereNotNull(string property)
        {
            return this.Where(property, null, "IS NOT");
        }

        /// <summary>
        /// Adds a condition to filter for properties that are null.
        /// Equivalent to calling Where(property, null, "IS").
        /// </summary>
        /// <param name="property">The name of the property to check for null values.</param>
        /// <returns>The current instance of <see cref="QueryCMISService{T}"/> for method chaining.</returns>
        public QueryCMISService<T> WhereNull(string property)
        {
            return this.Where(property, null, "IS");
        }

        /// <summary>
        /// Sets the page size for pagination.
        /// </summary>
        /// <param name="size">The number of records per page. Must be greater than 0.</param>
        /// <returns>The current instance of <see cref="QueryCMISService{T}"/> for method chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when size is less than or equal to 0.</exception>
        public QueryCMISService<T> PageSize(int size)
        {
            if (size <= 0)
            {
                throw new ArgumentException("Page size must be greater than 0", nameof(size));
            }

            this.pageSize = size;
            return this;
        }

        /// <summary>
        /// Sets the page number for pagination (0-based).
        /// </summary>
        /// <param name="page">The page number to retrieve. Must be greater than or equal to 0.</param>
        /// <returns>The current instance of <see cref="QueryCMISService{T}"/> for method chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when page is less than 0.</exception>
        public QueryCMISService<T> Page(int page)
        {
            if (page < 0)
            {
                throw new ArgumentException("Page number must be greater than or equal to 0", nameof(page));
            }

            this.pageNumber = page;
            return this;
        }

        /// <summary>
        /// Adds an ORDER BY clause to sort the results.
        /// </summary>
        /// <param name="property">The property name to sort by.</param>
        /// <param name="ascending">True for ascending order, false for descending. Default is true.</param>
        /// <returns>The current instance of <see cref="QueryCMISService{T}"/> for method chaining.</returns>
        public QueryCMISService<T> OrderBy(string property, bool ascending = true)
        {
            string direction = ascending ? "ASC" : "DESC";
            this.orderByClause.Add($"{property} {direction}");
            return this;
        }

        /// <summary>
        /// Executes the query using FileNet's native pagination capabilities when available.
        /// This is the most efficient method for large datasets as it leverages repository-level pagination.
        /// Falls back to client-side pagination if native pagination is not supported.
        /// </summary>
        /// <returns>
        /// A <see cref="PagedResult{T}"/> containing the data for the current page with optimal performance.
        /// </returns>
        public PagedResult<T> ExecutePagedQuery()
        {
            string objectType = typeof(T).Name;

            try
            {
                // Build the appropriate query based on pagination requirements
                string query = this.BuildQuery(objectType);

                // Create optimized operation context without MaxItemsPerPage to avoid conflicts
                IOperationContext operationContext = this.session.CreateOperationContext();
                operationContext.MaxItemsPerPage = this.pageSize;
                operationContext.CacheEnabled = true; // Enable caching for better performance

                // Set specific properties to reduce data transfer
                if (this.selectedProperties.Any())
                {
                    operationContext.FilterString = string.Join(",", this.selectedProperties);
                }

                // Execute the query without pagination limits
                var queryResults = this.session.Query(query, true, operationContext);

                // Convert to list to ensure full enumeration
                var allResults = queryResults.ToList();

                // Handle pagination based on page number
                int skipCount = (this.pageNumber - 1) * this.pageSize;

                // Apply pagination on the materialized list
                List<IQueryResult> pagedResults = allResults.Skip(skipCount).Take(this.pageSize).ToList();

                // Map results efficiently
                var mappedResults = new List<T>(pagedResults.Count);
                foreach (var result in pagedResults)
                {
                    var mappedResult = this.MapResult(result);
                    if (mappedResult != null)
                    {
                        mappedResults.Add(mappedResult);
                    }
                }

                // Use the count from materialized results for better performance
                int totalCount = allResults.Count;

                return new PagedResult<T>
                {
                    Data = mappedResults,
                    PageNumber = this.pageNumber,
                    PageSize = this.pageSize,
                    TotalCount = totalCount,
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Maps a single CMIS query result to an instance of type T using reflection.
        /// Matches CMIS properties to object properties by name (case-insensitive).
        /// Supports both single values and collections.
        /// </summary>
        /// <param name="result">The CMIS query result to map.</param>
        /// <returns>An instance of type T with properties populated from the query result.</returns>
        private T MapResult(IQueryResult result)
        {
            var properties = typeof(T).GetProperties().Where(p => p.CanWrite);
            var obj = Activator.CreateInstance<T>();

            foreach (var prop in properties)
            {
                var cmisProperty = this.FindMatchingCmisProperty(result, prop);

                if (cmisProperty == null)
                {
                    continue;
                }

                this.MapPropertyValue(obj, prop, cmisProperty);
            }

            return obj;
        }

        /// <summary>
        /// Finds the matching CMIS property for a given object property.
        /// </summary>
        /// <param name="result">The CMIS query result.</param>
        /// <param name="property">The target object property.</param>
        /// <returns>The matching CMIS property or null if not found.</returns>
        private IPropertyData FindMatchingCmisProperty(IQueryResult result, PropertyInfo property)
        {
            var nameComparers = new Func<IPropertyData, bool>[]
            {
                p => string.Equals(p.LocalName, property.Name, StringComparison.OrdinalIgnoreCase),
                p => string.Equals(p.QueryName, property.Name, StringComparison.OrdinalIgnoreCase),
                p => string.Equals(p.DisplayName, property.Name, StringComparison.OrdinalIgnoreCase),
            };

            return result.Properties.FirstOrDefault(prop => nameComparers.Any(comparer => comparer(prop)));
        }

        /// <summary>
        /// Maps a CMIS property value to the target object property.
        /// </summary>
        /// <param name="targetObject">The target object.</param>
        /// <param name="property">The target property.</param>
        /// <param name="cmisProperty">The CMIS property.</param>
        private void MapPropertyValue(object targetObject, PropertyInfo property, IPropertyData cmisProperty)
        {
            try
            {
                var mappers = new List<PropertyMapper>
                {
                    new PropertyMapper(() => this.HasMultipleValues(cmisProperty) && this.IsCollectionType(property.PropertyType), () => this.MapCollectionProperty(targetObject, property, cmisProperty)),
                    new PropertyMapper(() => this.HasMultipleValues(cmisProperty) && !this.IsCollectionType(property.PropertyType), () => this.MapMultiValueToSingleProperty(targetObject, property, cmisProperty)),
                    new PropertyMapper(() => !this.HasMultipleValues(cmisProperty) && cmisProperty.FirstValue != null, () => this.MapSingleValue(targetObject, property, cmisProperty)),
                };

                var applicableMapper = mappers.FirstOrDefault(mapper => mapper.Condition());
                applicableMapper?.Action();
            }
            catch (Exception)
            {
                // Ignore conversion errors and continue with other properties
            }
        }

        /// <summary>
        /// Maps a single CMIS value to the target property.
        /// </summary>
        /// <param name="targetObject">The target object.</param>
        /// <param name="property">The target property.</param>
        /// <param name="cmisProperty">The CMIS property.</param>
        private void MapSingleValue(object targetObject, PropertyInfo property, IPropertyData cmisProperty)
        {
            var value = Convert.ChangeType(cmisProperty.FirstValue, property.PropertyType);
            property.SetValue(targetObject, value);
        }

        /// <summary>
        /// Determines if a CMIS property has multiple values by checking the Values collection.
        /// </summary>
        /// <param name="cmisProperty">The CMIS property to check.</param>
        /// <returns>True if the property has multiple values, false otherwise.</returns>
        private bool HasMultipleValues(IPropertyData cmisProperty)
        {
            return cmisProperty.Values != null && cmisProperty.Values.Count > 1;
        }

        /// <summary>
        /// Maps CMIS property with multiple values to a single property.
        /// Uses the first value or concatenates values with a separator.
        /// </summary>
        /// <param name="targetObject">The target object to set the property on.</param>
        /// <param name="property">The property information for the single property.</param>
        /// <param name="cmisProperty">The CMIS property containing multiple values.</param>
        private void MapMultiValueToSingleProperty(object targetObject, PropertyInfo property, IPropertyData cmisProperty)
        {
            var values = cmisProperty.Values?.Where(v => v != null).ToList();
            if (values == null || !values.Any())
            {
                return;
            }

            var valueMappers = new Dictionary<Type, Func<object>>
            {
                { typeof(string), () => string.Join("; ", values.Select(v => v.ToString())) },
                { typeof(object), () => this.ConvertFirstValue(values.First(), property.PropertyType) },
            };

            var mapperKey = property.PropertyType == typeof(string) ? typeof(string) : typeof(object);
            var convertedValue = valueMappers[mapperKey]();

            if (convertedValue != null)
            {
                property.SetValue(targetObject, convertedValue);
            }
        }

        /// <summary>
        /// Converts the first available value with fallback handling.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The target type.</param>
        /// <returns>The converted value or null.</returns>
        private object ConvertFirstValue(object value, Type targetType)
        {
            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Determines if a property type is a collection type (List, Array, IEnumerable, etc.).
        /// </summary>
        /// <param name="propertyType">The property type to check.</param>
        /// <returns>True if the property type is a collection type, false otherwise.</returns>
        private bool IsCollectionType(Type propertyType)
        {
            var collectionCheckers = new Func<bool>[]
            {
                () => propertyType.IsArray,
                () => this.IsGenericCollectionType(propertyType),
                () => this.IsNonGenericEnumerableType(propertyType),
            };

            return collectionCheckers.Any(checker => checker());
        }

        /// <summary>
        /// Checks if the type is a generic collection type.
        /// </summary>
        /// <param name="propertyType">The property type to check.</param>
        /// <returns>True if it's a generic collection type.</returns>
        private bool IsGenericCollectionType(Type propertyType)
        {
            if (!propertyType.IsGenericType)
            {
                return false;
            }

            var supportedGenericTypes = new[]
            {
                typeof(List<>),
                typeof(IList<>),
                typeof(IEnumerable<>),
                typeof(ICollection<>),
            };

            var genericTypeDefinition = propertyType.GetGenericTypeDefinition();
            return supportedGenericTypes.Contains(genericTypeDefinition);
        }

        /// <summary>
        /// Checks if the type is a non-generic enumerable type (excluding string).
        /// </summary>
        /// <param name="propertyType">The property type to check.</param>
        /// <returns>True if it's a non-generic enumerable type.</returns>
        private bool IsNonGenericEnumerableType(Type propertyType)
        {
            return typeof(System.Collections.IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(string);
        }

        /// <summary>
        /// Maps CMIS property values to a collection property on the target object.
        /// Handles conversion of multiple values from CMIS to the appropriate collection type.
        /// </summary>
        /// <param name="targetObject">The target object to set the property on.</param>
        /// <param name="property">The property information for the collection property.</param>
        /// <param name="cmisProperty">The CMIS property containing the values to map.</param>
        private void MapCollectionProperty(object targetObject, PropertyInfo property, IPropertyData cmisProperty)
        {
            var values = cmisProperty.Values?.Where(v => v != null).ToList();
            if (values == null || !values.Any())
            {
                return;
            }

            var elementType = this.GetCollectionElementType(property.PropertyType);
            if (elementType == null)
            {
                return;
            }

            var convertedValues = this.ConvertValuesToElementType(values, elementType);
            var collection = this.CreateCollectionInstance(property.PropertyType, elementType, convertedValues);

            if (collection != null)
            {
                property.SetValue(targetObject, collection);
            }
        }

        /// <summary>
        /// Gets the element type of a collection property.
        /// </summary>
        /// <param name="propertyType">The collection property type.</param>
        /// <returns>The element type or null if cannot be determined.</returns>
        private Type GetCollectionElementType(Type propertyType)
        {
            var elementTypeGetters = new Dictionary<Func<bool>, Func<Type>>
            {
                { () => propertyType.IsArray, () => propertyType.GetElementType() },
                { () => propertyType.IsGenericType, () => propertyType.GetGenericArguments()[0] },
                { () => this.IsNonGenericCollection(propertyType), () => typeof(object) },
            };

            var applicableGetter = elementTypeGetters.FirstOrDefault(kvp => kvp.Key());
            return applicableGetter.Value?.Invoke();
        }

        /// <summary>
        /// Checks if the type is a non-generic collection.
        /// </summary>
        /// <param name="propertyType">The property type to check.</param>
        /// <returns>True if it's a non-generic collection.</returns>
        private bool IsNonGenericCollection(Type propertyType)
        {
            var nonGenericCollectionTypes = new[]
            {
                typeof(System.Collections.IEnumerable),
                typeof(System.Collections.ArrayList),
            };

            return nonGenericCollectionTypes.Any(t => t == propertyType);
        }

        /// <summary>
        /// Converts values to the specified element type.
        /// </summary>
        /// <param name="values">The values to convert.</param>
        /// <param name="elementType">The target element type.</param>
        /// <returns>List of converted values.</returns>
        private List<object> ConvertValuesToElementType(List<object> values, Type elementType)
        {
            var convertedValues = new List<object>();

            foreach (var value in values)
            {
                var convertedValue = this.TryConvertValue(value, elementType);
                if (convertedValue != null)
                {
                    convertedValues.Add(convertedValue);
                }
            }

            return convertedValues;
        }

        /// <summary>
        /// Attempts to convert a value to the specified type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="elementType">The target type.</param>
        /// <returns>The converted value or null if conversion fails.</returns>
        private object TryConvertValue(object value, Type elementType)
        {
            try
            {
                return Convert.ChangeType(value, elementType);
            }
            catch (Exception)
            {
                return elementType == typeof(object) ? value : null;
            }
        }

        /// <summary>
        /// Creates a collection instance of the appropriate type.
        /// </summary>
        /// <param name="propertyType">The property type.</param>
        /// <param name="elementType">The element type.</param>
        /// <param name="convertedValues">The values to add to the collection.</param>
        /// <returns>The created collection instance.</returns>
        private object CreateCollectionInstance(Type propertyType, Type elementType, List<object> convertedValues)
        {
            var collectionCreators = new Dictionary<Func<bool>, Func<object>>
            {
                { () => propertyType.IsArray, () => this.CreateArray(elementType, convertedValues) },
                { () => this.IsGenericCollectionType(propertyType), () => this.CreateGenericList(elementType, convertedValues) },
                { () => propertyType == typeof(System.Collections.ArrayList), () => this.CreateArrayList(convertedValues) },
            };

            var applicableCreator = collectionCreators.FirstOrDefault(kvp => kvp.Key());
            return applicableCreator.Value?.Invoke();
        }

        /// <summary>
        /// Creates an array from the converted values.
        /// </summary>
        /// <param name="elementType">The element type.</param>
        /// <param name="convertedValues">The values to add to the array.</param>
        /// <returns>The created array.</returns>
        private object CreateArray(Type elementType, List<object> convertedValues)
        {
            var array = Array.CreateInstance(elementType, convertedValues.Count);

            for (int i = 0; i < convertedValues.Count; i++)
            {
                array.SetValue(convertedValues[i], i);
            }

            return array;
        }

        /// <summary>
        /// Creates a generic list from the converted values.
        /// </summary>
        /// <param name="elementType">The element type.</param>
        /// <param name="convertedValues">The values to add to the list.</param>
        /// <returns>The created list.</returns>
        private object CreateGenericList(Type elementType, List<object> convertedValues)
        {
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = Activator.CreateInstance(listType);
            var addMethod = listType.GetMethod("Add");

            foreach (var convertedValue in convertedValues)
            {
                addMethod.Invoke(list, new[] { convertedValue });
            }

            return list;
        }

        /// <summary>
        /// Creates an ArrayList from the converted values.
        /// </summary>
        /// <param name="convertedValues">The values to add to the ArrayList.</param>
        /// <returns>The created ArrayList.</returns>
        private object CreateArrayList(List<object> convertedValues)
        {
            var arrayList = new System.Collections.ArrayList();

            foreach (var convertedValue in convertedValues)
            {
                arrayList.Add(convertedValue);
            }

            return arrayList;
        }

        /// <summary>
        /// Builds the complete CMIS query string from the selected properties and conditions.
        /// </summary>
        /// <param name="objectType">The CMIS object type to query (typically derived from the generic type T).</param>
        /// <returns>A formatted CMIS query string ready for execution.</returns>
        private string BuildQuery(string objectType)
        {
            string selectClause = this.selectedProperties.Any() ? string.Join(", ", this.selectedProperties) : "*";

            string whereClause = this.conditions.Any() ? "WHERE " + string.Join(" AND ", this.conditions.Select(this.FormatCondition)) : string.Empty;

            string orderByClause = this.orderByClause.Any() ? "ORDER BY " + string.Join(", ", this.orderByClause) : string.Empty;

            return $"SELECT {selectClause} FROM {objectType} {whereClause} {orderByClause}".Trim();
        }

        /// <summary>
        /// Formats a single query condition into a SQL-like string representation.
        /// Handles special cases for NULL checks and standard value comparisons.
        /// </summary>
        /// <param name="condition">The query condition to format.</param>
        /// <returns>A formatted condition string suitable for inclusion in a CMIS query.</returns>
        private string FormatCondition(QueryCondition condition)
        {
            if (condition.Operator == "IS" || condition.Operator == "IS NOT")
            {
                return $"{condition.Property} {condition.Operator} NULL";
            }

            return $"{condition.Property} {condition.Operator} '{condition.Value}'";
        }

        /// <summary>
        /// Represents a single condition in a CMIS query WHERE clause.
        /// Encapsulates the property name, comparison value, and operator.
        /// </summary>
        private class QueryCondition
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="QueryCondition"/> class.
            /// </summary>
            /// <param name="property">The name of the property to compare.</param>
            /// <param name="value">The value to compare against.</param>
            /// <param name="operator">The comparison operator to use.</param>
            public QueryCondition(string property, object value, string @operator)
            {
                this.Property = property;
                this.Value = value;
                this.Operator = @operator;
            }

            /// <summary>
            /// Gets the name of the property to be compared.
            /// </summary>
            public string Property { get; }

            /// <summary>
            /// Gets the value to compare the property against.
            /// </summary>
            public object Value { get; }

            /// <summary>
            /// Gets the comparison operator (e.g., "=", "!=", "IS", "IS NOT").
            /// </summary>
            public string Operator { get; }
        }

        /// <summary>
        /// Represents a property mapping condition and action pair.
        /// </summary>
        private class PropertyMapper
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PropertyMapper"/> class.
            /// </summary>
            /// <param name="condition">The condition to check.</param>
            /// <param name="action">The action to execute if condition is true.</param>
            public PropertyMapper(Func<bool> condition, Action action)
            {
                this.Condition = condition;
                this.Action = action;
            }

            /// <summary>
            /// Gets the condition function to evaluate.
            /// </summary>
            public Func<bool> Condition { get; }

            /// <summary>
            /// Gets the action to execute.
            /// </summary>
            public Action Action { get; }
        }
    }

    /// <summary>
    /// Represents a paginated result set containing data and pagination information.
    /// </summary>
    /// <typeparam name="T">The type of data contained in the result set.</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// Gets or sets the data for the current page.
        /// </summary>
        public List<T> Data { get; set; }

        /// <summary>
        /// Gets or sets the current page number (0-based).
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Gets or sets the number of items per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets the total number of items across all pages.
        /// </summary>
        public int TotalCount { get; set; }
    }
}
