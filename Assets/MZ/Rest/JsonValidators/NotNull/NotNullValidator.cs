using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MZ.Rest.JsonValidators.NotNull
{
    public class NotNullValidator
    {
        private readonly Dictionary<Type, FieldInfo[]> fieldsDictionary = new Dictionary<Type, FieldInfo[]>();

        public void ValidateData(object root, Type rootType, List<string> errors, string rootName = "root", int currentDepth = 1, int maxDepth = 10)
        {
            // too deep
            if (currentDepth > maxDepth)
            {
                return;
            }

            // value type
            if (rootType.IsValueType)
            {
                return;
            }

            // type fields
            FieldInfo[] fields = GetFields(rootType);

            foreach (FieldInfo field in fields)
            {
                var fieldName = field.Name;
                var fieldValue = root != null ? field.GetValue(root) : null;
                var fieldType = field.FieldType;

                // field must be ignored
                if (Attribute.GetCustomAttribute(field, typeof(JsonIgnoreAttribute)) != null)
                {
                    return;
                }

                if (Attribute.GetCustomAttribute(field, typeof(CannotBeNullAttribute)) is CannotBeNullAttribute)
                {
                    if (fieldValue == null)
                    {
                        errors.Add($"Path {rootName}.{fieldName} is null but it should have a value");
                    }
                }

                if (fieldValue != null)
                {
                    // list
                    if (fieldValue is IEnumerable fieldValueList)
                    {
                        Type itemType = null;

                        // generics
                        if (fieldType.IsGenericType && fieldType.GetGenericArguments().Length == 1)
                        {
                            itemType = fieldType.GetGenericArguments()[0];
                        }
                        // array
                        else if (fieldType.GetElementType() != null)
                        {
                            itemType = fieldType.GetElementType();
                        }

                        if (itemType != null)
                        {
                            int index = 0;
                            foreach (var item in fieldValueList)
                            {
                                ValidateData(item, itemType, errors, $"{rootName}.{fieldName}[{index}]", currentDepth + 1);
                                index += 1;
                            }
                        }
                    }
                    // normal property
                    else
                    {
                        ValidateData(fieldValue, fieldType, errors, $"{rootName}.{fieldName}", currentDepth + 1);
                    }
                }
            }
        }

        private FieldInfo[] GetFields(Type rootType)
        {
            FieldInfo[] fields;

            if (fieldsDictionary.ContainsKey(rootType))
            {
                fields = fieldsDictionary[rootType];
            }
            else
            {
                fields = rootType.GetFields(BindingFlags.Instance | BindingFlags.Public);
                fieldsDictionary[rootType] = fields;
            }

            return fields;
        }
    }
}
