﻿namespace Ecng.Compilation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Ecng.Common;

public static class ICompilerExtensions
{
	public static string RuntimePath { get; } = Path.GetDirectoryName(typeof(object).Assembly.Location);

	public static string ToFullRuntimePath(this string assemblyName)
		=> Path.Combine(RuntimePath, assemblyName);

	/// <summary>
	/// Are there any errors in the compilation.
	/// </summary>
	/// <param name="result">The result of the compilation.</param>
	/// <returns><see langword="true" /> - If there are errors, <see langword="true" /> - If the compilation is performed without errors.</returns>
	public static bool HasErrors(this CompilationResult result)
		=> result.CheckOnNull(nameof(result)).Errors.Any(e => e.Type == CompilationErrorTypes.Error);

	public static CompilationResult Compile(this ICompiler compiler, string name, string source, IEnumerable<string> refs, CancellationToken cancellationToken = default)
		=> Compile(compiler, name, [source], refs, cancellationToken);

	public static CompilationResult Compile(this ICompiler compiler, string name, IEnumerable<string> sources, IEnumerable<string> refs, CancellationToken cancellationToken = default)
		=> compiler.Compile(name, sources, refs.Select(ToRef), cancellationToken);

	public static (string name, byte[] body) ToRef(this string path)
		=> (Path.GetFileNameWithoutExtension(path), File.ReadAllBytes(path));

	public static IEnumerable<string> ToValidPaths<TRef>(this IEnumerable<TRef> references)
		where TRef : ICodeReference
		=> references.Where(r => r.IsValid).Select(r => r.Location).ToArray();

	/// <summary>
	/// Throw if errors.
	/// </summary>
	/// <param name="res"><see cref="CompilationResult"/></param>
	/// <returns><see cref="CompilationResult"/></returns>
	public static CompilationResult ThrowIfErrors(this CompilationResult res)
	{
		if (res.HasErrors())
			throw new InvalidOperationException($"Compilation error: {res.Errors.Where(e => e.Type == CompilationErrorTypes.Error).Take(2).Select(e => e.ToString()).JoinN()}");

		return res;
	}
}