namespace Core
{
    using DotCMIS.Client;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class CMISService<T>
        where T : class, new()
    {
        private readonly ISession session;
        private readonly List<QueryCondition> conditions = new List<QueryCondition>();
        private readonly List<string> selectedProperties = new List<string>();

        public CMISService(ISession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public CMISService<T> Select(params string[] properties)
        {
            this.selectedProperties.AddRange(properties);
            return this;
        }

        public CMISService<T> Where(string property, object value, string @operator = "=")
        {
            this.conditions.Add(new QueryCondition(property, value, @operator));
            return this;
        }

        public CMISService<T> WhereNotNull(string property)
        {
            return this.Where(property, null, "IS NOT");
        }

        public CMISService<T> WhereNull(string property)
        {
            return this.Where(property, null, "IS");
        }

        public T Execute()
        {
            string objectType = typeof(T).Name;
            string query = this.BuildQuery(objectType);

            var result = this.session.Query(query, true).ToList();

            if (result.Count > 0)
            {
                return this.MapResults(result);
            }

            return null;
        }

        public IDocument GetObjectById(string Id)
        {
            return this.session.GetObject(Id) as IDocument;
        }

        private T MapResults(IList<IQueryResult> results)
        {
            var properties = typeof(T).GetProperties().Where(p => p.CanWrite);
            var obj = Activator.CreateInstance<T>();

            var result = results.First();

            foreach (var prop in properties)
            {
                var cmisProperty = result.Properties.FirstOrDefault(p =>
                    string.Equals(p.LocalName, prop.Name, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.QueryName, prop.Name, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.DisplayName, prop.Name, StringComparison.OrdinalIgnoreCase));

                if (cmisProperty != null && cmisProperty.FirstValue != null)
                {
                    var value = Convert.ChangeType(cmisProperty.FirstValue, prop.PropertyType);
                    prop.SetValue(obj, value);
                }
            }

            return obj;
        }

        private string BuildQuery(string objectType)
        {
            string selectClause = this.selectedProperties.Any() ?
                string.Join(", ", this.selectedProperties) : "*";

            string whereClause = this.conditions.Any() ?
                "WHERE " + string.Join(" AND ", this.conditions.Select(this.FormatCondition)) : string.Empty;

            return $"SELECT {selectClause} FROM {objectType} {whereClause}";
        }

        private string FormatCondition(QueryCondition condition)
        {
            if (condition.Operator == "IS" || condition.Operator == "IS NOT")
            {
                return $"{condition.Property} {condition.Operator} NULL";
            }

            return $"{condition.Property} {condition.Operator} '{condition.Value}'";
        }

        private class QueryCondition
        {
            public string Property { get; }

            public object Value { get; }

            public string Operator { get; }

            public QueryCondition(string property, object value, string @operator)
            {
                this.Property = property;
                this.Value = value;
                this.Operator = @operator;
            }
        }
    }
}
