﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Ecng.Web.BBCodes.SyntaxTree
{
    public interface ISyntaxTreeNodeCollection : IList<SyntaxTreeNode>
    {
    }

    public class SyntaxTreeNodeCollection : Collection<SyntaxTreeNode>, ISyntaxTreeNodeCollection
    {
        public SyntaxTreeNodeCollection()
            : base()
        {
        }
        public SyntaxTreeNodeCollection(IEnumerable<SyntaxTreeNode> list)
            : base(list.ToArray())
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
        }

        protected override void SetItem(int index, SyntaxTreeNode item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            base.SetItem(index, item);
        }
        protected override void InsertItem(int index, SyntaxTreeNode item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            base.InsertItem(index, item);
        }
    }

    public class ImmutableSyntaxTreeNodeCollection : ReadOnlyCollection<SyntaxTreeNode>, ISyntaxTreeNodeCollection
    {
        public ImmutableSyntaxTreeNodeCollection(IEnumerable<SyntaxTreeNode> list)
            : base(list.ToArray())
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
        }
        internal ImmutableSyntaxTreeNodeCollection(IList<SyntaxTreeNode> list, bool isFresh)
            : base(isFresh ? list : list.ToArray())
        {
        }

        static readonly ImmutableSyntaxTreeNodeCollection empty = new ImmutableSyntaxTreeNodeCollection(new SyntaxTreeNode[0], true);
        public static ImmutableSyntaxTreeNodeCollection Empty => empty;
    }
}
