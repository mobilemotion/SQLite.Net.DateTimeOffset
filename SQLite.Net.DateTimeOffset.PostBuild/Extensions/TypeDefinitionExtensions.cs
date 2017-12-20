using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace SQLite.Net.DateTimeOffset.PostBuild.Extensions
{
    internal static class TypeDefinitionExtensions
    {
        /// <summary>
        /// Finds all properties of a given type that are flagged with the "DateTimeOffsetSerialize" attribute, are NOT flagged
        /// with the "SQLite.Ignore" attribute, and are of type <see cref="System.DateTimeOffset"/>. Returns a
        /// list containing all detected properties within the given type, or an empty list if no matching properties could be
        /// found, or NULL if the "DateTimeOffsetSerialize" attribute has been used on a property of a different data type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static IEnumerable<FlaggedProperty> FindFlaggedProperties(this TypeDefinition type)
        {
            var results = new List<FlaggedProperty>();

            foreach (var property in type.Properties.Where(p =>
                p.HasCustomAttributes &&
                p.CustomAttributes.Any(a =>
                    a.AttributeType.FullName.Equals(
                        "SQLite.Net.DateTimeOffset.Attributes.DateTimeOffsetSerializeAttribute")) &&
                !p.CustomAttributes.Any(a => a.AttributeType.FullName.Equals("SQLite.IgnoreAttribute"))))
            {
                if (!property.PropertyType.FullName.Equals("System.DateTimeOffset"))
                    return null;

                var serializeAttribute =
                    property.CustomAttributes.FirstOrDefault(
                        a => a.AttributeType.FullName.Equals(
                            "SQLite.Net.DateTimeOffset.Attributes.DateTimeOffsetSerializeAttribute"));

                // Define default values...
                var format = "yyyy-MM-dd HH:mm:ss zzzz";
                var keepOriginal = false;
                if (serializeAttribute.HasConstructorArguments)
                {
                    // ...and override them, if specified in the attribute constructor
                    foreach (var argument in serializeAttribute.ConstructorArguments)
                    {
                        if (argument.Type.MetadataType == MetadataType.String)
                            format = argument.Value as string;
                        if (argument.Type.MetadataType == MetadataType.Boolean)
                            keepOriginal = (bool) argument.Value;
                    }
                }

                results.Add(new FlaggedProperty
                {
                    Property = property,
                    Format = format,
                    KeepOriginal = keepOriginal
                });
            }

            return results;
        }

        /// <summary>
        /// Processes a given property by creating a new property of type string, adapting the original property's getter and
        /// setter methods to point to the newly created property, and rearrange the original property's attributes.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="property"></param>
        /// <param name="serializeFormat"></param>
        /// <param name="keepOriginal"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        internal static bool RebuildProperty(this TypeDefinition type, PropertyDefinition property,
            string serializeFormat, bool keepOriginal, TaskLoggingHelper log)
        {
            // Resolve basic data types
            var stringType = type.Module.Import(typeof(string));
            var voidType = type.Module.Import(typeof(void));

            // Define member names
            var propertyNameOriginal = property.Name;
            var propertyNameDuplicate = $"{propertyNameOriginal}_Serialized";
            var rnd = new Random();
            while (type.Properties.Any(p => p.Name.Equals(propertyNameDuplicate)))
            {
                // Don't accidentally use the name of an existing property
                var suffix = rnd.Next(1000, 9999);
                propertyNameDuplicate = $"{propertyNameOriginal}_Serialized_{suffix}";
            }
            var backingFieldName = $"<{propertyNameDuplicate}>k_BackingField";
            var getMethodName = $"get_{propertyNameDuplicate}";
            var setMethodName = $"set_{propertyNameDuplicate}";

            // Ensure that the new property is stored in a column that is called the same as the original property
            // (or as specified in the original property's "Column" attribute, if any)
            var columnName = property.Name;
            if (keepOriginal)
            {
                // If the original property shall be stored in the database as well, we are forced to use the new
                // property's name as column name
                columnName = propertyNameDuplicate;
            }
            if (property.HasCustomAttributes)
            {
                foreach (var attribute in property.CustomAttributes)
                {
                    if (attribute.AttributeType.FullName.Equals("SQLite.ColumnAttribute") &&
                        attribute.HasConstructorArguments)
                    {
                        // If the original property has been decorated with a Column attribute, we need to ensure that
                        // the desired column name is used for the new property (maybe followed by a suffix, if the
                        // original property shall be kept as well)
                        columnName = attribute.ConstructorArguments[0].Value as string;
                        if (keepOriginal)
                            columnName += "_Serialized";
                        break;
                    }
                }
            }

            // Create new property including backing field
            var duplicateProperty = new PropertyDefinition(propertyNameDuplicate, PropertyAttributes.None, stringType);
            var backingField = new FieldDefinition(backingFieldName, FieldAttributes.Private, stringType);
            type.Fields.Add(backingField);

            // Provide an initial value for the new property's backing field
            var toStringMethod = type.Module.FindDateTimeOffsetToStringMethod(log);
            if (toStringMethod == null) return false;

            var originalBackingField = type.Fields.FirstOrDefault(
                f => f.FieldType.FullName.Equals("System.DateTimeOffset") &&
                     f.Name.Equals($"<{property.Name}>k__BackingField"));

            var newInitialSetter1 = Instruction.Create(OpCodes.Ldarg_0);
            var newInitialSetter2 = Instruction.Create(OpCodes.Ldarg_0);
            var newInitialSetter3 = Instruction.Create(OpCodes.Ldflda, originalBackingField);
            var newInitialSetter4 = Instruction.Create(OpCodes.Ldstr, serializeFormat);
            var newInitialSetter5 = Instruction.Create(OpCodes.Call, toStringMethod);
            var newInitialSetter6 = Instruction.Create(OpCodes.Stfld, backingField);

            var ctors = type.Methods.Where(m => m.Name.Equals(".ctor"));
            foreach (var ctor in ctors)
            {
                var ctorProcessor = ctor.Body.GetILProcessor();
                var initialSetter = ctor.Body.Instructions.FirstOrDefault(i =>
                    i.OpCode == OpCodes.Stfld && i.Operand is FieldDefinition && i.Operand == originalBackingField);
                if (initialSetter != null)
                {
                    ctorProcessor.InsertAfter(initialSetter, newInitialSetter1);
                }
                else
                {
                    ctorProcessor.InsertBefore(ctorProcessor.Body.Instructions[0], newInitialSetter1);
                }
                ctorProcessor.InsertAfter(newInitialSetter1, newInitialSetter2);
                ctorProcessor.InsertAfter(newInitialSetter2, newInitialSetter3);
                ctorProcessor.InsertAfter(newInitialSetter3, newInitialSetter4);
                ctorProcessor.InsertAfter(newInitialSetter4, newInitialSetter5);
                ctorProcessor.InsertAfter(newInitialSetter5, newInitialSetter6);
            }

            // Create getter method for new property
            var getMethod = new MethodDefinition(getMethodName,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, stringType);
            getMethod.Body = new MethodBody(getMethod);
            var processor = getMethod.Body.GetILProcessor();
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldfld, backingField);
            processor.Emit(OpCodes.Ret);
            type.Methods.Add(getMethod);
            duplicateProperty.GetMethod = getMethod;

            // Create setter method for new property
            var setMethod = new MethodDefinition(setMethodName,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, voidType);
            setMethod.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, stringType));
            setMethod.Body = new MethodBody(setMethod);
            processor = setMethod.Body.GetILProcessor();
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldarg_1);
            processor.Emit(OpCodes.Stfld, backingField);
            processor.Emit(OpCodes.Ret);
            type.Methods.Add(setMethod);
            duplicateProperty.SetMethod = setMethod;

            // Adapt existing property's getter method
            var parseExactMethod = type.Module.FindDateTimeOffsetParseExactMethod(log);
            if (parseExactMethod == null) return false;
            var invariantCultureGetterMethod = type.Module.FindInvariantCultureGetterMethod(log);
            if (invariantCultureGetterMethod == null) return false;

            processor = property.GetMethod.Body.GetILProcessor();
            processor.Empty();
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Call, getMethod);
            processor.Emit(OpCodes.Ldstr, serializeFormat);
            processor.Emit(OpCodes.Call, invariantCultureGetterMethod);
            processor.Emit(OpCodes.Call, parseExactMethod);
            processor.Emit(OpCodes.Ret);

            // Adapt existing property's setter method
            processor = property.SetMethod.Body.GetILProcessor();
            processor.Empty();
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldarga_S, property.SetMethod.Parameters[0]);
            processor.Emit(OpCodes.Ldstr, serializeFormat);
            processor.Emit(OpCodes.Call, toStringMethod);
            processor.Emit(OpCodes.Call, setMethod);
            processor.Emit(OpCodes.Ret);

            // Add new property to class
            type.Properties.Add(duplicateProperty);

            for (int i = property.CustomAttributes.Count - 1; i >= 0; i--)
            {
                // Remove the original "DateTimeOffsetSerialize" attribute (to ensure the property is not rebuilt
                // again on a potential second run)
                if (property.CustomAttributes[i].AttributeType.FullName
                    .Equals("SQLite.Net.DateTimeOffset.Attributes.DateTimeOffsetSerializeAttribute"))
                {
                    property.CustomAttributes.RemoveAt(i);
                }
                // If the original property shall not be stored, remove its Column attribute as well (if available)
                // Otherwise, it will have the same Column attribute assigned as the new property
                else if (!keepOriginal && property.CustomAttributes[i].AttributeType.FullName
                             .Equals("SQLite.ColumnAttribute"))
                {
                    property.CustomAttributes.RemoveAt(i);
                }
            }

            // Add a "Column" attribute to the new property, to guarantee a human-readable column name in the database
            var columnsCtorMethod = type.Module.FindColumnAttributeCtorMethod(log);
            if (columnsCtorMethod == null) return false;
            var columnAttribute = new CustomAttribute(columnsCtorMethod);
            columnAttribute.ConstructorArguments.Add(new CustomAttributeArgument(type.Module.TypeSystem.String,
                columnName));
            duplicateProperty.CustomAttributes.Add(columnAttribute);

            // If the original property shall not be stored in the database, flag it with an "Ignore" attribute
            if (!keepOriginal)
            {
                var ignoreCtorMethod = type.Module.FindIgnoreAttributeCtorMethod(log);
                if (ignoreCtorMethod == null) return false;
                property.CustomAttributes.Add(new CustomAttribute(ignoreCtorMethod));
            }

            return true;
        }
    }
}
