using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using SQLite.Net.DateTimeOffset.PostBuild.Extensions;

namespace SQLite.Net.DateTimeOffset.PostBuild
{
    public class PostBuildTask : Task
    {
		/// <summary>
		/// Path (incl. file name) of the executable to be processed.
		/// </summary>
		[Required]
		public string AssemblyPath { get; set; }

		public override bool Execute()
	    {
			// Load assembly
			ModuleDefinition module;
			try
			{
				var resolver = new DefaultAssemblyResolver();
				resolver.AddSearchDirectory(Path.GetDirectoryName(AssemblyPath));
				module = ModuleDefinition.ReadModule(AssemblyPath, new ReaderParameters()
				{
					AssemblyResolver = resolver,
					ReadSymbols = true
				});
			}
			catch (Exception e)
			{
				Log.LogError(Resources.Resources.Error_LoadAssembly, AssemblyPath, e.Message);
				return false;
			}

			bool hasChanged = false;

			foreach (var type in module.Types)
			{
				// Find all properties flagged with the "DateTimeOffsetSerializeAttribute" attribute
				var propertiesToRebuild = type.FindFlaggedProperties();
				if (propertiesToRebuild == null)
				{
					// Used "DateTimeOffsetSerializeAttribute" attribute on non-DateTimeOffset property?
					Log.LogError(Resources.Resources.Error_WrongDataType);
					return false;
				}
				var flaggedProperties = propertiesToRebuild as FlaggedProperty[] ?? propertiesToRebuild.ToArray();
				if (!flaggedProperties.Any())
					continue;

				foreach (var property in flaggedProperties)
				{
					// Process all detected properties
					Log.LogMessage(Resources.Resources.Message_PropertyProcessing, property.Property.FullName);
					if (!type.RebuildProperty(property.Property, property.Format, property.KeepOriginal, Log))
						return false;
				}
				hasChanged = true;
			}
			// If any properties have been rebuilt, write these changes back to the assembly
			if (hasChanged)
				return module.WriteAssembly(AssemblyPath, Log);

			return true;
		}
	}
}
