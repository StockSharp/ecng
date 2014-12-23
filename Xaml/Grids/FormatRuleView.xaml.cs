﻿namespace Ecng.Xaml.Grids
{
	using System.Windows;

	using Ecng.Collections;
	using Ecng.ComponentModel;
	using Ecng.Localization;

	public partial class FormatRuleView
    {
        public FormatRuleView()
        {
			InitializeComponent();

			ConditionsCombo.SetDataSource<ComparisonOperator>();
			SelectedCondition = ComparisonOperator.Less;

	        BackgroundLabel.Text = BackgroundLabel.Text.Translate();
			ForegroundLabel.Text = ForegroundLabel.Text.Translate();
        }

		private readonly PairSet<int, ComparisonOperator> _conditions = new PairSet<int, ComparisonOperator>
		{
			{ 0, ComparisonOperator.Less },
			{ 1, ComparisonOperator.LessOrEqual },
			{ 2, ComparisonOperator.Equal },
			{ 3, ComparisonOperator.NotEqual },
			{ 4, ComparisonOperator.GreaterOrEqual },
			{ 5, ComparisonOperator.Greater },
		};

		public ComparisonOperator SelectedCondition
		{
			get { return _conditions[ConditionsCombo.SelectedIndex]; }
			set { ConditionsCombo.SelectedIndex = _conditions[value]; }
		}

		public static readonly DependencyProperty RuleProperty = DependencyProperty.Register("Rule", typeof(FormatRule), typeof(FormatRuleView), new PropertyMetadata());

		public FormatRule Rule
		{
			get { return (FormatRule)GetValue(RuleProperty); }
			set { SetValue(RuleProperty, value); }
		}
    }
}