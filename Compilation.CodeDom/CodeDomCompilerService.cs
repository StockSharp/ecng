﻿namespace Ecng.Compilation.CodeDom
{
	using System;

	using Ecng.Collections;

	[Obsolete("Use CodeDomCompiler directly.")]
	public class CodeDomCompilerService : ICompilerService
	{
		private readonly SynchronizedDictionary<CompilationLanguages, ICompiler> _compilers = new();

		public CodeDomCompilerService(string outputDir, string tempPath)
		{
			OutputDir = outputDir;
			TempPath = tempPath;
		}

		public string OutputDir { get; }
		public string TempPath { get; }

		ICompiler ICompilerService.GetCompiler(CompilationLanguages language)
			=> _compilers.SafeAdd(language, key => new CodeDomCompiler(key, OutputDir, TempPath));
	}
}