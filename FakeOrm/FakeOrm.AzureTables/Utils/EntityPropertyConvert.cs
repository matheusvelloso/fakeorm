using FakeOrm.AzureTables.Attributes;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace FakeOrm.AzureTables.Utils
{
    public static class EntityPropertyConvert
    {
        public static IDictionary<string, EntityProperty> Serialize<TEntity>(TEntity entity, OperationContext operationContext)
        {
            IDictionary<string, EntityProperty> result = new Dictionary<string, EntityProperty>();

            foreach (var property in entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                Type type = property.PropertyType;
                string propertyName = PropertyValidation(property);

                var ignoredProperty = (PropertyIgnoredAttribute)Attribute.GetCustomAttribute(property, typeof(PropertyIgnoredAttribute));
                if (ignoredProperty != null)
                    continue;

                var attributedProperty = (PropertySerializedAttribute)Attribute.GetCustomAttribute(property, typeof(PropertySerializedAttribute));
                var valueProperty = entity.GetType().GetProperty(property.Name)?.GetValue(entity);

                if (attributedProperty != null)
                {
                    result.Add(propertyName, new EntityProperty(JsonConvert.SerializeObject(valueProperty)));
                }
                else
                {
                    dynamic value = Convert.ChangeType(valueProperty, type);
                    result.Add(propertyName, SerializeProperty(type, value));
                }
            }

            return result;
        }

        public static void Deserialize<TEntity>(TEntity entity, IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            TableEntity.ReadUserObject(entity, properties, operationContext);

            foreach (var property in entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                var propertyName = PropertyValidation(property);

                if (properties.ContainsKey(propertyName))
                    DeserializeProperty(entity, property, properties[propertyName]);

                var attributedProperty = (PropertySerializedAttribute)Attribute.GetCustomAttribute(property, typeof(PropertySerializedAttribute));
                if (attributedProperty != null)
                {
                    ValidateDeSerializePropertySerializedAttribute(entity, properties, property, propertyName, attributedProperty);
                    continue;
                }

                if (typeof(DateTimeOffset) == property.PropertyType)
                {
                    DateTimeOffset dateTimeOffset;
                    if (properties.ContainsKey(propertyName) && DateTimeOffset.TryParse(properties[propertyName].StringValue, out dateTimeOffset))
                        entity.GetType().GetProperty(property.Name).SetValue(entity, dateTimeOffset);
                    continue;
                }

                if (typeof(Guid) == property.PropertyType)
                {
                    Guid guid;
                    if (properties.ContainsKey(propertyName) && Guid.TryParse(properties[propertyName].StringValue, out guid))
                        entity.GetType().GetProperty(property.Name).SetValue(entity, guid);
                    continue;
                }

                var propertyNameAttribute = (PropertyNameAttribute)Attribute.GetCustomAttribute(property, typeof(PropertyNameAttribute));
                if (propertyNameAttribute != null)
                {
                    DeserializeProperty(entity, property, properties[propertyName]);
                }

            }
        }

        private static void DeserializeProperty<TEntity>(TEntity entity, PropertyInfo property, EntityProperty entityProperty)
        {
            switch (Type.GetTypeCode(property.PropertyType))
            {
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    object value;
                    if (property.PropertyType.IsEnum) 
                        value = Enum.ToObject(property.PropertyType, entityProperty.Int64Value);
                    else 
                        value = Convert.ChangeType(entityProperty.Int64Value, property.PropertyType);

                    property.SetValue(entity, value);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    property.SetValue(entity, entityProperty.Int64Value);
                    break;
                case TypeCode.String:
                    property.SetValue(entity, entityProperty.StringValue);
                    break;

            }
        }

        private static void ValidateDeSerializePropertySerializedAttribute<TEntity>(TEntity entity, IDictionary<string, EntityProperty> properties, PropertyInfo property, string propertyName, PropertySerializedAttribute attributedProperty)
        {
            Type resultType = null;
            if (attributedProperty.ConvertToType != null)
            {
                resultType = attributedProperty.ConvertToType;
            }
            else
            {
                resultType = property.PropertyType;
            }
            var objectValue = JsonConvert.DeserializeObject(properties[propertyName].StringValue, resultType);
            entity.GetType().GetProperty(property.Name)?.SetValue(entity, objectValue);
        }

        private static EntityProperty SerializeProperty(Type type, dynamic value)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Empty:
                    break;
                case TypeCode.DBNull:
                    break;
                case TypeCode.Boolean:
                    return new EntityProperty(Convert.ToBoolean(value));
                case TypeCode.Char:
                    break;
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:

                    return new EntityProperty((long)value);
                case TypeCode.DateTime:
                    return new EntityProperty(Convert.ToDateTime(value));
                case TypeCode.String:
                    var parsedValue = Convert.ToString(value);
                    return new EntityProperty(String.IsNullOrEmpty(parsedValue) ? String.Empty : parsedValue);
                case TypeCode.Object:
                default:
                    if (typeof(DateTimeOffset) == type)
                        return new EntityProperty(value);

                    if (typeof(Guid) == type)
                    {
                        //TODO: SAVE GUID TYPE GUID
                        var guid = (Guid)value;
                        return new EntityProperty(guid.ToString());
                    }

                    return new EntityProperty(String.Empty);
            }

            return new EntityProperty(Convert.ToString(value));
        }

        private static string PropertyValidation(PropertyInfo property)
        {

            foreach (var item in property.CustomAttributes)
            {
                if (item.AttributeType.Name != nameof(PropertyNameAttribute)) continue;
                foreach (var i in item.ConstructorArguments)
                {

                    return i.Value.ToString();
                }
            }
            return property.Name;
        }
    }
}
