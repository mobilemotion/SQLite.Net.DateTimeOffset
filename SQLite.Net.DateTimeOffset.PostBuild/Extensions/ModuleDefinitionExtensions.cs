using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace SQLite.Net.DateTimeOffset.PostBuild.Extensions
{
    /// <summary>
    /// Contains extension methods for the <see cref="Mono.Cecil.ModuleDefinition"/> class.
    /// </summary>
    internal static class ModuleDefinitionExtensions
    {
        /// <summary>
        /// Stores the open assembly as executable file.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="path"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        internal static bool WriteAssembly(this ModuleDefinition module, string path, TaskLoggingHelper log)
        {
            try
            {
                // First backup the original file
                string backupFile = $"{path}.bak";
                File.Delete(backupFile);
                File.Copy(path, backupFile);
            }
            catch (Exception e1)
            {
                log.LogError(Resources.Resources.Error_CreateBackupFailed, path, e1.Message);
                return false;
            }
            try
            {
                module.Assembly.Write(path, new WriterParameters
                {
                    WriteSymbols = true
                });
            }
            catch (Exception e2)
            {
                log.LogErrorFromException(e2, showStackTrace: false);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Resolves and returns the <code>System.DateTimeOffset.ParseExact()</code> method.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        internal static MethodReference FindDateTimeOffsetParseExactMethod(this ModuleDefinition module,
            TaskLoggingHelper log)
        {
            TypeDefinition dateTimeOffsetType;
            try
            {
                dateTimeOffsetType = module.Import(typeof(System.DateTimeOffset)).Resolve();
            }
            catch (Exception e)
            {
                log.LogError(Resources.Resources.Error_ResolveDateTimeOffsetParseExactMethodFailed, e.Message);
                return null;
            }
            var foreignParseExactMethod = dateTimeOffsetType.Methods.Single(m =>
                m.Name.Equals("ParseExact") &&
                m.Parameters.Count == 3 &&
                m.Parameters[0].ParameterType.MetadataType == MetadataType.String &&
                m.Parameters[1].ParameterType.MetadataType == MetadataType.String &&
                m.Parameters[2].ParameterType.Name.Equals("IFormatProvider"));
            var parseExactMethod = module.Import(foreignParseExactMethod);
            return parseExactMethod;
        }

        /// <summary>
        /// Resolves and returns the <code>System.DateTimeOffset.ToString()</code> method.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        internal static MethodReference FindDateTimeOffsetToStringMethod(this ModuleDefinition module,
            TaskLoggingHelper log)
        {
            TypeDefinition dateTimeOffsetType;
            try
            {
                dateTimeOffsetType = module.Import(typeof(System.DateTimeOffset)).Resolve();
            }
            catch (Exception e)
            {
                log.LogWarning(Resources.Resources.Error_ResolveDateTimeOffsetToStringMethodFailed, e.Message);
                return null;
            }
            var foreignToStringMethod = dateTimeOffsetType.Methods.Single(m =>
                m.Name.Equals("ToString") &&
                m.Parameters.Count == 1 &&
                m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
            var toStringMethod = module.Import(foreignToStringMethod);
            return toStringMethod;
        }

        /// <summary>
        /// Resolves and returns the <code>System.Globalization.CultureInfo.InvariantCulture</code> property's constructor method.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        internal static MethodReference FindInvariantCultureGetterMethod(this ModuleDefinition module,
            TaskLoggingHelper log)
        {
            TypeDefinition cultureInfoType;
            try
            {
                cultureInfoType = module.Import(typeof(CultureInfo)).Resolve();
            }
            catch (Exception e)
            {
                log.LogWarning(Resources.Resources.Error_ResolveInvariantCultureGetterMethodFailed, e.Message);
                return null;
            }
            var invariantCultureProperty = cultureInfoType.Properties.Single(p => p.Name.Equals("InvariantCulture"));
            var foreignGetterMethod = invariantCultureProperty.GetMethod;
            MethodReference getterMethod = module.Import(foreignGetterMethod);
            return getterMethod;
        }

        /// <summary>
        /// Resolves and returns the <code>SQLite.Ignore</code> attribute's constructor method.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        internal static MethodReference FindIgnoreAttributeCtorMethod(this ModuleDefinition module,
            TaskLoggingHelper log)
        {
            TypeDefinition ignoreAttributeType;
            try
            {
                ignoreAttributeType = module.Import(typeof(IgnoreAttribute)).Resolve();
            }
            catch (Exception e)
            {
                log.LogWarning(Resources.Resources.Error_ResolveIgnoreAttributeCtorMethodFailed, e.Message);
                return null;
            }
            var foreignCtorMethod = ignoreAttributeType.GetConstructors().Single(m =>
                m.Parameters.Count == 0);
            var ctorMethod = module.Import(foreignCtorMethod);
            return ctorMethod;
        }

        /// <summary>
        /// Resolves and returns the <code>SQLite.Column</code> attribute's constructor method.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        internal static MethodReference FindColumnAttributeCtorMethod(this ModuleDefinition module,
            TaskLoggingHelper log)
        {
            TypeDefinition columnAttributeType;
            try
            {
                columnAttributeType = module.Import(typeof(ColumnAttribute)).Resolve();
            }
            catch (Exception e)
            {
                log.LogWarning(Resources.Resources.Error_ResolveColumnAttributeCtorMethodFailed, e.Message);
                return null;
            }
            var foreignCtorMethod = columnAttributeType.GetConstructors().Single(m =>
                m.Parameters.Count == 1 &&
                m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
            var ctorMethod = module.Import(foreignCtorMethod);
            return ctorMethod;
        }
    }
}
