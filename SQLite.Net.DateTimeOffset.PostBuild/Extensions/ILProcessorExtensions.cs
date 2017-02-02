using Mono.Cecil.Cil;

namespace SQLite.Net.DateTimeOffset.PostBuild.Extensions
{
	/// <summary>
	/// Contains extension methods for the <see cref="Mono.Cecil.Cil.ILProcessor"/> class.
	/// </summary>
	internal static class ILProcessorExtensions
	{
		/// <summary>
		/// Removes all instrcutions from a <see cref="Mono.Cecil.Cil.ILProcessor"/>'s body.
		/// </summary>
		/// <param name="processor"></param>
		internal static void Empty(this ILProcessor processor)
		{
			for (int i = processor.Body.Instructions.Count - 1; i >= 0; i--)
				processor.Remove(processor.Body.Instructions[i]);
		}
	}
}
