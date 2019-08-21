namespace Ecng.ComponentModel.Expressions
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	/// <summary>
	/// Compiled mathematical formula.
	/// </summary>
	public abstract class ExpressionFormula
	{
		/// <summary>
		/// To calculate the basket value.
		/// </summary>
		/// <param name="values">Inner values.</param>
		/// <returns>The basket value.</returns>
		public abstract decimal Calculate(decimal[] values);

		/// <summary>
		/// Initializes a new instance of the <see cref="ExpressionFormula"/>.
		/// </summary>
		/// <param name="expression">Mathematical formula.</param>
		/// <param name="identifiers">Identifiers.</param>
		protected ExpressionFormula(string expression, IEnumerable<string> identifiers)
		{
			if (expression.IsEmpty())
				throw new ArgumentNullException(nameof(expression));

			Expression = expression;
			Identifiers = identifiers ?? throw new ArgumentNullException(nameof(identifiers));
		}

		internal ExpressionFormula(string error)
		{
			if (error.IsEmpty())
				throw new ArgumentNullException(nameof(error));

			Error = error;
		}

		/// <summary>
		/// Mathematical formula.
		/// </summary>
		public string Expression { get; }

		/// <summary>
		/// Compilation error.
		/// </summary>
		public string Error { get; }

		/// <summary>
		/// Identifiers.
		/// </summary>
		public IEnumerable<string> Identifiers { get; }

		/// <summary>
		/// Available functions.
		/// </summary>
		public static IEnumerable<string> Functions => ExpressionHelper.Functions;
	}
}