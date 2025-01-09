﻿namespace Ecng.Compilation;

using System;

using Ecng.Collections;

public class CompilerProvider
{
	private readonly SynchronizedDictionary<CompilationLanguages, ICompiler> _compilers = [];

	public void RegisterCompiler(CompilationLanguages language, ICompiler compiler)
		=> _compilers.Add(language, compiler ?? throw new ArgumentNullException(nameof(compiler)));

	public ICompiler GetCompiler(CompilationLanguages language)
		=> _compilers[language];

	public bool TryGetCompiler(CompilationLanguages language, out ICompiler compiler)
		=> _compilers.TryGetValue(language, out compiler);

	public void UnRegisterCompiler(CompilationLanguages language)
		=> _compilers.Remove(language);
}